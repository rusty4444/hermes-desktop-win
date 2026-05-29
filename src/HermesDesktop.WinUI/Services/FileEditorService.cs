using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Service for editing files on the remote host via SSH.
    /// </summary>
    public class FileEditorService
    {
        private readonly SSHTransport _sshTransport;

        public FileEditorService(SSHTransport sshTransport)
        {
            _sshTransport = sshTransport ?? throw new ArgumentNullException(nameof(sshTransport));
        }

        /// <summary>
        /// Gets the content of a file on the remote host.
        /// </summary>
        public async Task<string> GetFileContentAsync(string filePath)
        {
            var pyPath = JsonSerializer.Serialize(filePath);
            var pythonScript = @"
import json, os

file_path = " + pyPath + @"

def get_file_content(file_path):
    try:
        expanded = os.path.expanduser(file_path)
        with open(expanded, 'r') as f:
            content = f.read()
        return {'content': content, 'error': None}
    except Exception as e:
        return {'content': None, 'error': str(e)}

if __name__ == '__main__':
    result = get_file_content(file_path)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<FileContentResult>(pythonScript);
            if (!string.IsNullOrEmpty(result.Error))
                throw new IOException(result.Error);
            return result.Content ?? string.Empty;
        }

        /// <summary>
        /// Saves content to a file on the remote host.
        /// </summary>
        public async Task SaveFileAsync(string filePath, string content)
        {
            var pyPath = JsonSerializer.Serialize(filePath);
            var pyContent = JsonSerializer.Serialize(content);

            var pythonScript = @"
import json, os

file_path = " + pyPath + @"
content = " + pyContent + @"

def save_file(file_path, content):
    try:
        expanded = os.path.expanduser(file_path)
        directory = os.path.dirname(expanded)
        if directory:
            os.makedirs(directory, exist_ok=True)
        with open(expanded, 'w') as f:
            f.write(content)
        return {'success': True, 'error': None}
    except Exception as e:
        return {'success': False, 'error': str(e)}

if __name__ == '__main__':
    result = save_file(file_path, content)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<SaveFileResult>(pythonScript);
            if (!result.Success)
                throw new IOException(result.Error ?? "Unknown error saving file");
        }

        /// <summary>
        /// Checks if a file exists on the remote host.
        /// </summary>
        public async Task<bool> FileExistsAsync(string filePath)
        {
            var pyPath = JsonSerializer.Serialize(filePath);
            var pythonScript = @"
import json, os

file_path = " + pyPath + @"

def file_exists(file_path):
    expanded = os.path.expanduser(file_path)
    return {'exists': os.path.isfile(expanded)}

if __name__ == '__main__':
    result = file_exists(file_path)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<FileExistsResult>(pythonScript);
            return result.Exists;
        }

        /// <summary>
        /// Lists files and directories in a path on the remote host.
        /// </summary>
        public async Task<List<FileItem>> ListFilesAsync(string directoryPath)
        {
            var pyPath = JsonSerializer.Serialize(directoryPath);
            var pythonScript = @"
import json, os, stat

dir_path = " + pyPath + @"

def list_files(dir_path):
    expanded = os.path.expanduser(dir_path)
    if not os.path.isdir(expanded):
        return []

    items = []
    try:
        for entry in sorted(os.listdir(expanded)):
            full_path = os.path.join(expanded, entry)
            st = os.stat(full_path)
            is_dir = stat.S_ISDIR(st.st_mode)
            items.append({
                'name': entry,
                'fullPath': full_path,
                'isDirectory': is_dir,
                'size': st.st_size,
                'modified': int(st.st_mtime)
            })
    except PermissionError:
        pass

    # Sort: directories first, then alphabetically
    items.sort(key=lambda x: (not x['isDirectory'], x['name'].lower()))
    return items

if __name__ == '__main__':
    result = list_files(dir_path)
    print(json.dumps(result))
";
            return await _sshTransport.ExecuteJSONAsync<List<FileItem>>(pythonScript);
        }

        /// <summary>
        /// Deletes a file on the remote host.
        /// </summary>
        public async Task DeleteFileAsync(string filePath)
        {
            var pyPath = JsonSerializer.Serialize(filePath);
            var pythonScript = @"
import json, os

file_path = " + pyPath + @"

def delete_file(file_path):
    try:
        expanded = os.path.expanduser(file_path)
        if os.path.isfile(expanded):
            os.remove(expanded)
            return {'success': True, 'error': None}
        else:
            return {'success': False, 'error': 'File not found'}
    except Exception as e:
        return {'success': False, 'error': str(e)}

if __name__ == '__main__':
    result = delete_file(file_path)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<SaveFileResult>(pythonScript);
            if (!result.Success)
                throw new IOException(result.Error ?? "Unknown error deleting file");
        }
    }
}
