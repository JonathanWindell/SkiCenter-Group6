using BusinessLayer.Controllers;
using EntityLayer;
using PresentationLayer.Resources.Animations;
using System.Windows;
using System.Windows.Controls;

namespace PresentationLayer.Views
{
    public partial class SkiSchoolView : Window
    {
        #region Fields

        private readonly SkiSchoolController _skiSchoolController;
        private bool _isInitializing = true;
        private bool _isFetching = false;

        #endregion

        #region Display items

        public class BookingDropdownItem
        {
            public int BookingID { get; set; }
            public string DisplayText { get; set; }
            public int ActiveWeek { get; set; }
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
            public List<SkiLessonBooking> Participants { get; set; } = new();

            public string AvailabilityDisplay => $"{BookedCount} / {MaxParticipants}";
            public string PriceDisplay => $"{Price:N0} kr";
        }

        #endregion

        #region Constructor and loading

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

        /// <summary>
        /// Data to populate filters. 
        /// </summary>
        private void PopulateFilters()
        {
            var weeks = new List<int> { 51, 52 };
            for (int w = 1; w <= 16; w++) weeks.Add(w);
            CmbWeekNumber.ItemsSource = weeks;
            CmbWeekNumber.SelectedItem = 1;

            var levels = new List<string> { "Alla", "Grön", "Blå", "Röd", "Svart", "Privatlektion" };
            CmbLessonLevel.ItemsSource = levels;
            CmbLessonLevel.SelectedItem = "Alla";
        }

        /// <summary>
        /// Gets every Preliminary, Active and Confirmed booking. 
        /// </summary>
        private void LoadActiveBookings()
        {
            try
            {
                var bookings = _skiSchoolController.GetActiveBookings().ToList();
                var items = new List<BookingDropdownItem>();

                foreach (var b in bookings)
                {
                    int week = 0;

                    // Search for current booking week
                    var acc = b.Accommodations?.FirstOrDefault();
                    if (acc != null)
                    {
                        week = System.Globalization.ISOWeek.GetWeekOfYear(acc.StartDate);
                        if (week == 53) week = 52;
                    }

                    // Create text that is viewed in dropdown
                    string weekText = week > 0 ? $" (v.{week})" : "";

                    items.Add(new BookingDropdownItem
                    {
                        BookingID = b.BookingID,
                        ActiveWeek = week,
                        DisplayText = $"#{b.BookingID} - {b.Customer?.DisplayName}{weekText}"
                    });
                }

                CmbBookings.ItemsSource = items;

                if (items.Any())
                {
                    CmbBookings.SelectedIndex = 0;
                }
            }
            catch
            {
                TxtSelectedBookingInfo.Text = "Inga aktiva bokningar hittades";
            }
        }

        /// <summary>
        /// Gets Ski lesson sessions (asynchronously) and populates view. 
        /// </summary>
        private async Task LoadSessionsAsync()
        {
            if (_isFetching || _isInitializing || CmbWeekNumber.SelectedItem == null)
            {
                return;
            }

            _isFetching = true;

            int selectedWeek = (int)CmbWeekNumber.SelectedItem;
            string selectedLevel = CmbLessonLevel.SelectedItem?.ToString() ?? "Alla";
            int? previouslySelectedSessionId = (DgSessions.SelectedItem as SessionDisplayItem)?.SessionID;

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
                        var participants = s.Participants?.ToList() ?? new List<SkiLessonBooking>();
                        items.Add(new SessionDisplayItem
                        {
                            SessionID = s.SkiLessonSessionID,
                            WeekNumber = s.WeekNumber,
                            Level = lesson.Level,
                            Days = lesson.Days,
                            TimeSlot = s.TimeSlot,
                            BookedCount = participants.Count,
                            MaxParticipants = lesson.MaxParticipants,
                            Price = lesson.Price,
                            Participants = participants
                        });
                    }
                }

                var sortedItems = items.OrderBy(i => i.Level).ThenBy(i => i.TimeSlot).ToList();
                DgSessions.ItemsSource = sortedItems;

