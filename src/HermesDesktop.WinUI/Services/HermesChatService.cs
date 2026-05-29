using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Sends chat prompts to remote Hermes CLI and returns results.
    /// Mirrors the macOS HermesChatService.
    /// </summary>
    public class HermesChatService
    {
        private readonly SSHTransport _sshTransport;

        public HermesChatService(SSHTransport sshTransport)
        {
            _sshTransport = sshTransport ?? throw new ArgumentNullException(nameof(sshTransport));
        }

        /// <summary>
        /// Sends a chat prompt to the remote Hermes instance.
        /// </summary>
        /// <param name="prompt">The message to send</param>
        /// <param name="sessionId">Optional session ID to resume; null = new session</param>
        /// <param name="autoApproveCommands">If true, uses --yolo mode</param>
        /// <param name="timeoutSeconds">Max time to wait for response (default 300 = 5 minutes)</param>
        public async Task<ChatTurnResult> SendMessageAsync(
            string prompt,
            string sessionId = null,
            bool autoApproveCommands = false,
            int timeoutSeconds = 300)
        {
            var pyPrompt = JsonSerializer.Serialize(prompt);
            var pySessionId = JsonSerializer.Serialize(sessionId ?? "");
            var pyTimeout = timeoutSeconds;
            var pyYolo = autoApproveCommands ? "True" : "False";

            var pythonScript = @"
import json, os, pathlib, subprocess, selectors, time, shutil

prompt = " + pyPrompt + @"
session_id = " + pySessionId + @"
timeout_seconds = " + pyTimeout + @"
auto_approve = " + pyYolo + @"

def compact_output(stdout, stderr, exit_code):
    merged = '\n'.join([
        (stderr or '').strip(),
        (stdout or '').strip()
    ]).strip()
    if not merged:
        return f'Hermes chat exited with code {exit_code}.'
    if len(merged) <= 8000:
        return merged
    return merged[-8000:]

def run_chat(command, env, timeout_secs):
    process = subprocess.Popen(
        command,
        env=env,
        stdin=subprocess.DEVNULL,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    started_at = time.monotonic()
    stdout_chunks = []
    stderr_chunks = []

    try:
        while True:
            now = time.monotonic()
            if now - started_at > timeout_secs:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except:
                    process.kill()
                stdout = ''.join(stdout_chunks)
                stderr = ''.join(stderr_chunks)
                return stdout, stderr, 124, True

            exit_code = process.poll()
            if exit_code is not None:
                remaining_stdout = process.stdout.read() if process.stdout else b''
                remaining_stderr = process.stderr.read() if process.stderr else b''
                if remaining_stdout:
                    stdout_chunks.append(remaining_stdout.decode('utf-8', errors='replace'))
                if remaining_stderr:
                    stderr_chunks.append(remaining_stderr.decode('utf-8', errors='replace'))
                return ''.join(stdout_chunks), ''.join(stderr_chunks), exit_code, False

            # Read available output
            ready, _, _ = selectors.DefaultSelector().select(timeout=0.5)
            # Simple polling approach instead
            time.sleep(0.3)

            if process.stdout and process.stdout.readable():
                try:
                    data = process.stdout.read1(4096)
                    if data:
                        stdout_chunks.append(data.decode('utf-8', errors='replace'))
                except:
                    pass
            if process.stderr and process.stderr.readable():
                try:
                    data = process.stderr.read1(4096)
                    if data:
                        stderr_chunks.append(data.decode('utf-8', errors='replace'))
                except:
                    pass
    finally:
        try:
            process.terminate()
        except:
            pass

try:
    home = pathlib.Path.home()
    hermes_home = home / '.hermes'
    env = os.environ.copy()
    env['HERMES_HOME'] = str(hermes_home)
    env['NO_COLOR'] = '1'
    env['TERM'] = 'dumb'

    # Find hermes binary
    hermes_path = shutil.which('hermes')
    if not hermes_path:
        local_bin = str(home / '.local' / 'bin' / 'hermes')
        if os.path.isfile(local_bin):
            hermes_path = local_bin
    if not hermes_path:
        print(json.dumps({'ok': False, 'error': 'Hermes CLI not found on remote host'}))
        sys.exit(0)

    # Build arguments
    args = [hermes_path]
    if session_id:
        args.extend(['--resume', session_id])
    if auto_approve:
        args.append('--yolo')
    args.extend(['chat', '--quiet', '--query', prompt])

    stdout, stderr, exit_code, timed_out = run_chat(args, env, timeout_seconds)

    output = compact_output(stdout, stderr, exit_code)
    if timed_out:
        output = '(timed out after ' + str(timeout_seconds) + 's)\n' + output

    # Try to extract session_id from the output
    new_session_id = session_id if session_id else None
    if not new_session_id and stdout:
        # Hermes sometimes outputs session_id in a recognizable format
        import re
        m = re.search(r'session[_-]?id[:\\s]+([a-zA-Z0-9_-]+)', stdout, re.IGNORECASE)
        if m:
            new_session_id = m.group(1)

    print(json.dumps({
        'ok': exit_code == 0 or not timed_out,
        'session_id': new_session_id,
        'output': output,
        'stdout': stdout,
        'stderr': stderr,
        'timed_out': timed_out,
    }, ensure_ascii=False))
except Exception as e:
    print(json.dumps({'ok': False, 'error': str(e)}))
";
            return await _sshTransport.ExecuteJSONAsync<ChatTurnResult>(pythonScript);
        }
    }
}
