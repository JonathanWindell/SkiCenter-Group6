using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BusinessLayer.Controllers;
using EntityLayer;
using PresentationLayer.Resources.Animations;

namespace PresentationLayer.Views
{
    public partial class SkiSchoolView : Window
    {
        private readonly SkiSchoolController _skiSchoolController;
        private bool _isInitializing = true;
        private bool _isFetching = false;

        public class BookingDropdownItem
        {
            public int BookingID { get; set; }
            public string DisplayText { get; set; }
            public override string ToString() => DisplayText;
        }

        public class SessionDisplayItem
        {
            public int SessionID { get; set; }
            public int WeekNumber { get; set; }
            public string Level { get; set; }
            public string Days { get; set; }
            public string TimeSlot { get; set; }
            public int BookedCount { get; set; }
            public int MaxParticipants { get; set; }
            public decimal Price { get; set; }

            public string AvailabilityDisplay => $"{BookedCount} / {MaxParticipants}";
            public string PriceDisplay => $"{Price:N0} kr";
        }

        public SkiSchoolView(SkiSchoolController skiSchoolController)
        {
            InitializeComponent();
            _skiSchoolController = skiSchoolController ?? throw new ArgumentNullException(nameof(skiSchoolController));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await WindowAnimations.FadeInWindowAsync(this);

            PopulateFilters();
            LoadActiveBookings();

            _isInitializing = false;
            await LoadSessionsAsync();
        }

        private void PopulateFilters()
        {
            var weeks = new List<int> { 51, 52 };
            for (int w = 1; w <= 16; w++) weeks.Add(w);
            CmbWeekNumber.ItemsSource = weeks;
            CmbWeekNumber.SelectedItem = 1; // Week 1 as default

            var levels = new List<string> { "Alla", "Grön", "Blå", "Röd", "Svart", "Privatlektion" };
            CmbLessonLevel.ItemsSource = levels;
            CmbLessonLevel.SelectedItem = "Alla";
        }

        /// <summary>
        /// Populates the booking dropdown with real bookings from the database.
        /// </summary>
        private void LoadActiveBookings()
        {
            try
            {
                var bookings = _skiSchoolController.GetActiveBookings().ToList();
                var items = bookings.Select(b => new BookingDropdownItem
                {
                    BookingID = b.BookingID,
                    DisplayText = $"#{b.BookingID} - {b.Customer?.DisplayName} ({b.BookingStatus})"
                }).ToList();

                CmbBookings.ItemsSource = items;

                if (items.Any())
                {
                    CmbBookings.SelectedIndex = 0;
                }
            }
            catch
            {
                // If customer is missing or list is empty
                TxtSelectedBookingInfo.Text = "Inga aktiva bokningar hittades";
            }
        }

        private async Task LoadSessionsAsync()
        {
            if (_isFetching || _isInitializing || CmbWeekNumber.SelectedItem == null)
            {
                return;
            }

            _isFetching = true;

            int selectedWeek = (int)CmbWeekNumber.SelectedItem;
            string selectedLevel = CmbLessonLevel.SelectedItem?.ToString() ?? "Alla";

            await ElementAnimations.ToggleLoadingAnimationAsync(true, MainContentGrid, LoadingOverlay, SpinnerTransform);

            var items = new List<SessionDisplayItem>();

            try
            {
                var allLessons = await Task.Run(() => _skiSchoolController.GetAllLessonsWithSessions().ToList());

                foreach (var lesson in allLessons)
                {
                    if (selectedLevel != "Alla" && lesson.Level != selectedLevel)
                    {
                        continue;
                    }

                    var matchingSessions = lesson.Sessions
                        .Where(s => s.WeekNumber == selectedWeek)
                        .ToList();

                    foreach (var s in matchingSessions)
                    {
                        int booked = s.Participants?.Count ?? 0;
                        items.Add(new SessionDisplayItem
                        {
                            SessionID = s.SkiLessonSessionID,
                            WeekNumber = s.WeekNumber,
                            Level = lesson.Level,
                            Days = lesson.Days,
                            TimeSlot = s.TimeSlot,
                            BookedCount = booked,
                            MaxParticipants = lesson.MaxParticipants,
                            Price = lesson.Price
                        });
                    }
                }

                // Sort after level and time windows
                DgSessions.ItemsSource = items.OrderBy(i => i.Level).ThenBy(i => i.TimeSlot).ToList();
            }
            finally
            {
                await ElementAnimations.ToggleLoadingAnimationAsync(false, MainContentGrid, LoadingOverlay, SpinnerTransform);
                _isFetching = false;
                UpdateSummary();
            }
        }

        private async void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitializing)
            {
                await LoadSessionsAsync();
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadSessionsAsync();
        }

        private void CmbBookings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbBookings.SelectedItem is BookingDropdownItem selected)
            {
                TxtSelectedBookingInfo.Text = selected.DisplayText;
            }
            else
            {
                TxtSelectedBookingInfo.Text = "Ingen bokning vald";
            }
            UpdateSummary();
        }

        private void DgSessions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (DgSessions.SelectedItem is SessionDisplayItem selectedSession)
            {
                TxtSummaryTitle.Text = $"{selectedSession.Level} ({selectedSession.Days})";
                TxtSummaryTime.Text = $"Vecka {selectedSession.WeekNumber} - {selectedSession.TimeSlot}";
                TxtSummarySpots.Text = $"{selectedSession.MaxParticipants - selectedSession.BookedCount} platser kvar (av {selectedSession.MaxParticipants})";
                TxtSummaryPrice.Text = selectedSession.PriceDisplay;

                bool hasBooking = CmbBookings.SelectedItem is BookingDropdownItem;
                bool hasSpots = selectedSession.BookedCount < selectedSession.MaxParticipants;

                BtnBookSession.IsEnabled = hasBooking && hasSpots;
            }
            else
            {
                TxtSummaryTitle.Text = "Ingen lektion vald";
                TxtSummaryTime.Text = "-";
                TxtSummarySpots.Text = "-";
                TxtSummaryPrice.Text = "-";
                BtnBookSession.IsEnabled = false;
            }
        }

        private async void BtnBookSession_Click(object sender, RoutedEventArgs e)
        {
            if (DgSessions.SelectedItem is not SessionDisplayItem selectedSession)
            {
                MessageBox.Show("Vänligen välj ett lektionspass i tabellen.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (CmbBookings.SelectedItem is not BookingDropdownItem selectedBooking)
            {
                MessageBox.Show("Välj en bokning i rullgardinsmenyn att koppla lektionen till.", "Bokning saknas", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool success = _skiSchoolController.BookParticipant(selectedBooking.BookingID, selectedSession.SessionID);

            if (success)
            {
                MessageBox.Show($"Deltagare har bokats till {selectedSession.Level} (V.{selectedSession.WeekNumber}) på bokning #{selectedBooking.BookingID}!",
                                "Bokning genomförd", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadSessionsAsync();
            }
            else
            {
                MessageBox.Show("Det gick inte att genomföra bokningen. Kontrollera att passet inte blev fullbokat.",
                                "Fel vid bokning", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                this.Owner.Show();
            }
            this.Close();
        }
    }
}