using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;
using HermesDesktop.WinUI.Services;

namespace HermesDesktop.WinUI.ViewModels
{
    /// <summary>
    /// View model for the Files view.
    /// </summary>
    public class FilesViewModel : INotifyPropertyChanged
    {
        private readonly AppState _appState;
        private ObservableCollection<FileItem> _files = new ObservableCollection<FileItem>();
        private string _currentPath = string.Empty;
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;
        private FileItem _selectedFile = null;
        private string _fileContent = string.Empty;
        private bool _isFileLoading = false;
        private bool _isFileSaving = false;

        public FilesViewModel()
        {
            _appState = AppState.Instance;
            // We'll start at the home directory
            _currentPath = "~";
        }

        public ObservableCollection<FileItem> Files
        {
            get => _files;
            set => SetField(ref _files, value);
        }

        public string CurrentPath
        {
            get => _currentPath;
            set => SetField(ref _currentPath, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }

        public FileItem SelectedFile
        {
            get => _selectedFile;
            set => SetField(ref _selectedFile, value);
        }

        public string FileContent
        {
            get => _fileContent;
            set => SetField(ref _fileContent, value);
        }

        public bool IsFileLoading
        {
            get => _isFileLoading;
            set => SetField(ref _isFileLoading, value);
        }

        public bool IsFileSaving
        {
            get => _isFileSaving;
            set => SetField(ref _isFileSaving, value);
        }

        /// <summary>
        /// Loads the contents of the current directory.
        /// </summary>
        public async Task LoadDirectoryAsync()
        {
            if (_appState.ActiveConnection == null || string.IsNullOrWhiteSpace(_appState.ActiveConnection.EffectiveTarget))
            {
                ErrorMessage = "No connection configured.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                // We'll expand the tilde to the home directory
                var path = _currentPath;
                if (path == "~")
                {
                    path = os.path.expanduser('~');
                }
                else
                {
                    // We'll assume the path is already absolute or relative to the home directory.
                    // We'll just use it as is and let the remote script handle it.
                }

                var pythonScript = $@"
import json
import os
import stat

path = {json.dumps(path)}

def list_directory(path):
    try:
        entries = []
        with os.scandir(path) as it:
            for entry in it:
                stat = entry.stat()
                entries.append({{
                    'name': entry.name,
                    'path': entry.path,
                    'is_dir': entry.is_dir(),
                    'size': stat.st_size,
                    'modified': stat.st_mtime
                }})
        # Sort: directories first, then by name
        entries.sort(key=lambda x: (not x['is_dir'], x['name'].lower()))
        return entries
    except Exception as e:
        return {{'error': str(e)}}

if __name__ == '__main__':
    result = list_directory(path)
    print(json.dumps(result))
";
                var result = await _appState.FileEditorService.ExecuteJSONAsync<DirectoryResult>(pythonScript);
                if (result.Error != null)
                {
                    ErrorMessage = result.Error;
                    Files.Clear();
                }
                else
                {
                    Files.Clear();
                    foreach (var entry in result.Entries)
                    {
                        Files.Add(new FileItem
                        {
                            Name = entry.Name,
                            FullPath = entry.Path,
                            IsDirectory = entry.IsDir,
                            Size = entry.Size,
                            Modified = entry.Modified
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load directory: {ex.Message}";
                Files.Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Navigates to the parent directory.
        /// </summary>
        public async Task GoUpAsync()
        {
            if (CurrentPath == "~" || CurrentPath == "/")
            {
                // Already at the root or home, we can't go up further.
                return;
            }

            // We'll compute the parent path.
            // For simplicity, we'll use a remote script to get the parent directory.
            var pythonScript = $@"
import json
import os

path = {json.dumps(CurrentPath)}

def get_parent(path):
    # Normalize the path
    path = os.path.normpath(path)
    if path == '/' or path == '~':
        return path
    parent = os.path.dirname(path)
    if parent == '':
        return '/'
    return parent

if __name__ == '__main__':
    result = get_parent(path)
    print(json.dumps({{'parent': result}}))
";
            var result = await _appState.FileEditorService.ExecuteJSONAsync<ParentResult>(pythonScript);
            if (result.Parent != null)
            {
                CurrentPath = result.Parent;
                await LoadDirectoryAsync();
            }
        }

        /// <summary>
        /// Loads the content of the selected file.
        /// </summary>
        public async Task LoadFileContentAsync()
        {
            if (SelectedFile == null || SelectedFile.IsDirectory)
            {
                FileContent = string.Empty;
                return;
            }

            IsFileLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                var content = await _appState.FileEditorService.GetFileContentAsync(SelectedFile.FullPath);
                FileContent = content;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load file: {ex.Message}";
                FileContent = string.Empty;
            }
            finally
            {
                IsFileLoading = false;
            }
        }

        /// <summary>
        /// Saves the content of the selected file.
        /// </summary>
        public async Task SaveFileContentAsync()
        {
            if (SelectedFile == null || SelectedFile.IsDirectory)
            {
                ErrorMessage = "Cannot save a directory.";
                return;
            }

            IsFileSaving = true;
            ErrorMessage = string.Empty;
            try
            {
                await _appState.FileEditorService.SaveFileAsync(SelectedFile.FullPath, FileContent);
                // Optionally, we can reload the file to confirm the save.
                await LoadFileContentAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save file: {ex.Message}";
            }
            finally
            {
                IsFileSaving = false;
            }
        }

        /// <summary>
        /// Creates a new file in the current directory.
        /// </summary>
        public async Task CreateNewFileAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                ErrorMessage = "File name cannot be empty.";
                return;
            }

            var newFilePath = System.IO.Path.Combine(CurrentPath, fileName);
            // We'll use the file editor service to create an empty file.
            IsFileSaving = true;
            ErrorMessage = string.Empty;
            try
            {
                await _appState.FileEditorService.SaveFileAsync(newFilePath, string.Empty);
                // After creating the file, we reload the directory to show the new file.
                await LoadDirectoryAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to create file: {ex.Message}";
            }
            finally
            {
                IsFileSaving = false;
            }
        }

        /// <summary>
        /// Deletes the selected file or directory.
        /// </summary>
        public async Task DeleteSelectedAsync()
        {
            if (SelectedFile == null)
            {
                ErrorMessage = "No file selected.";
                return;
            }

            IsFileSaving = true;
            ErrorMessage = string.Empty;
            try
            {
                // We'll use a remote script to delete the file or directory.
                var pythonScript = $@"
import json
import os
import shutil

path = {json.dumps(SelectedFile.FullPath)}

def delete_path(path):
    try:
        if os.path.isdir(path):
            shutil.rmtree(path)
        else:
            os.remove(path)
        return {{'success': true, 'error': None}}
    except Exception as e:
        return {{'success': false, 'error': str(e)}}

if __name__ == '__main__':
    result = delete_path(path)
    print(json.dumps(result))
";
                var result = await _appState.FileEditorService.ExecuteJSONAsync<DeleteResult>(pythonScript);
                if (!result.Success)
                {
                    ErrorMessage = result.Error;
                }
                else
                {
                    // After deleting, we reload the directory.
                    await LoadDirectoryAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete: {ex.Message}";
            }
            finally
            {
                IsFileSaving = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    #region Helper Classes for JSON Results

    public class DirectoryResult
    {
        public List<DirectoryEntry> Entries { get; set; } = new List<DirectoryEntry>();
        public string Error { get; set; }
    }

    public class DirectoryEntry
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDir { get; set; }
        public long Size { get; set; }
        public double Modified { get; set; } // Unix timestamp
    }

    public class ParentResult
    {
        public string Parent { get; set; }
    }

    public class DeleteResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    #endregion
}
