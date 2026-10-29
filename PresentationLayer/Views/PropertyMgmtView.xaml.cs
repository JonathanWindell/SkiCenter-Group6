using BusinessLayer.Controllers;
using EntityLayer;
using PresentationLayer.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PresentationLayer.Views
{
    public partial class PropertyMgmtView : Window
    {
        #region Fields and constructor

        private readonly PropertyController _propertyController;
        private readonly ObservableCollection<PropertyRow> _properties = new();

        public PropertyMgmtView(PropertyController propertyController)
        {
            InitializeComponent();

            _propertyController = propertyController;

            CollectionViewSource.GetDefaultView(_properties).Filter = MatchesFilters;
            PropertyDataGrid.ItemsSource = _properties;

            PropertyTypeFilterComboBox.SelectionChanged += FilterComboBox_SelectionChanged;
            PropertyStatusFilterComboBox.SelectionChanged += FilterComboBox_SelectionChanged;
            ResetFiltersButton.Click += ResetFiltersButton_Click;

            LoadData();
            PopulateFilters();
        }

        #endregion

        #region Loading and filters

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
                    Type = "Boende",
                    Number = a.AccommodationNumber,
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
                    Type = "Mötesrum",
                    Number = m.MeetingRoomNumber,
                    Category = m.Description,
                    Capacity = m.Capacity,
                    OriginalStatus = m.Status.ToString(),
                    Status = m.Status.ToString(),
                    AvailableStatuses = meetingRoomStatusOptions
                });
            }

            CollectionViewSource.GetDefaultView(_properties)?.Refresh();
            UpdateCountText();
        }

        private void PopulateFilters()
        {
            PropertyTypeFilterComboBox.Items.Clear();
            PropertyTypeFilterComboBox.Items.Add(new ComboBoxItem { Content = "Alla typer" });
            PropertyTypeFilterComboBox.Items.Add(new ComboBoxItem { Content = "Boende" });
            PropertyTypeFilterComboBox.Items.Add(new ComboBoxItem { Content = "Mötesrum" });
            PropertyTypeFilterComboBox.SelectedIndex = 0;

            PropertyStatusFilterComboBox.Items.Clear();
            PropertyStatusFilterComboBox.Items.Add(new ComboBoxItem { Content = "Alla statusar" });

            // Gets unique statusnames from enum
            var allStatuses = Enum.GetNames<AccommodationStatus>()
                .Union(Enum.GetNames<MeetingRoomStatus>())
                .Distinct();

            foreach (var status in allStatuses)
            {
                PropertyStatusFilterComboBox.Items.Add(new ComboBoxItem { Content = status });
            }
            PropertyStatusFilterComboBox.SelectedIndex = 0;
        }

        private bool MatchesFilters(object item)
        {
            if (item is not PropertyRow row)
                return false;

            string? type =
                (PropertyTypeFilterComboBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString();

            string? status =
                (PropertyStatusFilterComboBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString();

            // Both selected filters must match the same booking.
            bool matchesType =
                PropertyTypeFilterComboBox.SelectedIndex <= 0 ||
                row.Type == type;

            bool matchesStatus =
                PropertyStatusFilterComboBox.SelectedIndex <= 0 ||
                row.Status == status;

            return matchesType && matchesStatus;
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(_properties)?.Refresh();
            UpdateCountText();

            if (PropertyDataGrid.Items.Count > 0)
            {
                PropertyDataGrid.ScrollIntoView(PropertyDataGrid.Items[0]);
            }
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            PropertyTypeFilterComboBox.SelectedIndex = 0;
            PropertyStatusFilterComboBox.SelectedIndex = 0;
            UpdateCountText();
        }

        private void UpdateCountText()
        {
            var view = CollectionViewSource.GetDefaultView(_properties);
            int count = view.Cast<PropertyRow>().Count();

            PropertyCountTextBlock.Text = count switch
            {
                0 => "Inga rader matchar filtren.",
                1 => "1 rad hittades",
                _ => $"{count} rader hittades"
            };
        }

        #endregion

        #region Change status

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
                if (row.Type == "Boende")
                {
                    if (Enum.TryParse<AccommodationStatus>(row.Status, out var newAccStatus))
                    {
                        if (_propertyController.UpdateAccommodationStatus(row.Id, newAccStatus))
                            successCount++;
                    }
                }
                else if (row.Type == "Mötesrum")
                {
                    if (Enum.TryParse<MeetingRoomStatus>(row.Status, out var newMeetingStatus))
                    {
                        if (_propertyController.UpdateMeetingRoomStatus(row.Id, newMeetingStatus))
                            successCount++;
                    }
                }
            }

            MessageBox.Show($"Klart! {successCount} av {changedRows.Count} boenden har fått ny status.", "Status uppdaterad", MessageBoxButton.OK, MessageBoxImage.Information);

            // Reload list to reset IsChanged
            LoadData();
        }

        #endregion

        #region Navigation

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}