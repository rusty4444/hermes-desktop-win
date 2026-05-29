using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;
using HermesDesktop.WinUI.Services;

namespace HermesDesktop.WinUI.ViewModels
{
    public class FilesViewModel : INotifyPropertyChanged
    {
        private readonly AppState _appState;
        private ObservableCollection<FileItem> _files = new ObservableCollection<FileItem>();
        private string _currentPath = "~";
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;
        private FileItem _selectedFile = null;
        private string _fileContent = string.Empty;
        private bool _isFileLoading = false;
        private bool _isFileSaving = false;

        public FilesViewModel()
        {
            _appState = AppState.Instance;
            _currentPath = "~";
        }

        public ObservableCollection<FileItem> Files { get => _files; set => SetField(ref _files, value); }
        public string CurrentPath { get => _currentPath; set => SetField(ref _currentPath, value); }
        public bool IsLoading { get => _isLoading; set => SetField(ref _isLoading, value); }
        public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }
        public FileItem SelectedFile { get => _selectedFile; set => SetField(ref _selectedFile, value); }
        public string FileContent { get => _fileContent; set => SetField(ref _fileContent, value); }
        public bool IsFileLoading { get => _isFileLoading; set => SetField(ref _isFileLoading, value); }
        public bool IsFileSaving { get => _isFileSaving; set => SetField(ref _isFileSaving, value); }

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
                var items = await _appState.FileEditorService.ListFilesAsync(CurrentPath);
                Files.Clear();
                foreach (var item in items)
                    Files.Add(item);
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

        public async Task GoUpAsync()
        {
            if (CurrentPath == "~" || CurrentPath == "/" || CurrentPath == ".")
                return;

            // Compute parent locally
            var normalized = CurrentPath;
            if (normalized.EndsWith("/") && normalized.Length > 1)
                normalized = normalized.TrimEnd('/');

            var lastSlash = normalized.LastIndexOf('/');
            if (lastSlash > 0)
            {
                CurrentPath = normalized.Substring(0, lastSlash);
            }
            else if (lastSlash == 0 && normalized.Length > 1)
            {
                CurrentPath = "/";
            }
            else
            {
                CurrentPath = "~";
            }

            await LoadDirectoryAsync();
        }

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

        public async Task CreateNewFileAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                ErrorMessage = "File name cannot be empty.";
                return;
            }

            var newFilePath = CurrentPath == "~"
                ? "~/" + fileName
                : CurrentPath.TrimEnd('/') + "/" + fileName;

            IsFileSaving = true;
            ErrorMessage = string.Empty;
            try
            {
                await _appState.FileEditorService.SaveFileAsync(newFilePath, string.Empty);
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
                await _appState.FileEditorService.DeleteFileAsync(SelectedFile.FullPath);
                await LoadDirectoryAsync();
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
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
