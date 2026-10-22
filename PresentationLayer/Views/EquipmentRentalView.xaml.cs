using BusinessLayer.Controllers;
using EntityLayer;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Lets ski-shop staff find an active room booking and add available equipment to it.
    /// The rental price is added to the booking so it can be paid on the room.
    /// </summary>
    public partial class EquipmentRentalView : Window
    {
        private const string AllCategoriesOption = "Alla kategorier";

        private static readonly Brush ErrorBorderBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
        private static readonly Brush LicenseBorderBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));

        private readonly SkiShopController _skiShopController;

        private List<EquipmentItem> _availableItems = new();
        private readonly List<EquipmentItem> _selectedItems = new();
        private List<Booking> _bookingResults = new();
        private Booking? _selectedBooking;


        // Close the rental window and return to the existing dashboard.
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// A row in the selected equipment table.
        /// </summary>
        private record SelectedItemRow(int EquipmentItemID, string ArticleNumber, string Name, string PriceText);

        public EquipmentRentalView(SkiShopController skiShopController)
        {
            InitializeComponent();
            _skiShopController = skiShopController;

            SearchBookingButton.Click += SearchBookingButton_Click;
            BookingResultsComboBox.SelectionChanged += BookingResultsComboBox_SelectionChanged;
            CategoryFilterComboBox.SelectionChanged += (_, _) => ApplyItemFilter();
            ItemSearchTextBox.TextChanged += (_, _) => ApplyItemFilter();
            AddToRentalButton.Click += AddToRentalButton_Click;
            RemoveFromRentalButton.Click += RemoveFromRentalButton_Click;
            StartDatePicker.SelectedDateChanged += (_, _) => { UpdateRentalSummary(); ApplyItemFilter(); };
            EndDatePicker.SelectedDateChanged += (_, _) => { UpdateRentalSummary(); ApplyItemFilter(); };
            StartDatePicker.SelectedDateChanged += (_, _) => UpdateRentalSummary();
            EndDatePicker.SelectedDateChanged += (_, _) => UpdateRentalSummary();
            LicenseVerifiedCheckBox.Checked += (_, _) => ResetLicenseHighlight();
            ConfirmRentalButton.Click += ConfirmRentalButton_Click;

            Loaded += (_, _) => LoadAvailableItems();
        }

        #region Booking search

        private void SearchBookingButton_Click(object sender, RoutedEventArgs e)
        {
            BookingSearchMessage.Visibility = Visibility.Collapsed;
            BookingResultsComboBox.Visibility = Visibility.Collapsed;

            string searchText = BookingSearchTextBox.Text.Trim();

            if (searchText.Length == 0)
            {
                ShowSearchMessage("Ange ett bokningsnummer eller lägenhetsnummer.");
                return;
            }

            try
            {
                _bookingResults = _skiShopController.FindActiveBookings(searchText).ToList();
            }
            catch (Exception ex)
            {
                ShowSearchMessage($"Kunde inte söka efter bokningar: {ex.Message}");
                return;
            }

            if (_bookingResults.Count == 0)
            {
                SelectBooking(null);
                ShowSearchMessage("Ingen aktiv rumsbokning hittades.");
                return;
            }

            if (_bookingResults.Count == 1)
            {
                SelectBooking(_bookingResults[0]);
                return;
            }

            // Several matches: let the user choose
            BookingResultsComboBox.Items.Clear();
            foreach (Booking booking in _bookingResults)
            {
                BookingResultsComboBox.Items.Add($"Bokning {booking.BookingID} – {booking.Customer?.DisplayName}");
            }

            BookingResultsComboBox.Visibility = Visibility.Visible;
            BookingResultsComboBox.SelectedIndex = 0;
        }

        private void BookingResultsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = BookingResultsComboBox.SelectedIndex;

            if (index >= 0 && index < _bookingResults.Count)
            {
                SelectBooking(_bookingResults[index]);
            }
        }

        private void ShowSearchMessage(string message)
        {
            BookingSearchMessage.Text = message;
            BookingSearchMessage.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Shows the booking's details and sets the default rental period to the rest of the stay.
        /// </summary>
        private void SelectBooking(Booking? booking)
        {
            _selectedBooking = booking;
            _selectedItems.Clear();
            RentalFeedbackTextBlock.Text = string.Empty;

            if (booking == null)
            {
                BookingInfoBorder.Visibility = Visibility.Collapsed;
                EquipmentPanel.IsEnabled = false;
                RefreshItemTables();
                return;
            }

            DateTime stayStart = booking.Accommodations.Min(ba => ba.StartDate).Date;
            DateTime stayEnd = booking.Accommodations.Max(ba => ba.EndDate).Date;

            string rooms = string.Join(", ", booking.Accommodations
                .Select(ba => ba.Accommodation?.AccommodationNumber)
                .Where(number => !string.IsNullOrEmpty(number)));

            BookingTitleTextBlock.Text = $"Bokning {booking.BookingID} – {rooms}";
            BookingCustomerTextBlock.Text = $"Kund: {booking.Customer?.DisplayName}";
            BookingStayTextBlock.Text = $"Vistelse: {stayStart:yyyy-MM-dd} – {stayEnd:yyyy-MM-dd}";
            BookingTotalTextBlock.Text = $"Totalbelopp: {booking.TotalAmount:N2} kr";

            List<string> rentedItems = booking.Rentals
                .SelectMany(r => r.RentalItems.Select(ri => ri.EquipmentItem))
                .Select(item => $"{item.ArticleNumber} ({item.Equipment?.Category})")
                .ToList();

            BookingRentalsTextBlock.Text = rentedItems.Count == 0
                ? "Ingen utrustning hyrd på bokningen."
                : $"Hyrd utrustning: {string.Join(", ", rentedItems)}";

            BookingInfoBorder.Visibility = Visibility.Visible;
            EquipmentPanel.IsEnabled = true;

            // Default period: from today (or the first day of the stay) to the last full day of the stay
            DateTime defaultStart = stayStart > DateTime.Today ? stayStart : DateTime.Today;
            DateTime defaultEnd = stayEnd.AddDays(-1) >= defaultStart ? stayEnd.AddDays(-1) : defaultStart;

            StartDatePicker.DisplayDateStart = defaultStart;
            StartDatePicker.DisplayDateEnd = stayEnd;
            EndDatePicker.DisplayDateStart = defaultStart;
            EndDatePicker.DisplayDateEnd = stayEnd;
            StartDatePicker.SelectedDate = defaultStart;
            EndDatePicker.SelectedDate = defaultEnd;

            RefreshItemTables();
        }

        #endregion

        #region Equipment selection

        private void LoadAvailableItems()
        {
            try
            {
                _availableItems = _skiShopController.GetAvailableItemsForRental().ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte hämta tillgänglig utrustning: {ex.Message}");
                _availableItems = new List<EquipmentItem>();
            }

            string? selectedCategory = CategoryFilterComboBox.SelectedItem as string;

            CategoryFilterComboBox.Items.Clear();
            CategoryFilterComboBox.Items.Add(AllCategoriesOption);

            foreach (string category in _availableItems
                .Select(item => item.Equipment?.Category)
                .OfType<string>()
                .Distinct()
                .OrderBy(category => category))
            {
                CategoryFilterComboBox.Items.Add(category);
            }

            CategoryFilterComboBox.SelectedItem = selectedCategory != null && CategoryFilterComboBox.Items.Contains(selectedCategory)
                ? selectedCategory
                : AllCategoriesOption;

            RefreshItemTables();
        }

        /// <summary>
        /// Shows available items that match the filters and are not already selected.
        /// </summary>
        private void ApplyItemFilter()
        {
            string? category = CategoryFilterComboBox.SelectedItem as string;
            string search = ItemSearchTextBox.Text.Trim();
            HashSet<int> selectedIds = _selectedItems.Select(item => item.EquipmentItemID).ToHashSet();

            // 1. Hämta datumen från UI
            DateTime start = StartDatePicker.SelectedDate ?? DateTime.Today;
            DateTime end = EndDatePicker.SelectedDate ?? DateTime.Today;

            List<EquipmentItem> filtered = _availableItems
                .Where(item => !selectedIds.Contains(item.EquipmentItemID))
                .Where(item => category == null || category == AllCategoriesOption || item.Equipment?.Category == category)
                .Where(item => search.Length == 0
                    || item.ArticleNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (item.Equipment?.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                // 2. NY KOD: Filtrera bort pjäxor som är uthyrda de valda datumen
                .Where(item => _skiShopController.IsItemAvailableForDates(item, start, end))
                .ToList();

            AvailableItemsDataGrid.ItemsSource = filtered;
            AvailableCountTextBlock.Text = $"{filtered.Count} tillgängliga";
        }

        private void AddToRentalButton_Click(object sender, RoutedEventArgs e)
        {
            List<EquipmentItem> chosen = AvailableItemsDataGrid.SelectedItems.OfType<EquipmentItem>().ToList();

            if (chosen.Count == 0)
            {
                RentalFeedbackTextBlock.Text = "Markera utrustning i listan för att lägga till den.";
                return;
            }

            _selectedItems.AddRange(chosen);
            RentalFeedbackTextBlock.Text = string.Empty;
            RefreshItemTables();
        }

        private void RemoveFromRentalButton_Click(object sender, RoutedEventArgs e)
        {
            HashSet<int> removeIds = SelectedItemsDataGrid.SelectedItems
                .OfType<SelectedItemRow>()
                .Select(row => row.EquipmentItemID)
                .ToHashSet();

            _selectedItems.RemoveAll(item => removeIds.Contains(item.EquipmentItemID));
            RefreshItemTables();
        }

        private void RefreshItemTables()
        {
            ApplyItemFilter();
            UpdateRentalSummary();
        }

        /// <summary>
        /// Updates prices, total, and whether the driver's license check is required.
        /// </summary>
        private void UpdateRentalSummary()
        {
            int? days = GetRentalDays();
            decimal total = 0m;
            bool missingPrice = false;
            var rows = new List<SelectedItemRow>();

            foreach (EquipmentItem item in _selectedItems)
            {
                decimal? price = days.HasValue
                    ? SkiShopController.CalculateRentalPrice(item.Equipment?.Prices ?? Enumerable.Empty<EquipmentPriceMatrix>(), days.Value)
                    : null;

                if (price.HasValue)
                {
                    total += price.Value;
                }
                else if (days.HasValue)
                {
                    missingPrice = true;
                }

                rows.Add(new SelectedItemRow(
                    item.EquipmentItemID,
                    item.ArticleNumber,
                    item.Equipment?.DisplayName ?? string.Empty,
                    price.HasValue ? price.Value.ToString("N2") : "–"));
            }

            SelectedItemsDataGrid.ItemsSource = rows;

            if (_selectedItems.Count == 0)
            {
                RentalSummaryTextBlock.Text = "Ingen utrustning vald.";
            }
            else if (!days.HasValue)
            {
                RentalSummaryTextBlock.Text = "Välj en giltig hyresperiod.";
            }
            else
            {
                RentalSummaryTextBlock.Text = $"{days} dag(ar) – totalt {total:N2} kr" +
                    (missingPrice ? " (en eller flera saknar pris)" : string.Empty);
            }

            // The license check is only shown, and required, when a snowmobile is selected
            bool requiresLicense = SkiShopController.RequiresDriverLicense(_selectedItems);
            LicenseBorder.Visibility = requiresLicense ? Visibility.Visible : Visibility.Collapsed;

            if (!requiresLicense)
            {
                LicenseVerifiedCheckBox.IsChecked = false;
                ResetLicenseHighlight();
            }

            ConfirmRentalButton.IsEnabled = _selectedBooking != null && _selectedItems.Count > 0;
        }

        private int? GetRentalDays()
        {
            if (StartDatePicker.SelectedDate is not DateTime start || EndDatePicker.SelectedDate is not DateTime end || end < start)
            {
                return null;
            }

            return SkiShopController.CountRentalDays(start, end);
        }

        private void ResetLicenseHighlight()
        {
            LicenseBorder.BorderBrush = LicenseBorderBrush;
            LicenseBorder.BorderThickness = new Thickness(1);
        }

        #endregion

        #region Confirm rental

        /// <summary>
        /// Validates the input, adds the equipment to the booking and shows a confirmation.
        /// </summary>
        private void ConfirmRentalButton_Click(object sender, RoutedEventArgs e)
        {
            RentalFeedbackTextBlock.Text = string.Empty;

            if (_selectedBooking == null)
            {
                RentalFeedbackTextBlock.Text = "Sök fram en aktiv bokning först.";
                return;
            }

            if (_selectedItems.Count == 0)
            {
                RentalFeedbackTextBlock.Text = "Välj minst en utrustning att hyra ut.";
                return;
            }

            if (StartDatePicker.SelectedDate is not DateTime startDate || EndDatePicker.SelectedDate is not DateTime endDate)
            {
                RentalFeedbackTextBlock.Text = "Välj både start- och slutdatum.";
                return;
            }

            if (endDate < startDate)
            {
                RentalFeedbackTextBlock.Text = "Slutdatum kan inte vara före startdatum.";
                return;
            }

            bool requiresLicense = SkiShopController.RequiresDriverLicense(_selectedItems);
            bool licenseVerified = LicenseVerifiedCheckBox.IsChecked == true;

            if (requiresLicense && !licenseVerified)
            {
                RentalFeedbackTextBlock.Text = "Bekräfta att kundens körkort är kontrollerat innan skoter kan hyras ut.";
                LicenseBorder.BorderBrush = ErrorBorderBrush;
                LicenseBorder.BorderThickness = new Thickness(2);
                return;
            }

            int bookingId = _selectedBooking.BookingID;
            List<EquipmentItem> rentedItems = _selectedItems.ToList();

            (bool success, string message, decimal addedAmount) result;

            try
            {
                result = _skiShopController.AddEquipmentToBooking(
                    bookingId,
                    rentedItems.Select(item => item.EquipmentItemID),
                    startDate,
                    endDate,
                    licenseVerified);
            }
            catch (Exception ex)
            {
                RentalFeedbackTextBlock.Text = $"Ett fel uppstod när uthyrningen skulle sparas: {ex.Message}";
                return;
            }

            if (!result.success)
            {
                RentalFeedbackTextBlock.Text = result.message;
                // Another user may have rented an item in the meantime, so reload the list
                LoadAvailableItems();
                return;
            }

            string itemList = string.Join("\n", rentedItems.Select(item => $"  • {item.ArticleNumber} – {item.Equipment?.DisplayName}"));

            MessageBox.Show(
                $"Utrustningen är kopplad till bokning {bookingId} och betalas på rummet.\n\n" +
                $"{itemList}\n\n" +
                $"Period: {startDate:yyyy-MM-dd} – {endDate:yyyy-MM-dd}\n" +
                $"Tillagt belopp: {result.addedAmount:N2} kr\n\n" +
                result.message,
                "Uthyrning registrerad",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Reload the booking and equipment so the new rental is visible
            _selectedItems.Clear();
            LoadAvailableItems();

            Booking? updated = _skiShopController.FindActiveBookings(bookingId.ToString())
                .FirstOrDefault(b => b.BookingID == bookingId);
            SelectBooking(updated);
        }

        #endregion

    }
}