                if (previouslySelectedSessionId.HasValue)
                {
                    DgSessions.SelectedItem = sortedItems.FirstOrDefault(i => i.SessionID == previouslySelectedSessionId.Value);
                }
            }
            finally
            {
                _isFetching = false;
                UpdateSummary();
            }
        }

        #endregion

        #region Filters and selection

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

                // Automagisk vecko-väljare (Alternativ A)
                if (selected.ActiveWeek > 0 && CmbWeekNumber.Items.Contains(selected.ActiveWeek))
                {
                    CmbWeekNumber.SelectedItem = selected.ActiveWeek;
                }
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


        /// <summary>
        /// Updates information regarding the chosen Ski lesson booking. 
        /// </summary>
        private void UpdateSummary()
        {
            if (DgSessions.SelectedItem is SessionDisplayItem selectedSession)
            {
                TxtSummaryTitle.Text = $"{selectedSession.Level} ({selectedSession.Days})";
                TxtSummaryTime.Text = $"Vecka {selectedSession.WeekNumber} - {selectedSession.TimeSlot}";
                TxtSummarySpots.Text = $"{selectedSession.MaxParticipants - selectedSession.BookedCount} platser kvar (av {selectedSession.MaxParticipants})";
                TxtSummaryPrice.Text = selectedSession.PriceDisplay;

                // Unlocks Btn if number of participants > 0
                BtnViewParticipants.IsEnabled = selectedSession.BookedCount > 0;

                if (CmbBookings.SelectedItem is BookingDropdownItem selectedBooking)
                {
                    int customerSpots = selectedSession.Participants.Count(p => p.BookingID == selectedBooking.BookingID);
                    TxtCustomerBookedSpots.Text = $"{customerSpots} st";

                    BtnBookSession.IsEnabled = selectedSession.BookedCount < selectedSession.MaxParticipants;
                    BtnCancelSession.IsEnabled = customerSpots > 0;
                }
                else
                {
                    TxtCustomerBookedSpots.Text = "0 st";
                    BtnBookSession.IsEnabled = false;
                    BtnCancelSession.IsEnabled = false;
                }
            }
            else
            {
                TxtSummaryTitle.Text = "Ingen lektion vald";
                TxtSummaryTime.Text = "-";
                TxtSummarySpots.Text = "-";
                TxtCustomerBookedSpots.Text = "0 st";
                TxtSummaryPrice.Text = "-";

                BtnBookSession.IsEnabled = false;
                BtnCancelSession.IsEnabled = false;

                // Locks button if user presses outside of "ViewParticipants" button
                BtnViewParticipants.IsEnabled = false;
            }
        }

        #endregion

        #region Participants and booking

        /// <summary>
        /// Button for booking Ski lesson session to booking.
        /// </summary>
        private void BtnViewParticipants_Click(object sender, RoutedEventArgs e)
        {
            if (DgSessions.SelectedItem is not SessionDisplayItem selectedSession)
                return;

            string participantList = _skiSchoolController.GetFormattedParticipantList(selectedSession.SessionID);

            MessageBox.Show(
                participantList,
                $"Deltagare - {selectedSession.Level}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Button for booking Ski lesson session to booking.
        /// </summary>
        private async void BtnBookSession_Click(object sender, RoutedEventArgs e)
        {
            if (DgSessions.SelectedItem is not SessionDisplayItem selectedSession)
                return;

            if (CmbBookings.SelectedItem is not BookingDropdownItem selectedBooking)
                return;

            // Adds ski lesson Session to Booking
            var result = _skiSchoolController.BookParticipant(selectedBooking.BookingID, selectedSession.SessionID);

            if (result.success)
            {
                MessageBox.Show(result.message, "Lektion bokad", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSessionsAsync();
            }
            else
            {
                MessageBox.Show(result.message, "Fel vid bokning", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Button for cancelling booked ski lesson session. Gets every spot tied to booking and removes 1 spot. 
        /// </summary>
        private async void BtnCancelSession_Click(object sender, RoutedEventArgs e)
        {
            if (DgSessions.SelectedItem is not SessionDisplayItem selectedSession)
                return;

            if (CmbBookings.SelectedItem is not BookingDropdownItem selectedBooking)
                return;

            // Fetch the customers booked spots to cancel.
            var participantRecord = selectedSession.Participants
                .FirstOrDefault(p => p.BookingID == selectedBooking.BookingID);

            if (participantRecord == null)
                return;

            var confirm = MessageBox.Show(
                $"Vill du avboka 1 plats på {selectedSession.Level} (V.{selectedSession.WeekNumber}) för bokning #{selectedBooking.BookingID}?",
                "Bekräfta avbokning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            bool success = _skiSchoolController.CancelParticipant(participantRecord.SkiLessonBookingID);

            if (success)
            {
                MessageBox.Show($"1 plats på {selectedSession.Level} har avbokats från bokning #{selectedBooking.BookingID}.",
                                "Plats avbokad", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadSessionsAsync();
            }
            else
            {
                MessageBox.Show("Kunde inte avboka platsen.", "Fel vid avbokning", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Navigation

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                this.Owner.Show();
            }
            this.Close();
        }

        #endregion
    }
}