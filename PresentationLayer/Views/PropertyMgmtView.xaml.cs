using BusinessLayer.Controllers;
using EntityLayer;
using PresentationLayer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PresentationLayer.Views
{
    public partial class PropertyMgmtView : Window
    {
        private readonly PropertyController _propertyController;
        private readonly ObservableCollection<PropertyRow> _properties = new();
        private readonly ICollectionView _propertyView;

        public PropertyMgmtView(PropertyController propertyController)
        {
            InitializeComponent();

            _propertyController = propertyController;

            _propertyView = CollectionViewSource.GetDefaultView(_properties);
            _propertyView.Filter = MatchesFilter;
            PropertyDataGrid.ItemsSource = _propertyView;

            PropertyTypeFilterComboBox.SelectionChanged += FilterComboBox_SelectionChanged;
            PropertyStatusFilterComboBox.SelectionChanged += FilterComboBox_SelectionChanged;
            ResetFiltersButton.Click += ResetFiltersButton_Click;

            LoadData();
        }

        private void LoadData()
        {
            _properties.Clear();

            // Accommodations
            var accommodations = _propertyController.GetAllAccommodations();
            var accommodationStatusOptions = Enum.GetNames<AccommodationStatus>().ToList();

            foreach (var a in accommodations)
            {
                _properties.Add(new PropertyRow
                {
                    Id = a.AccommodationID,
                    PropertyType = "Boende",
                    PropertyNumber = a.AccommodationNumber,
                    Category = a.AccommodationType?.Description ?? a.AccommodationType?.CategoryCode ?? "Standard",
                    Capacity = a.AccommodationType?.NumberOfBeds ?? 0,
                    OriginalStatus = a.Status.ToString(),
                    Status = a.Status.ToString(),
                    AvailableStatuses = accommodationStatusOptions
                });
            }

            // Meetingrooms
            var meetingRooms = _propertyController.GetAllMeetingRooms();
            var meetingRoomStatusOptions = Enum.GetNames<MeetingRoomStatus>().ToList();

            foreach (var m in meetingRooms)
            {
                _properties.Add(new PropertyRow
                {
                    Id = m.MeetingRoomID,
                    PropertyType = "Mötesrum",
                    PropertyNumber = m.MeetingRoomNumber,
                    Category = m.Description,
                    Capacity = m.Capacity,
                    OriginalStatus = m.Status.ToString(),
                    Status = m.Status.ToString(),
                    AvailableStatuses = meetingRoomStatusOptions
                });
            }

            _propertyView.Refresh();
            UpdateCountText();
        }

        private bool MatchesFilter(object item)
        {
            if (item is not PropertyRow row) return false;

            // Filter by type
            if (PropertyTypeFilterComboBox.SelectedItem is ComboBoxItem typeItem)
            {
                string type = typeItem.Content?.ToString() ?? "";
                if (type != "Alla typer" && row.PropertyType != type)
                    return false;
            }

            // Filter by status
            if (PropertyStatusFilterComboBox.SelectedItem is ComboBoxItem statusItem)
            {
                string status = statusItem.Content?.ToString() ?? "";
                if (status != "Alla statusar" && !string.Equals(row.Status, status, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _propertyView?.Refresh();
            UpdateCountText();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            PropertyTypeFilterComboBox.SelectedIndex = 0;
            PropertyStatusFilterComboBox.SelectedIndex = 0;
            _propertyView.Refresh();
            UpdateCountText();
        }

        private void UpdateCountText()
        {
            int count = _propertyView?.Cast<object>().Count() ?? 0;
            PropertyCountTextBlock.Text = $"Antal visade rader: {count}";
        }


        private void ChangePropertyStatusButton_Click(object sender, RoutedEventArgs e)
        {
            // Identifies all rows changed in the view
            var changedRows = _properties.Where(p => p.IsChanged).ToList();

            if (!changedRows.Any())
            {
                MessageBox.Show("Inga ändringar har gjorts.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int successCount = 0;

            foreach (var row in changedRows)
            {
                if (row.PropertyType == "Boende")
                {
                    if (Enum.TryParse<AccommodationStatus>(row.Status, out var newStatus))
                    {
                        if (_propertyController.UpdateAccommodationStatus(row.Id, newStatus))
                            successCount++;
                    }
                }
                else if (row.PropertyType == "Mötesrum")
                {
                    if (Enum.TryParse<MeetingRoomStatus>(row.Status, out var newStatus))
                    {
                        if (_propertyController.UpdateMeetingRoomStatus(row.Id, newStatus))
                            successCount++;
                    }
                }
            }

            MessageBox.Show($"{successCount} av {changedRows.Count} rader sparades!", "Status uppdaterad", MessageBoxButton.OK, MessageBoxImage.Information);

            // Reload list to reset IsChanged
            LoadData();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}