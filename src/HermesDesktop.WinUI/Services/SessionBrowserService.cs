using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Service for browsing Hermes sessions on the remote host.
    /// </summary>
    public class SessionBrowserService
    {
        private readonly SSHTransport _sshTransport;

        public SessionBrowserService(SSHTransport sshTransport)
        {
            _sshTransport = sshTransport ?? throw new ArgumentNullException(nameof(sshTransport));
        }

        /// <summary>
        /// Gets a list of sessions from the remote host.
        /// </summary>
        public async Task<IEnumerable<SessionInfo>> GetSessionsAsync(int limit = 50, int offset = 0)
        {
            // We'll run a remote Python script that returns a JSON array of session objects.
            var pythonScript = @"
import json
import os
import sys

def get_sessions(limit, offset):
    sessions_dir = os.path.expanduser('~/.hermes/sessions')
    if not os.path.isdir(sessions_dir):
        return []

    sessions = []
    for filename in os.listdir(sessions_dir):
        if filename.endswith('.json'):
            filepath = os.path.join(sessions_dir, filename)
            try:
                with open(filepath, 'r') as f:
                    data = json.load(f)
                    # We expect each session file to have at least an 'id' and a 'timestamp'
                    sessions.append({
                        'id': data.get('id'),
                        'timestamp': data.get('timestamp'),
                        'title': data.get('title', 'Untitled'),
                        'messageCount': data.get('messageCount', 0)
                    })
            except Exception:
                # Skip invalid files
                pass

    # Sort by timestamp descending (newest first)
    sessions.sort(key=lambda x: x.get('timestamp', 0), reverse=True)
    return sessions[offset:offset+limit]

if __name__ == '__main__':
    limit = int(sys.argv[1]) if len(sys.argv) > 1 else 50
    offset = int(sys.argv[2]) if len(sys.argv) > 2 else 0
    result = get_sessions(limit, offset)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<List<SessionInfo>>(pythonScript, limit.ToString(), offset.ToString());
            return result;
        }

        /// <summary>
        /// Gets the transcript for a specific session.
        /// </summary>
        public async Task<SessionTranscript> GetSessionTranscriptAsync(string sessionId)
        {
            var pythonScript = $@"
import json
import os
import sys

session_id = '{sessionId}'
sessions_dir = os.path.expanduser('~/.hermes/sessions')
filepath = os.path.join(sessions_dir, session_id + '.json')

if not os.path.isfile(filepath):
    print(json.dumps({{'error': 'Session not found'}}))
    sys.exit(1)

try:
    with open(filepath, 'r') as f:
        data = json.load(f)
    # We expect the transcript to be in a 'messages' field
    transcript = {{
        'id': data.get('id'),
        'title': data.get('title', 'Untitled'),
        'messages': data.get('messages', []),
        'timestamp': data.get('timestamp')
    }}
    print(json.dumps(transcript))
except Exception as e:
    print(json.dumps({{'error': str(e)}}))
";
            var result = await _sshTransport.ExecuteJSONAsync<SessionTranscript>(pythonScript);
            return result;
        }
    }

    public class SessionInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public long Timestamp { get; set; }
        public int MessageCount { get; set; }
    }

    public class SessionTranscript
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public long Timestamp { get; set; }
        public List<SessionMessage> Messages { get; set; } = new List<SessionMessage>();
    }

    public class SessionMessage
    {
        public string Content { get; set; }
        public string Role { get; set; } // e.g., 'user' or 'assistant'
        public long Timestamp { get; set; }
    }
}
