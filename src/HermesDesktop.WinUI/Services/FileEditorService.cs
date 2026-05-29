using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Service for editing files on the remote host.
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
            var pythonScript = $@"
import json
import os
import sys

file_path = {json.dumps(filePath)}

def get_file_content(file_path):
    try:
        with open(file_path, 'r') as f:
            content = f.read()
        return {{'content': content, 'error': None}}
    except Exception as e:
        return {{'content': None, 'error': str(e)}}

if __name__ == '__main__':
    result = get_file_content(file_path)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<FileContentResult>(pythonScript);
            if (result.Error != null)
            {
                throw new IOException(result.Error);
            }
            return result.Content;
        }

        /// <summary>
        /// Saves the content to a file on the remote host.
        /// </summary>
        public async Task SaveFileAsync(string filePath, string content)
        {
            var pythonScript = $@"
import json
import os
import sys

file_path = {json.dumps(filePath)}
content = {json.dumps(content)}

def save_file(file_path, content):
    try:
        # Ensure the directory exists
        directory = os.path.dirname(file_path)
        if directory and not os.path.exists(directory):
            os.makedirs(directory)
        with open(file_path, 'w') as f:
            f.write(content)
        return {{'success': True, 'error': None}}
    except Exception as e:
        return {{'success': False, 'error': str(e)}}

if __name__ == '__main__':
    result = save_file(file_path, content)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<SaveFileResult>(pythonScript);
            if (!result.Success)
            {
                throw new IOException(result.Error);
            }
        }

        /// <summary>
        /// Checks if a file exists on the remote host.
        /// </summary>
        public async Task<bool> FileExistsAsync(string filePath)
        {
            var pythonScript = $@"
import json
import os
import sys

file_path = {json.dumps(filePath)}

def file_exists(file_path):
    return {{'exists': os.path.isfile(file_path)}}

if __name__ == '__main__':
    result = file_exists(file_path)
    print(json.dumps(result))
";
            var result = await _sshTransport.ExecuteJSONAsync<FileExistsResult>(pythonScript);
            return result.Exists;
        }
    }

    public class FileContentResult
    {
        public string Content { get; set; }
        public string Error { get; set; }
    }

    public class SaveFileResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class FileExistsResult
    {
        public bool Exists { get; set; }
    }
}
