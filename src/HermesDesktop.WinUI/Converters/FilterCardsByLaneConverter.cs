using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.UI.Xaml.Data;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Converters
{
    public class FilterCardsByLaneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ObservableCollection<KanbanCard> cards && parameter is string laneId)
            {
                // Return a list of cards that belong to the given laneId
                var filtered = new ObservableCollection<KanbanCard>();
                foreach (var card in cards)
                {
                    if (card.LaneId == laneId)
                    {
                        filtered.Add(card);
                    }
                }
                return filtered;
            }
            return new ObservableCollection<KanbanCard>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
