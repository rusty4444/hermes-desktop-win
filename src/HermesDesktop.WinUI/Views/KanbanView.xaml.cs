using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using HermesDesktop.WinUI.ViewModels;

namespace HermesDesktop.WinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class KanbanView : Page
    {
        public KanbanView()
        {
            this.InitializeComponent();
            Loaded += KanbanView_Loaded;
        }

        private async void KanbanView_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadBoardAsync();
        }

        private void AddCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Models.KanbanLane lane)
            {
                // We'll show a simple dialog to get the card details
                // For simplicity, we'll just add a card with a default title
                _ = ViewModel.AddCardAsync(lane.Id, "New Card", "Description");
            }
        }

        private void MoveCard_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement moving a card to another lane
            var dialog = new ContentDialog
            {
                Title = "Move Card",
                Content = "This feature is not yet implemented.",
                CloseButtonText = "OK"
            };
            _ = dialog.ShowAsync();
        }

        private void DeleteCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Models.KanbanCard card)
            {
                _ = ViewModel.DeleteCardAsync(card.Id);
            }
        }
    }
}
