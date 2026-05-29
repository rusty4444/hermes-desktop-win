using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Services
{
    public class CronBrowserService
    {
        private readonly SSHTransport _sshTransport;

        public CronBrowserService(SSHTransport sshTransport)
        {
            _sshTransport = sshTransport ?? throw new ArgumentNullException(nameof(sshTransport));
        }

        public async Task<List<CronJobInfo>> ListJobsAsync()
        {
            var pythonScript = @"
import json, os, pathlib
from datetime import datetime, timezone

def normalize_text(value):
    if value is None:
        return None
    if isinstance(value, str):
        return value.strip() or None
    return str(value).strip() or None

def normalize_bool(value):
    if isinstance(value, bool):
        return value
    if value is None:
        return None
    lowered = str(value).strip().lower()
    if lowered in ('1', 'true', 'yes', 'on'):
        return True
    if lowered in ('0', 'false', 'no', 'off'):
        return False
    return None

def normalize_date(value):
    if value is None:
        return None
    if isinstance(value, (int, float)):
        return datetime.fromtimestamp(float(value), tz=timezone.utc).isoformat(timespec='seconds')
    text = normalize_text(value)
    if text is None:
        return None
    try:
        return datetime.fromtimestamp(float(text), tz=timezone.utc).isoformat(timespec='seconds')
    except:
        pass
    try:
        parsed = datetime.fromisoformat(text.replace('Z', '+00:00'))
        if parsed.tzinfo is not None:
            parsed = parsed.astimezone(timezone.utc)
        return parsed.isoformat(timespec='seconds')
    except:
        return text

def normalize_state(item):
    raw = normalize_text(item.get('state') or item.get('status') or item.get('job_state'))
    if raw:
        return raw.lower()
    if item.get('paused_at') is not None:
        return 'paused'
    if normalize_bool(item.get('running')) is True:
        return 'running'
    if normalize_bool(item.get('enabled')) is False:
        return 'paused'
    return 'scheduled'

def normalize_job(item):
    if not isinstance(item, dict):
        return None
    job_id = normalize_text(item.get('id') or item.get('job_id') or item.get('slug'))
    if not job_id:
        return None
    payload = item.get('payload') if isinstance(item.get('payload'), dict) else {}
    prompt = normalize_text(item.get('prompt') or payload.get('prompt') or payload.get('task')) or ''
    name = normalize_text(item.get('name') or item.get('title') or payload.get('name') or prompt.splitlines()[0] if prompt else None) or job_id
    schedule = item.get('schedule') if isinstance(item.get('schedule'), dict) else {}
    expr = normalize_text(schedule.get('expr') or item.get('schedule_expr') or item.get('cron'))
    state = normalize_state(item)

    return {
        'id': job_id,
        'name': name,
        'prompt': prompt,
        'script': normalize_text(item.get('script') or payload.get('script')),
        'workdir': normalize_text(item.get('workdir') or payload.get('workdir')),
        'no_agent': normalize_bool(item.get('no_agent')) or False,
        'schedule_expr': expr,
        'state': state,
        'enabled': normalize_bool(item.get('enabled')) if normalize_bool(item.get('enabled')) is not None else state != 'paused',
        'created_at': normalize_date(item.get('created_at')),
        'next_run_at': normalize_date(item.get('next_run_at')),
        'last_run_at': normalize_date(item.get('last_run_at')),
        'last_status': normalize_text(item.get('last_status')),
        'last_error': normalize_text(item.get('last_error')),
        'deliver': normalize_text(item.get('deliver') or item.get('delivery_target') or payload.get('deliver')),
        'model': normalize_text(item.get('model') or payload.get('model')),
        'provider': normalize_text(item.get('provider') or payload.get('provider')),
    }

try:
    hermes_home = pathlib.Path.home() / '.hermes'
    jobs_path = hermes_home / 'cron' / 'jobs.json'
    if not jobs_path.exists():
        print(json.dumps({'jobs': []}))
    else:
        raw = json.loads(jobs_path.read_text(encoding='utf-8'))
        if isinstance(raw, dict):
            raw_jobs = raw.get('jobs') or raw.get('items') or raw.get('cron_jobs') or []
        elif isinstance(raw, list):
            raw_jobs = raw
        else:
            raw_jobs = []
        jobs = []
        for item in raw_jobs:
            normalized = normalize_job(item)
            if normalized:
                jobs.append(normalized)
        jobs.sort(key=lambda j: (j.get('next_run_at') is None, j.get('next_run_at') or '', j.get('name', '').lower()))
        print(json.dumps({'jobs': jobs}))
except Exception as e:
    print(json.dumps({'error': str(e)}))
";
            var result = await _sshTransport.ExecuteJSONAsync<CronJobListResponse>(pythonScript);
            if (!string.IsNullOrEmpty(result.Error))
                throw new Exception(result.Error);
            return result.Jobs ?? new List<CronJobInfo>();
        }

        public async Task PauseJobAsync(string jobId)
        {
            await ExecuteCronCommandAsync("pause", jobId);
        }

        public async Task ResumeJobAsync(string jobId)
        {
            await ExecuteCronCommandAsync("resume", jobId);
        }

        public async Task RunJobNowAsync(string jobId)
        {
            await ExecuteCronCommandAsync("run", jobId);
        }

        public async Task RemoveJobAsync(string jobId)
        {
            await ExecuteCronCommandAsync("remove", jobId);
        }

        private async Task ExecuteCronCommandAsync(string command, string jobId)
        {
            var pyCmd = JsonSerializer.Serialize(command);
            var pyId = JsonSerializer.Serialize(jobId);
            var pythonScript = @"
import json, os, pathlib, subprocess

job_id = " + pyId + @"
command = " + pyCmd + @"

hermes_binary = shutil.which('hermes')
if not hermes_binary:
    try:
        result = subprocess.run([str(pathlib.Path.home() / '.local' / 'bin' / 'hermes')],
                               capture_output=True, text=True)
        if result.returncode == 0:
            hermes_binary = str(pathlib.Path.home() / '.local' / 'bin' / 'hermes')
    except:
        pass
if not hermes_binary:
    print(json.dumps({'ok': False, 'error': 'hermes CLI not found on remote host'}))
    sys.exit(0)

hermes_home = pathlib.Path.home() / '.hermes'
env = os.environ.copy()
env['HERMES_HOME'] = str(hermes_home)

result = subprocess.run([hermes_binary, 'cron', command, job_id],
                       capture_output=True, text=True, env=env)

if result.returncode != 0:
    msg = (result.stderr or result.stdout or f'hermes cron {command} failed').strip()
    print(json.dumps({'ok': False, 'error': msg}))
else:
    print(json.dumps({'ok': True, 'message': (result.stdout or '').strip() or None}))
";
            var r = await _sshTransport.ExecuteJSONAsync<CronCommandResponse>(pythonScript);
            if (!r.Ok)
                throw new Exception(r.Error ?? $"Cron {command} failed");
        }
    }

    public class CronJobListResponse
    {
        public List<CronJobInfo> Jobs { get; set; }
        public string Error { get; set; }
    }

    public class CronCommandResponse
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
    }
}
