using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HermesDesktop.WinUI.ViewModels;

namespace HermesDesktop.WinUI.Views
{
    public sealed partial class SkillsView : Page
    {
        public SkillsView()
        {
            this.InitializeComponent();
            Loaded += SkillsView_Loaded;
        }

        private async void SkillsView_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadSkillsAsync();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.LoadSkillsAsync();
        }

        private async void SkillsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is Models.SkillInfo skill)
            {
                ViewModel.SelectedSkill = skill;
                await ViewModel.LoadSkillContentAsync();
                SkillTitleText.Text = skill.Title;
            }
        }

        private async void SaveSkill_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveSkillContentAsync();
        }

        private async void NewSkill_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "New Skill",
                Content = new TextBox
                {
                    PlaceholderText = "Enter skill filename (e.g., my-skill.SKILL.md)",
                    Name = "SkillNameInput"
                },
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel"
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var textBox = dialog.Content as TextBox;
                var skillName = textBox?.Text?.Trim();
                if (!string.IsNullOrEmpty(skillName))
                {
                    await ViewModel.CreateNewSkillAsync(skillName);
                }
            }
        }

        private async void DeleteSkill_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedSkill == null) return;

            var dialog = new ContentDialog
            {
                Title = "Delete Skill",
                Content = $"Are you sure you want to delete '{ViewModel.SelectedSkill.Title}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel"
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteSelectedSkillAsync();
            }
        }
    }
}
