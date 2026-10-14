using BusinessLayer.Controllers;
using PresentationLayer.Resources.Animations;
using System.Windows;
using System.Windows.Controls;

namespace PresentationLayer.Views
{
    public partial class SkiSchoolView : Window
    {
        private readonly SkiSchoolController _skiSchoolController;
        private readonly Window _parentWindow;

        // ViewModel-wrapper för ren presentation i DataGrid
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

        public SkiSchoolView(SkiSchoolController skiSchoolController, Window parentWindow = null, int? preselectedBookingId = null)
        {
            InitializeComponent();
            _skiSchoolController = skiSchoolController ?? throw new ArgumentNullException(nameof(skiSchoolController));
            _parentWindow = parentWindow;

            if (preselectedBookingId.HasValue && preselectedBookingId.Value > 0)
            {
                TxtBookingId.Text = preselectedBookingId.Value.ToString();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await WindowAnimations.FadeInWindowAsync(this);
            PopulateFilters();
            await LoadSessionsAsync();
        }

        /// <summary>
        /// Populates dropdown filters with weeks and course levels.
        /// </summary>
        private void PopulateFilters()
        {
            // Vintersäsongens veckor
            var weeks = new List<int> { 51, 52 };
            for (int w = 1; w <= 16; w++) weeks.Add(w);
            CmbWeekNumber.ItemsSource = weeks;
            CmbWeekNumber.SelectedItem = 1; // Standard: Vecka 1

            // Nivåer
            var levels = new List<string> { "Alla", "Grön", "Blå", "Röd", "Svart", "Privatlektion" };
            CmbLessonLevel.ItemsSource = levels;
            CmbLessonLevel.SelectedItem = "Alla";
        }

        /// <summary>
        /// Loads available sessions from controller and updates UI.
        /// </summary>
        private async Task LoadSessionsAsync()
        {
            if (CmbWeekNumber.SelectedItem == null) return;

            int selectedWeek = (int)CmbWeekNumber.SelectedItem;
            string selectedLevel = CmbLessonLevel.SelectedItem?.ToString() ?? "Alla";

            // Starta spinner-animation
            await ElementAnimations.ToggleLoadingAnimationAsync(true, MainContentGrid, LoadingOverlay, SpinnerTransform);

            var items = new List<SessionDisplayItem>();

            await Task.Run(() =>
            {
                var allLessons = _skiSchoolController.GetAllLessons().ToList();

                foreach (var lesson in allLessons)
                {
                    if (selectedLevel != "Alla" && lesson.Level != selectedLevel)
                    {
                        continue;
                    }

                    // Hämta pass för vald vecka
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
            });

            DgSessions.ItemsSource = items;

            // Återställ laddningsanimation
            await ElementAnimations.ToggleLoadingAnimationAsync(false, MainContentGrid, LoadingOverlay, SpinnerTransform);

            ClearSummary();
        }

        private async void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await LoadSessionsAsync();
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadSessionsAsync();
        }

        private void DgSessions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgSessions.SelectedItem is SessionDisplayItem selected)
            {
                TxtSummaryTitle.Text = $"{selected.Level} ({selected.Days})";
                TxtSummaryTime.Text = $"Vecka {selected.WeekNumber} - {selected.TimeSlot}";
                TxtSummarySpots.Text = $"{selected.MaxParticipants - selected.BookedCount} platser kvar (av {selected.MaxParticipants})";
                TxtSummaryPrice.Text = selected.PriceDisplay;

                BtnBookSession.IsEnabled = (selected.BookedCount < selected.MaxParticipants);
            }
            else
            {
                ClearSummary();
            }
        }

        private void ClearSummary()
        {
            TxtSummaryTitle.Text = "Ingen lektion vald";
            TxtSummaryTime.Text = "-";
            TxtSummarySpots.Text = "-";
            TxtSummaryPrice.Text = "-";
            BtnBookSession.IsEnabled = false;
        }

        /// <summary>
        /// Handles participant booking against the chosen booking ID.
        /// </summary>
        private void BtnBookSession_Click(object sender, RoutedEventArgs e)
        {
            if (DgSessions.SelectedItem is not SessionDisplayItem selectedSession)
            {
                MessageBox.Show("Vänligen välj ett lektionspass i tabellen.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(TxtBookingId.Text.Trim(), out int bookingId) || bookingId <= 0)
            {
                MessageBox.Show("Ange ett giltigt numeriskt Boknings-ID för att koppla lektionen.", "Valideringsfel", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtBookingId.Focus();
                return;
            }

            bool success = _skiSchoolController.BookParticipant(bookingId, selectedSession.SessionID);

            if (success)
            {
                MessageBox.Show($"Deltagare har bokats till {selectedSession.Level} (V.{selectedSession.WeekNumber}) på bokning #{bookingId}!",
                                "Bokning genomförd", MessageBoxButton.OK, MessageBoxImage.Information);

                // Uppdatera listan så att lediga platser räknas om
                _ = LoadSessionsAsync();
            }
            else
            {
                MessageBox.Show("Det gick inte att genomföra bokningen. Kontrollera att bokningsnumret existerar och att passet inte är fullt.",
                                "Fel vid bokning", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null)
            {
                _parentWindow.Show();
                this.Close();
            }
            else
            {
                this.Close();
            }
        }
    }
}