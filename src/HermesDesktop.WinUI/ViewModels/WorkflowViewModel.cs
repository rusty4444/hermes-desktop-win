using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;
using HermesDesktop.WinUI.Services;

namespace HermesDesktop.WinUI.ViewModels
{
    /// <summary>
    /// View model for the Workflows view.
    /// </summary>
    public class WorkflowViewModel : INotifyPropertyChanged
    {
        private readonly AppState _appState;
        private ObservableCollection<Workflow> _workflows = new ObservableCollection<Workflow>();
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;
        private Workflow _selectedWorkflow = null;

        public WorkflowViewModel()
        {
            _appState = AppState.Instance;
        }

        public ObservableCollection<Workflow> Workflows
        {
            get => _workflows;
            set => SetField(ref _workflows, value);
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

        public Workflow SelectedWorkflow
        {
            get => _selectedWorkflow;
            set => SetField(ref _selectedWorkflow, value);
        }

        /// <summary>
        /// Loads the workflows from local storage.
        /// </summary>
        public async Task LoadWorkflowsAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                var workflows = await _appState.WorkflowService.GetWorkflowsAsync();
                Workflows.Clear();
                foreach (var workflow in workflows)
                {
                    Workflows.Add(workflow);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load workflows: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Saves the selected workflow.
        /// </summary>
        public async Task SaveWorkflowAsync()
        {
            if (SelectedWorkflow == null)
            {
                ErrorMessage = "No workflow selected.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                await _appState.WorkflowService.SaveWorkflowAsync(SelectedWorkflow);
                await LoadWorkflowsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save workflow: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Deletes the selected workflow.
        /// </summary>
        public async Task DeleteWorkflowAsync()
        {
            if (SelectedWorkflow == null)
            {
                ErrorMessage = "No workflow selected.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                await _appState.WorkflowService.DeleteWorkflowAsync(SelectedWorkflow.Id);
                await LoadWorkflowsAsync();
                SelectedWorkflow = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete workflow: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Executes the selected workflow (launches a terminal with the initial prompt).
        /// </summary>
        public async Task ExecuteWorkflowAsync()
        {
            if (SelectedWorkflow == null)
            {
                ErrorMessage = "No workflow selected.";
                return;
            }

            // We'll set the active connection's Hermes profile to the workflow's profile (if set)
            // and then launch the terminal with the initial prompt.
            // For simplicity, we'll just show a message that this feature is not implemented.
            // In a real implementation, we would navigate to the terminal view and send the initial prompt.
            ErrorMessage = "Executing workflow is not yet implemented.";
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
}
