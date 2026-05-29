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
    public class CronJobsViewModel : INotifyPropertyChanged
    {
        private readonly AppState _appState;
        private ObservableCollection<CronJobInfo> _jobs = new ObservableCollection<CronJobInfo>();
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;
        private CronJobInfo _selectedJob = null;

        public CronJobsViewModel()
        {
            _appState = AppState.Instance;
        }

        public ObservableCollection<CronJobInfo> Jobs { get => _jobs; set => SetField(ref _jobs, value); }
        public bool IsLoading { get => _isLoading; set => SetField(ref _isLoading, value); }
        public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }
        public CronJobInfo SelectedJob { get => _selectedJob; set => SetField(ref _selectedJob, value); }

        public async Task LoadJobsAsync()
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
                var jobs = await _appState.CronBrowserService.ListJobsAsync();
                Jobs.Clear();
                foreach (var job in jobs)
                    Jobs.Add(job);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load cron jobs: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task PauseJobAsync(string jobId)
        {
            try { await _appState.CronBrowserService.PauseJobAsync(jobId); await LoadJobsAsync(); }
            catch (Exception ex) { ErrorMessage = $"Failed to pause: {ex.Message}"; }
        }

        public async Task ResumeJobAsync(string jobId)
        {
            try { await _appState.CronBrowserService.ResumeJobAsync(jobId); await LoadJobsAsync(); }
            catch (Exception ex) { ErrorMessage = $"Failed to resume: {ex.Message}"; }
        }

        public async Task RunNowAsync(string jobId)
        {
            try { await _appState.CronBrowserService.RunJobNowAsync(jobId); await LoadJobsAsync(); }
            catch (Exception ex) { ErrorMessage = $"Failed to run: {ex.Message}"; }
        }

        public async Task RemoveJobAsync(string jobId)
        {
            try { await _appState.CronBrowserService.RemoveJobAsync(jobId); await LoadJobsAsync(); }
            catch (Exception ex) { ErrorMessage = $"Failed to remove: {ex.Message}"; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        protected bool SetField<T>(ref T f, T v, [CallerMemberName] string n = null) { if (EqualityComparer<T>.Default.Equals(f, v)) return false; f = v; OnPropertyChanged(n); return true; }
    }
}
