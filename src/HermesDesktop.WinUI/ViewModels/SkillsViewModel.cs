using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;
using HermesDesktop.WinUI.Services;

namespace HermesDesktop.WinUI.ViewModels
{
    /// <summary>
    /// View model for the Skills view.
    /// </summary>
    public class SkillsViewModel : INotifyPropertyChanged
    {
        private readonly AppState _appState;
        private ObservableCollection<SkillInfo> _skills = new ObservableCollection<SkillInfo>();
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;
        private SkillInfo _selectedSkill = null;
        private string _skillContent = string.Empty;
        private bool _isContentLoading = false;
        private bool _isContentSaving = false;

        public SkillsViewModel()
        {
            _appState = AppState.Instance;
        }

        public ObservableCollection<SkillInfo> Skills
        {
            get => _skills;
            set => SetField(ref _skills, value);
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

        public SkillInfo SelectedSkill
        {
            get => _selectedSkill;
            set => SetField(ref _selectedSkill, value);
        }

        public string SkillContent
        {
            get => _skillContent;
            set => SetField(ref _skillContent, value);
        }

        public bool IsContentLoading
        {
            get => _isContentLoading;
            set => SetField(ref _isContentLoading, value);
        }

        public bool IsContentSaving
        {
            get => _isContentSaving;
            set => SetField(ref _isContentSaving, value);
        }

        /// <summary>
        /// Loads the list of skills from the remote host.
        /// </summary>
        public async Task LoadSkillsAsync()
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
                var skills = await _appState.SkillService.GetSkillsAsync();
                Skills.Clear();
                foreach (var skill in skills)
                {
                    Skills.Add(skill);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load skills: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Loads the content of the selected skill.
        /// </summary>
        public async Task LoadSkillContentAsync()
        {
            if (SelectedSkill == null)
            {
                SkillContent = string.Empty;
                return;
            }

            IsContentLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                var content = await _appState.SkillService.GetSkillContentAsync(SelectedSkill.Id);
                SkillContent = content;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load skill content: {ex.Message}";
                SkillContent = string.Empty;
            }
            finally
            {
                IsContentLoading = false;
            }
        }

        /// <summary>
        /// Saves the content of the selected skill.
        /// </summary>
        public async Task SaveSkillContentAsync()
        {
            if (SelectedSkill == null)
            {
                ErrorMessage = "No skill selected.";
                return;
            }

            IsContentSaving = true;
            ErrorMessage = string.Empty;
            try
            {
                await _appState.SkillService.SaveSkillAsync(SelectedSkill.Id, SkillContent);
                // Optionally, we can reload the skill to confirm the save.
                await LoadSkillContentAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save skill content: {ex.Message}";
            }
            finally
            {
                IsContentSaving = false;
            }
        }

        /// <summary>
        /// Creates a new skill.
        /// </summary>
        public async Task CreateNewSkillAsync(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                ErrorMessage = "Skill ID cannot be empty.";
                return;
            }

            IsContentSaving = true;
            ErrorMessage = string.Empty;
            try
            {
                // We'll create an empty skill file.
                await _appState.SkillService.SaveSkillAsync(skillId, string.Empty);
                // After creating the skill, we reload the list to show the new skill.
                await LoadSkillsAsync();
                // We select the newly created skill.
                var newSkill = Skills.FirstOrDefault(s => s.Id == skillId);
                if (newSkill != null)
                {
                    SelectedSkill = newSkill;
                    await LoadSkillContentAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to create skill: {ex.Message}";
            }
            finally
            {
                IsContentSaving = false;
            }
        }

        /// <summary>
        /// Deletes the selected skill.
        /// </summary>
        public async Task DeleteSelectedSkillAsync()
        {
            if (SelectedSkill == null)
            {
                ErrorMessage = "No skill selected.";
                return;
            }

            IsContentSaving = true;
            ErrorMessage = string.Empty;
            try
            {
                await _appState.SkillService.DeleteSkillAsync(SelectedSkill.Id);
                // After deleting, we reload the list.
                await LoadSkillsAsync();
                SelectedSkill = null;
                SkillContent = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete skill: {ex.Message}";
            }
            finally
            {
                IsContentSaving = false;
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
}
