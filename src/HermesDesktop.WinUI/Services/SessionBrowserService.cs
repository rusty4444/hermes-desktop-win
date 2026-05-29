using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Services
{
    public class SessionBrowserService
    {
        private readonly SSHTransport _sshTransport;

        public SessionBrowserService(SSHTransport sshTransport)
        {
            _sshTransport = sshTransport ?? throw new ArgumentNullException(nameof(sshTransport));
        }

        public async Task<List<SessionInfo>> GetSessionsAsync(int limit = 50, int offset = 0)
        {
            var pythonScript = PythonHelper.BuildPythonScript(@"
import json, os

def get_sessions(limit, offset):
    sessions_dir = os.path.expanduser('~/.hermes/sessions')
    if not os.path.isdir(sessions_dir):
        return []

    sessions = []
    for filename in sorted(os.listdir(sessions_dir), reverse=True):
        if not filename.endswith('.json'):
            continue
        filepath = os.path.join(sessions_dir, filename)
        try:
            with open(filepath, 'r') as f:
                data = json.load(f)
            sessions.append({
                'id': data.get('id', filename),
                'title': data.get('title', 'Untitled'),
                'timestamp': data.get('timestamp', 0),
                'messageCount': data.get('messageCount', 0)
            })
        except Exception:
            pass

    sessions.sort(key=lambda x: x.get('timestamp', 0), reverse=True)
    return sessions[offset:offset + limit]

if __name__ == '__main__':
    limit = " + limit + @"
    offset = " + offset + @"
    result = get_sessions(limit, offset)
    print(json.dumps(result))
");
            return await _sshTransport.ExecuteJSONAsync<List<SessionInfo>>(pythonScript);
        }

        public async Task<SessionTranscript> GetSessionTranscriptAsync(string sessionId)
        {
            var safeSessionId = JsonSerializer.Serialize(sessionId);
            var pythonScript = PythonHelper.BuildPythonScript(@"
import json, os, sys

session_id = " + safeSessionId + @"
sessions_dir = os.path.expanduser('~/.hermes/sessions')
filepath = os.path.join(sessions_dir, session_id + '.json')

if not os.path.isfile(filepath):
    print(json.dumps({'error': 'Session not found'}))
    sys.exit(1)

try:
    with open(filepath, 'r') as f:
        data = json.load(f)
    transcript = {
        'id': data.get('id'),
        'title': data.get('title', 'Untitled'),
        'timestamp': data.get('timestamp'),
        'messages': data.get('messages', [])
    }
    print(json.dumps(transcript))
except Exception as e:
    print(json.dumps({'error': str(e)}))
");
            return await _sshTransport.ExecuteJSONAsync<SessionTranscript>(pythonScript);
        }
    }

    internal static class PythonHelper
    {
        public static string BuildPythonScript(string template)
        {
            var lines = template.TrimStart('\r', '\n').Split('\n');
            if (lines.Length == 0) return string.Empty;

            var minIndent = int.MaxValue;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var indent = line.Length - line.TrimStart().Length;
                if (indent < minIndent) minIndent = indent;
            }

            var dedented = lines.Select(l =>
                l.Length >= minIndent ? l.Substring(minIndent) : l.TrimStart());

            return string.Join("\n", dedented);
        }
    }
}
