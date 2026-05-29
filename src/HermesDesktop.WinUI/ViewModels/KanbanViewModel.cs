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
    /// View model for the Kanban view.
    /// </summary>
    public class KanbanViewModel : INotifyPropertyChanged
    {
        private readonly AppState _appState;
        private ObservableCollection<KanbanLane> _lanes = new ObservableCollection<KanbanLane>();
        private ObservableCollection<KanbanCard> _cards = new ObservableCollection<KanbanCard>();
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;

        public KanbanViewModel()
        {
            _appState = AppState.Instance;
        }

        public ObservableCollection<KanbanLane> Lanes
        {
            get => _lanes;
            set => SetField(ref _lanes, value);
        }

        public ObservableCollection<KanbanCard> Cards
        {
            get => _cards;
            set => SetField(ref _cards, value);
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

        /// <summary>
        /// Loads the Kanban board from the remote host.
        /// </summary>
        public async Task LoadBoardAsync()
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
                var board = await _appState.KanbanService.GetBoardAsync();
                Lanes.Clear();
                Cards.Clear();
                foreach (var lane in board.Lanes)
                {
                    lane.Cards = board.Cards.Where(c => c.LaneId == lane.Id).ToList();
                    Lanes.Add(lane);
                }
                foreach (var card in board.Cards)
                {
                    Cards.Add(card);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load Kanban board: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Adds a new lane.
        /// </summary>
        public async Task AddLaneAsync(string title)
        {
            // We don't have a service method for adding a lane, so we'll just add a card in a new lane? 
            // Actually, we need to update the board to add a lane. We'll do it by adding a card with a new lane title?
            // For simplicity, we'll just show a message that this is not implemented.
            ErrorMessage = "Adding lanes is not yet implemented.";
        }

        /// <summary>
        /// Adds a new card to the specified lane.
        /// </summary>
        public async Task AddCardAsync(string laneId, string title, string description = null)
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                await _appState.KanbanService.CreateCardAsync(laneId, title, description);
                await LoadBoardAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to add card: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates a card (e.g., move to another lane, update title/description).
        /// </summary>
        public async Task UpdateCardAsync(string cardId, string laneId = null, string title = null, string description = null)
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                await _appState.KanbanService.UpdateCardAsync(cardId, laneId, title, description);
                await LoadBoardAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to update card: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Deletes a card.
        /// </summary>
        public async Task DeleteCardAsync(string cardId)
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                await _appState.KanbanService.DeleteCardAsync(cardId);
                await LoadBoardAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete card: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
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
