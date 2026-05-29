using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;

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
}
