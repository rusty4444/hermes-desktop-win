using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using HermesDesktop.WinUI.ViewModels;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WorkflowsView : Page
    {
        public WorkflowsView()
        {
            this.InitializeComponent();
            Loaded += WorkflowsView_Loaded;
        }

        private async void WorkflowsView_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadWorkflowsAsync();
        }

        private void NewWorkflow_Click(object sender, RoutedEventArgs e)
        {
            // Create a new workflow and select it
            var newWorkflow = new Workflow
            {
                Title = "New Workflow",
                HermesProfile = string.Empty,
                InitialPrompt = string.Empty,
                SkillIds = new List<string>()
            };
            ViewModel.Workflows.Add(newWorkflow);
            ViewModel.SelectedWorkflow = newWorkflow;
        }

        private async void SaveWorkflow_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveWorkflowAsync();
        }

        private async void DeleteWorkflow_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.DeleteWorkflowAsync();
        }

        private async void ExecuteWorkflow_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ExecuteWorkflowAsync();
            // For now, we just show the error message (if any) from the view model.
            // In a real implementation, we would navigate to the terminal and send the initial prompt.
            if (!string.IsNullOrEmpty(ViewModel.ErrorMessage))
            {
                var dialog = new ContentDialog
                {
                    Title = "Execute Workflow",
                    Content = ViewModel.ErrorMessage,
                    CloseButtonText = "OK"
                };
                _ = dialog.ShowAsync();
            }
        }
    }
}
