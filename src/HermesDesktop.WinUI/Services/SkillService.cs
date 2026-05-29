using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Service for managing skills on the remote host.
    /// </summary>
    public class SkillService
    {
        private readonly SSHTransport _sshTransport;

        public SkillService(SSHTransport sshTransport)
        {
            _sshTransport = sshTransport ?? throw new ArgumentNullException(nameof(sshTransport));
        }

        /// <summary>
        /// Gets a list of skills (SKILL.md files) from the remote host.
        /// </summary>
        public async Task<List<SkillInfo>> GetSkillsAsync()
        {
            var pythonScript = @"
import json, os, re

def get_skills():
    skills_dir = os.path.expanduser('~/.hermes/skills')
    if not os.path.isdir(skills_dir):
        return []

    skills = []
    for root, dirs, files in os.walk(skills_dir):
        for filename in files:
            if not filename.endswith('.md'):
                continue
            filepath = os.path.join(root, filename)
            try:
                with open(filepath, 'r') as f:
                    content = f.read()

                title = filename
                description = ''

                # Try to parse YAML frontmatter
                if content.startswith('---'):
                    end = content.find('---', 3)
                    if end > 0:
                        fm = content[3:end]
                        for line in fm.split('\n'):
                            line = line.strip()
                            if line.startswith('title:'):
                                title = line[6:].strip().strip(chr(34)).strip(chr(39))
                            elif line.startswith('name:'):
                                title = line[5:].strip().strip(chr(34)).strip(chr(39))
                            elif line.startswith('description:'):
                                description = line[12:].strip().strip(chr(34)).strip(chr(39))

                skills.append({
                    'id': filename,
                    'title': title,
                    'path': filepath,
                    'description': description
                })
            except Exception:
                pass

    return skills

if __name__ == '__main__':
    result = get_skills()
    print(json.dumps(result))
";
            return await _sshTransport.ExecuteJSONAsync<List<SkillInfo>>(pythonScript);
        }

        /// <summary>
        /// Gets the content of a skill by its filename.
        /// </summary>
        public async Task<string> GetSkillContentAsync(string skillId)
        {
            var safeId = JsonSerializer.Serialize(skillId);
            var pythonScript = @"
import json, os, sys

skill_id = " + safeId + @"
skills_dir = os.path.expanduser('~/.hermes/skills')
filepath = os.path.join(skills_dir, skill_id)

if not os.path.isfile(filepath):
    filepath = os.path.join(skills_dir, skill_id + '.SKILL.md')
    if os.path.isfile(filepath):
        pass  # found it
    else:
        # Search recursively
        found = None
        for root, dirs, files in os.walk(skills_dir):
            for fname in files:
                if fname == skill_id or fname == skill_id + '.SKILL.md' or fname == skill_id + '.md':
                    found = os.path.join(root, fname)
                    break
            if found:
                break
        if found:
            filepath = found
        else:
            print(json.dumps({'error': 'Skill not found: ' + skill_id}))
            sys.exit(1)

try:
    with open(filepath, 'r') as f:
        content = f.read()
    print(json.dumps({'content': content}))
except Exception as e:
    print(json.dumps({'error': str(e)}))
";
            var result = await _sshTransport.ExecuteJSONAsync<SkillContentResult>(pythonScript);
            if (!string.IsNullOrEmpty(result.Error))
                throw new IOException(result.Error);
            return result.Content ?? string.Empty;
        }

        /// <summary>
        /// Saves content to a skill file on the remote host.
        /// </summary>
        public async Task SaveSkillAsync(string skillId, string content)
        {
            var safeId = JsonSerializer.Serialize(skillId);
            var safeContent = JsonSerializer.Serialize(content);
            var pythonScript = @"
import json, os

skill_id = " + safeId + @"
content = " + safeContent + @"
skills_dir = os.path.expanduser('~/.hermes/skills')

def save_skill(skill_id, content, skills_dir):
    try:
        os.makedirs(skills_dir, exist_ok=True)

        if not skill_id.endswith('.md'):
            filepath = os.path.join(skills_dir, skill_id + '.SKILL.md')
        else:
            filepath = os.path.join(skills_dir, skill_id)

        with open(filepath, 'w') as f:
            f.write(content)
        return {'success': True, 'error': None}
    except Exception as e:
        return {'success': False, 'error': str(e)}

if __name__ == '__main__':
    result = save_skill(skill_id, content, skills_dir)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<SaveResult>(pythonScript);
            if (!result.Success)
                throw new IOException(result.Error ?? "Unknown error saving skill");
        }

        /// <summary>
        /// Deletes a skill by its filename.
        /// </summary>
        public async Task DeleteSkillAsync(string skillId)
        {
            var safeId = JsonSerializer.Serialize(skillId);
            var pythonScript = @"
import json, os

skill_id = " + safeId + @"
skills_dir = os.path.expanduser('~/.hermes/skills')

def delete_skill(skill_id, skills_dir):
    try:
        filepath = os.path.join(skills_dir, skill_id)
        if not os.path.isfile(filepath):
            filepath = os.path.join(skills_dir, skill_id + '.SKILL.md')
        if os.path.isfile(filepath):
            os.remove(filepath)
            return {'success': True, 'error': None}
        else:
            return {'success': False, 'error': 'Skill not found: ' + skill_id}
    except Exception as e:
        return {'success': False, 'error': str(e)}

if __name__ == '__main__':
    result = delete_skill(skill_id, skills_dir)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<SaveResult>(pythonScript);
            if (!result.Success)
                throw new IOException(result.Error ?? "Unknown error deleting skill");
        }
    }
}
