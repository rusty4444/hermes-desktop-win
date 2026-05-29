using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

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
        /// Gets a list of skills from the remote host.
        /// </summary>
        public async Task<IEnumerable<SkillInfo>> GetSkillsAsync()
        {
            var pythonScript = @"
import json
import os

def get_skills():
    skills_dir = os.path.expanduser('~/.hermes/skills')
    if not os.path.isdir(skills_dir):
        return []

    skills = []
    for filename in os.listdir(skills_dir):
        if filename.endswith('.md') or filename.endswith('.SKILL.md'):
            filepath = os.path.join(skills_dir, filename)
            try:
                with open(filepath, 'r') as f:
                    content = f.read()
                # We try to extract the frontmatter (YAML) and the title.
                # For simplicity, we'll just return the filename and the first line as title.
                lines = content.Split('\n');
                string title = filename;
                if (lines.Length > 0 && lines[0].StartsWith('---'))
                {
                    // Simple YAML frontmatter parsing: look for 'title:' or 'name:'
                    for (int i = 1; i < lines.Length; i++)
                    {
                        if (lines[i].Trim().StartsWith('title:'))
                        {
                            title = lines[i].Substring('title:'.Length).Trim();
                            break;
                        }
                        if (lines[i].Trim().StartsWith('name:'))
                        {
                            title = lines[i].Substring('name:'.Length).Trim();
                            break;
                        }
                    }
                }
                skills.Add(new {
                    id = filename,
                    title = title,
                    path = filepath
                });
            }
            catch Exception
            {
                // Skip invalid files
                pass
            }
    return skills;

if __name__ == '__main__':
    result = get_skills()
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<List<SkillInfo>>(pythonScript);
            return result;
        }

        /// <summary>
        /// Gets the content of a skill.
        /// </summary>
        public async Task<string> GetSkillContentAsync(string skillId)
        {
            var pythonScript = $@"
import json
import os
import sys

skill_id = {json.dumps(skillId)}
skills_dir = os.path.expanduser('~/.hermes/skills')
filepath = os.path.join(skills_dir, skill_id)

if not os.path.isfile(filepath):
    // Try with .SKILL.md suffix
    filepath = os.path.join(skills_dir, skill_id + '.SKILL.md')
    if (!os.path.isfile(filepath))
    {
        print(json.dumps({{'error': 'Skill not found'}}))
        sys.exit(1)
    }

try:
    with open(filepath, 'r') as f:
        content = f.read()
    print(json.dumps({{'content': content}}))
catch Exception as e:
    print(json.dumps({{'error': e.ToString()})))
";
            var result = await _sshTransport.ExecuteJSONAsync<SkillContentResult>(pythonScript);
            if (result.Error != null)
            {
                throw new IOException(result.Error);
            }
            return result.Content;
        }

        /// <summary>
        /// Saves the content of a skill.
        /// </summary>
        public async Task SaveSkillAsync(string skillId, string content)
        {
            var pythonScript = $@"
import json
import os
import sys

skill_id = {json.dumps(skillId)}
content = {json.dumps(content)}
skills_dir = os.path.expanduser('~/.hermes/skills')

def save_skill(skill_id, content, skills_dir):
    try:
        // Ensure the directory exists
        if (!os.path.exists(skills_dir))
        {
            os.makedirs(skills_dir)
        }
        filepath = os.path.join(skills_dir, skill_id)
        // If the skill_id doesn't end with .md or .SKILL.md, we add .SKILL.md
        if (!skill_id.EndsWith('.md') && !skill_id.EndsWith('.SKILL.md'))
        {
            filepath = filepath + '.SKILL.md'
        }
        with open(filepath, 'w') as f:
            f.write(content)
        return {{'success': true, 'error': null}}
    catch Exception as e:
        return {{'success': false, 'error': e.ToString()}}

if __name__ == '__main__':
    result = save_skill(skill_id, content, skills_dir)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<SaveSkillResult>(pythonScript);
            if (!result.Success)
            {
                throw new IOException(result.Error);
            }
        }

        /// <summary>
        /// Deletes a skill.
        /// </summary>
        public async Task DeleteSkillAsync(string skillId)
        {
            var pythonScript = $@"
import json
import os
import sys

skill_id = {json.dumps(skillId)}
skills_dir = os.path.expanduser('~/.hermes/skills')

def delete_skill(skill_id, skills_dir):
    try:
        filepath = os.path.join(skills_dir, skill_id)
        if (!os.path.isfile(filepath))
        {
            // Try with .SKILL.md suffix
            filepath = os.path.join(skills_dir, skill_id + '.SKILL.md')
        }
        if (os.path.isfile(filepath))
        {
            os.remove(filepath)
            return {{'success': true, 'error': null}}
        }
        else
        {
            return {{'success': false, 'error': 'Skill not found'}}
        }
    catch Exception as e:
        return {{'success': false, 'error': e.ToString()}}

if __name__ == '__main__':
    result = delete_skill(skill_id, skills_dir)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<DeleteSkillResult>(pythonScript);
            if (!result.Success)
            {
                throw new IOException(result.Error);
            }
        }
    }

    public class SkillInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
    }

    public class SkillContentResult
    {
        public string Content { get; set; }
        public string Error { get; set; }
    }

    public class SaveSkillResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class DeleteSkillResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}
