using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Service for getting usage statistics from the remote host.
    /// </summary>
    public class UsageService
    {
        private readonly SSHTransport _sshTransport;

        public UsageService(SSHTransport sshTransport)
        {
            _sshTransport = sshTransport ?? throw new ArgumentNullException(nameof(sshTransport));
        }

        /// <summary>
        /// Gets the usage statistics.
        /// </summary>
        public async Task<UsageStats> GetUsageStatsAsync()
        {
            var pythonScript = @"
import json
import os

def get_usage_stats():
    # We assume the usage data is stored in ~/.hermes/usage/stats.json
    usage_dir = os.path.expanduser('~/.hermes/usage')
    stats_path = os.path.join(usage_dir, 'stats.json')
    if not os.path.isfile(stats_path):
        return {
            'total_sessions': 0,
            'total_messages': 0,
            'total_tokens': 0,
            'top_models': [],
            'recent_sessions': []
        }

    try:
        with open(stats_path, 'r') as f:
            data = json.load(f)
        return data
    except Exception:
        return {
            'total_sessions': 0,
            'total_messages': 0,
            'total_tokens': 0,
            'top_models': [],
            'recent_sessions': []
        }

if __name__ == '__main__':
    result = get_usage_stats()
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<UsageStats>(pythonScript);
            return result;
        }
    }

    public class UsageStats
    {
        public int TotalSessions { get; set; }
        public int TotalMessages { get; set; }
        public int TotalTokens { get; set; }
        public List<string> TopModels { get; set; } = new List<string>();
        public List<RecentSession> RecentSessions { get; set; } = new List<RecentSession>();
    }

    public class RecentSession
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public long Timestamp { get; set; }
        public int MessageCount { get; set; }
        public int TokenCount { get; set; }
    }
}
