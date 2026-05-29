using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using HermesDesktop.WinUI.ViewModels;

namespace HermesDesktop.WinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FilesView : Page
    {
        public FilesView()
        {
            this.InitializeComponent();
            Loaded += FilesView_Loaded;
        }

        private async void FilesView_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadDirectoryAsync();
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.GoUpAsync();
        }

        private void PathTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _ = ViewModel.LoadDirectoryAsync();
            }
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.LoadDirectoryAsync();
        }

        private async void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is Models.FileItem file)
                {
                    ViewModel.SelectedFile = file;
                    await ViewModel.LoadFileContentAsync();
                }
            }
            else
            {
                ViewModel.SelectedFile = null;
                ViewModel.FileContent = string.Empty;
            }
        }

        private async void NewFile_Click(object sender, RoutedEventArgs e)
        {
            // We'll show a simple dialog to get the file name.
            // For simplicity, we'll use a TextBox in a ContentDialog.
            var dialog = new ContentDialog
            {
                Title = "New File",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text="File name:" },
                        new TextBox { Name="FileNameTextBox", Width=200 }
                    }
                },
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel"
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var textBox = dialog.Content as TextBox;
                if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    await ViewModel.CreateNewFileAsync(textBox.Text.Trim());
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedFile != null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Delete File",
                    Content = $"Are you sure you want to delete '{ViewModel.SelectedFile.Name}'?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel"
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.DeleteSelectedAsync();
                }
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveFileContentAsync();
        }

        private async void Reload_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadFileContentAsync();
        }

        private async void CreateNewFile_Click(object sender, RoutedEventArgs e)
        {
            // Same as NewFile_Click
            NewFile_Click(sender, e);
        }
    }
}
