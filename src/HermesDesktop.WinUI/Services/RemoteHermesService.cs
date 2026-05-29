using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Services
{
    public class RemoteHermesService
    {
        private readonly SSHTransport _sshTransport;

        public RemoteHermesService(SSHTransport sshTransport)
        {
            _sshTransport = sshTransport ?? throw new ArgumentNullException(nameof(sshTransport));
        }

        public async Task<RemoteDiscovery> DiscoverAsync(ConnectionProfile connection)
        {
            var pyHermesHome = JsonSerializer.Serialize(connection.HermesHomePath ?? "~/.hermes");
            var pyProfile = JsonSerializer.Serialize(connection.HermesProfile ?? "default");
            var pythonScript = @"
import json, os, pathlib

def tilde(p, home):
    s = str(p)
    if s.startswith(str(home)):
        return '~' + s[len(str(home)):]
    return s

try:
    home = pathlib.Path.home()
    hermes_home = pathlib.Path(" + pyHermesHome + @")
    if not hermes_home.is_absolute():
        hermes_home = home / hermes_home

    # Discover profiles
    profiles = [{'name': 'default', 'path': tilde(home / '.hermes', home), 'is_default': True, 'exists': (home / '.hermes').exists()}]
    profiles_dir = home / '.hermes' / 'profiles'
    if profiles_dir.exists():
        for d in sorted(profiles_dir.iterdir()):
            if d.is_dir():
                profiles.append({'name': d.name, 'path': tilde(d, home), 'is_default': False, 'exists': True})

    result = {
        'ok': True,
        'remote_home': tilde(home, home),
        'hermes_home': tilde(hermes_home, home),
        'active_profile': {'name': " + pyProfile + @", 'path': tilde(hermes_home, home)},
        'available_profiles': profiles,
        'has_sessions': (hermes_home / 'sessions').exists(),
        'has_kanban': (home / '.hermes' / 'kanban.db').exists(),
        'has_cron': (hermes_home / 'cron' / 'jobs.json').exists(),
        'has_skills': (hermes_home / 'skills').exists(),
        'hermes_version': None,
    }

    # Try to get hermes version
    import subprocess, shutil
    hb = shutil.which('hermes')
    if not hb:
        hb = str(home / '.local' / 'bin' / 'hermes')
    try:
        r = subprocess.run([hb, '--version'], capture_output=True, text=True, timeout=10)
        if r.returncode == 0:
            result['hermes_version'] = r.stdout.strip()
    except:
        pass

    print(json.dumps(result))
except Exception as e:
    print(json.dumps({'ok': False, 'error': str(e)}))
";
            return await _sshTransport.ExecuteJSONAsync<RemoteDiscovery>(pythonScript);
        }
    }

    public class RemoteDiscovery
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public string RemoteHome { get; set; }
        public string HermesHome { get; set; }
        public string HermesVersion { get; set; }
        public bool HasSessions { get; set; }
        public bool HasKanban { get; set; }
        public bool HasCron { get; set; }
        public bool HasSkills { get; set; }
        public List<ProfileInfo> AvailableProfiles { get; set; }
    }

    public class ProfileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDefault { get; set; }
        public bool Exists { get; set; }
    }
}
