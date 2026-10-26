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
        private Customer? _selectedDropInCustomer;
        private Rental? _editingRental;

        private readonly List<ReturnQueueRow> _returnQueue = new();
        private EquipmentItem? _scannedReturnItem;

        /// <summary>
        /// A row in the selected equipment table.
        /// </summary>
        private record SelectedItemRow(int EquipmentItemID, string ArticleNumber, string Name, string PriceText);

        private record ReturnQueueRow(int EquipmentItemID, string ArticleNumber, string Description, EquipmentCondition Condition, bool IsDamaged, string StatusText);


        // Close the rental window and return to the existing dashboard.
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

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
            // A new period changes both the price and which items are free during it
            StartDatePicker.SelectedDateChanged += (_, _) => { UpdateRentalSummary(); ApplyItemFilter(); };
            EndDatePicker.SelectedDateChanged += (_, _) => { UpdateRentalSummary(); ApplyItemFilter(); };
            LicenseVerifiedCheckBox.Checked += (_, _) => ResetLicenseHighlight();
            ConfirmRentalButton.Click += ConfirmRentalButton_Click;

            SearchReturnItemButton.Click += SearchReturnItemButton_Click;
            AddScannedItemToListButton.Click += AddScannedItemToListButton_Click;
            RemoveFromReturnQueueButton.Click += RemoveFromReturnQueueButton_Click;
            ConfirmAllReturnsButton.Click += ConfirmAllReturnsButton_Click;

            ReturnConditionComboBox.ItemsSource = Enum.GetValues(typeof(EquipmentCondition));
            ReturnConditionComboBox.SelectedItem = EquipmentCondition.BarelyUsed;

            Loaded += (_, _) => LoadAvailableItems();
        }

        #region Mode-switch

        private void RentalType_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            CancelEditRentalMode();
            _selectedBooking = null;
            _selectedDropInCustomer = null;
            _selectedItems.Clear();
            BookingSearchTextBox.Clear();
            BookingSearchMessage.Visibility = Visibility.Collapsed;
            BookingResultsComboBox.Visibility = Visibility.Collapsed;
            BookingInfoBorder.Visibility = Visibility.Collapsed;
            EquipmentPanel.IsEnabled = false;

            if (RoomGuestRadioButton.IsChecked == true)
            {
                SearchHeaderTextBlock.Text = "Bokningsnummer eller lägenhetsnummer";
                ConfirmRentalButton.Content = "Lägg till på bokningen";
            }
            else
            {
                SearchHeaderTextBlock.Text = "Kund (telefonnummer, e-post eller namn)";
                ConfirmRentalButton.Content = "Slutför & Betala i kassa";
            }

            RefreshItemTables();
        }

        #endregion

        #region Booking search

        private void SearchBookingButton_Click(object sender, RoutedEventArgs e)
        {
            BookingSearchMessage.Visibility = Visibility.Collapsed;
            BookingResultsComboBox.Visibility = Visibility.Collapsed;

            string searchText = BookingSearchTextBox.Text.Trim();

            if (searchText.Length == 0)
            {
                ShowSearchMessage(RoomGuestRadioButton.IsChecked == true
                    ? "Ange ett bokningsnummer eller lägenhetsnummer."
                    : "Ange telefonnummer, e-post eller namn.");
                return;
            }

            if (RoomGuestRadioButton.IsChecked == true)
            {
                SearchRoomBooking(searchText);
            }
            else
            {
                SearchDropInCustomer(searchText);
            }
        }

        private void SearchRoomBooking(string searchText)
        {
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

            BookingResultsComboBox.Items.Clear();
            foreach (Booking booking in _bookingResults)
            {
                BookingResultsComboBox.Items.Add($"Bokning {booking.BookingID} – {booking.Customer?.DisplayName}");
            }

            BookingResultsComboBox.Visibility = Visibility.Visible;
            BookingResultsComboBox.SelectedIndex = 0;
        }

        private void SearchDropInCustomer(string searchText)
        {
            Customer? customer = _skiShopController.SearchCustomer(searchText);

            if (customer == null)
            {
                SelectCustomer(null);
                ShowSearchMessage("Ingen kund matchade sökningen.");
                return;
            }

            SelectCustomer(customer);
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
            CancelEditRentalMode();

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
                .Where(r => r.Status == "Active")
                .SelectMany(r => r.RentalItems)
                .Where(ri => ri.EquipmentItem != null && ri.EquipmentItem.Status == EquipmentStatus.Rented)
                .Select(ri => $"{ri.EquipmentItem.ArticleNumber} ({ri.EquipmentItem.Equipment?.Category})")
                .ToList();

            bool hasActiveRental = booking.Rentals.Any(r => r.Status == "Active");
            EditExistingRentalButton.Visibility = hasActiveRental ? Visibility.Visible : Visibility.Collapsed;

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

        private void SelectCustomer(Customer? customer)
        {
            _selectedDropInCustomer = customer;
            _selectedItems.Clear();
            RentalFeedbackTextBlock.Text = string.Empty;

            if (customer == null)
            {
                BookingInfoBorder.Visibility = Visibility.Collapsed;
                EquipmentPanel.IsEnabled = false;
                RefreshItemTables();
                return;
            }

            BookingTitleTextBlock.Text = "Drop-in Uthyrning";
            BookingCustomerTextBlock.Text = $"Kund: {customer.DisplayName}";
            BookingStayTextBlock.Text = $"Telefon: {customer.PhoneNumber}";
            BookingTotalTextBlock.Text = "Betalas i kassan";
            BookingRentalsTextBlock.Text = "Utrustning betalas direkt via kort/Swish.";
            EditExistingRentalButton.Visibility = Visibility.Collapsed;

            BookingInfoBorder.Visibility = Visibility.Visible;
            EquipmentPanel.IsEnabled = true;

            // Dates for drop-in
            StartDatePicker.DisplayDateStart = DateTime.Today;
            StartDatePicker.DisplayDateEnd = null;
            EndDatePicker.DisplayDateStart = DateTime.Today;
            EndDatePicker.DisplayDateEnd = null;
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;

            RefreshItemTables();
        }

        #endregion

        #region Edit rental mode

        private void EditExistingRentalButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBooking == null) return;

            var activeRentals = _selectedBooking.Rentals.Where(r => r.Status == "Active").ToList();
            if (!activeRentals.Any()) return;

            _editingRental = activeRentals.First();

            _selectedItems.Clear();

            var allItems = activeRentals
                .SelectMany(r => r.RentalItems)
                .Select(ri => ri.EquipmentItem)
                .Where(ei => ei != null);

            _selectedItems.AddRange(allItems!);

            StartDatePicker.SelectedDate = _editingRental.StartDate;
            EndDatePicker.SelectedDate = _editingRental.EndDate;
            LicenseVerifiedCheckBox.IsChecked = activeRentals.Any(r => r.LicenseVerified);

            ConfirmRentalButton.Content = "Spara ändringar";
            CancelEditRentalButton.Visibility = Visibility.Visible;
            EditExistingRentalButton.IsEnabled = false;

            RefreshItemTables();
        }

        private void CancelEditRentalButton_Click(object sender, RoutedEventArgs e)
        {
            CancelEditRentalMode();
            _selectedItems.Clear();
            RefreshItemTables();
        }

        private void CancelEditRentalMode()
        {
            _editingRental = null;
            ConfirmRentalButton.Content = RoomGuestRadioButton.IsChecked == true
                ? "Lägg till på bokningen"
                : "Slutför & Betala i kassa";
            CancelEditRentalButton.Visibility = Visibility.Collapsed;
            EditExistingRentalButton.IsEnabled = true;
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

            bool hasTarget = (_selectedBooking != null || _selectedDropInCustomer != null);
            bool allowEmptyListInEdit = _editingRental != null;
            ConfirmRentalButton.IsEnabled = hasTarget && (allowEmptyListInEdit || _selectedItems.Count > 0) && days.HasValue;
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

            if (_selectedItems.Count == 0 && _editingRental == null)
            {
                RentalFeedbackTextBlock.Text = "Välj minst en utrustning att hyra ut.";
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

            List<int> itemIds = _selectedItems.Select(item => item.EquipmentItemID).ToList();

            // Edit a existing rental
            if (_editingRental != null && _selectedBooking != null)
            {
                var result = _skiShopController.UpdateBookingRental(
                    _selectedBooking.BookingID,
                    _editingRental.RentalID,
                    itemIds,
                    startDate,
                    endDate,
                    licenseVerified);

                if (!result.Success)
                {
                    RentalFeedbackTextBlock.Text = result.Message;
                    return;
                }

                MessageBox.Show(
                    $"{result.Message}\nPrisjustering på rummet: {result.PriceDifference:+0.00;-0.00;0.00} kr",
                    "Uthyrning uppdaterad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                int bId = _selectedBooking.BookingID;
                CancelEditRentalMode();
                _selectedItems.Clear();
                LoadAvailableItems();

                var updated = _skiShopController.FindActiveBookings(bId.ToString()).FirstOrDefault(b => b.BookingID == bId);
                SelectBooking(updated);
                return;
            }

            // Drop-in customer, pays at the register
            if (DropInRadioButton.IsChecked == true)
            {
                if (_selectedDropInCustomer == null)
                {
                    RentalFeedbackTextBlock.Text = "Sök fram en kund först.";
                    return;
                }

                var dropInResult = _skiShopController.CreateDropInRental(
                    _selectedDropInCustomer.CustomerID,
                    itemIds,
                    startDate,
                    endDate,
                    licenseVerified);

                if (!dropInResult.Success)
                {
                    RentalFeedbackTextBlock.Text = dropInResult.Message;
                    return;
                }

                MessageBox.Show(
                    dropInResult.Message + "\n\nTa betalt i kassan via kort eller Swish.",
                    "Drop-in registrerad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _selectedItems.Clear();
                LoadAvailableItems();
                SelectCustomer(null);
                BookingSearchTextBox.Clear();
                return;
            }

            // New rental to guest with accommodation
            if (_selectedBooking != null)
            {
                int bookingId = _selectedBooking.BookingID;
                (bool success, string message, decimal addedAmount) result;

                try
                {
                    result = _skiShopController.AddEquipmentToBooking(
                        bookingId,
                        itemIds,
                        startDate,
                        endDate,
                        licenseVerified);
                }
                catch (Exception ex)
                {
                    RentalFeedbackTextBlock.Text = $"Ett fel uppstod: {ex.Message}";
                    return;
                }

                if (!result.success)
                {
                    RentalFeedbackTextBlock.Text = result.message;
                    LoadAvailableItems();
                    return;
                }

                string itemList = string.Join("\n", _selectedItems.Select(item => $"  • {item.ArticleNumber} – {item.Equipment?.DisplayName ?? item.Equipment?.Description}"));

                MessageBox.Show(
                    $"Klart för fjället! Utrustningen är kopplad till bokning {bookingId} och betalas på rummet.\n\n" +
                    $"{itemList}\n\n" +
                    $"Period: {startDate:yyyy-MM-dd} – {endDate:yyyy-MM-dd}\n" +
                    $"Tillagt belopp: {result.addedAmount:N2} kr\n\n" +
                    result.message,
                    "Uthyrning registrerad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _selectedItems.Clear();
                LoadAvailableItems();

                var updated = _skiShopController.FindActiveBookings(bookingId.ToString()).FirstOrDefault(b => b.BookingID == bookingId);
                SelectBooking(updated);
            }
        }
        #endregion

        #region Return flow
        private void SearchReturnItemButton_Click(object sender, RoutedEventArgs e)
        {
            ReturnSearchFeedbackTextBlock.Text = string.Empty;
            DelayWarningBorder.Visibility = Visibility.Collapsed;
            _scannedReturnItem = null;

            string barcode = ReturnArticleTextBox.Text.Trim();
            if (string.IsNullOrEmpty(barcode))
            {
                ReturnSearchFeedbackTextBlock.Text = "Ange ett artikelnummer.";
                ScannedItemDetailBorder.Visibility = Visibility.Collapsed;
                AddScannedItemToListButton.IsEnabled = false;
                return;
            }

            if (_returnQueue.Any(r => r.ArticleNumber.Equals(barcode, StringComparison.OrdinalIgnoreCase)))
            {
                ReturnSearchFeedbackTextBlock.Text = "Artikeln ligger redan i listan till höger.";
                ScannedItemDetailBorder.Visibility = Visibility.Collapsed;
                AddScannedItemToListButton.IsEnabled = false;
                return;
            }

            var item = _skiShopController.GetRentedItemByBarcode(barcode);
            if (item == null)
            {
                ReturnSearchFeedbackTextBlock.Text = "Ingen uthyrd artikel med det artikelnumret hittades.";
                ScannedItemDetailBorder.Visibility = Visibility.Collapsed;
                AddScannedItemToListButton.IsEnabled = false;
                return;
            }

            _scannedReturnItem = item;

            ScannedItemTitleTextBlock.Text = $"{item.ArticleNumber} – {item.Equipment?.DisplayName ?? item.Equipment?.Description}";
            ScannedItemCategoryTextBlock.Text = $"Kategori: {item.Equipment?.Category} | Storlek: {item.Size}";

            var rental = _skiShopController.GetActiveRentalForItem(item.EquipmentItemID);
            if (rental != null)
            {
                ScannedItemRentalInfoTextBlock.Text = $"Bokning #{rental.BookingID} | Planerad retur: {rental.ReturnDate:yyyy-MM-dd}";

                if (DateTime.Today > rental.ReturnDate.Date)
                {
                    int delayDays = (DateTime.Today - rental.ReturnDate.Date).Days;
                    DelayWarningTextBlock.Text = $"OBS: Försenad med {delayDays} dag(ar)!";
                    DelayWarningBorder.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ScannedItemRentalInfoTextBlock.Text = "Ingen aktiv uthyrningspost hittades.";
            }

            ReturnConditionComboBox.SelectedItem = item.Condition;
            ReturnIsDamagedCheckBox.IsChecked = false;

            ScannedItemDetailBorder.Visibility = Visibility.Visible;
            AddScannedItemToListButton.IsEnabled = true;
        }

        // Moves the article from the left window to the queue in the right window
        private void AddScannedItemToListButton_Click(object sender, RoutedEventArgs e)
        {
            if (_scannedReturnItem == null) return;

            var condition = (EquipmentCondition)(ReturnConditionComboBox.SelectedItem ?? EquipmentCondition.BarelyUsed);
            bool isDamaged = ReturnIsDamagedCheckBox.IsChecked == true;

            _returnQueue.Add(new ReturnQueueRow(
                _scannedReturnItem.EquipmentItemID,
                _scannedReturnItem.ArticleNumber,
                _scannedReturnItem.Equipment?.DisplayName ?? _scannedReturnItem.Equipment?.Description ?? "Utrustning",
                condition,
                isDamaged,
                isDamaged ? "Skadad" : "OK"));

            ReturnArticleTextBox.Clear();
            ScannedItemDetailBorder.Visibility = Visibility.Collapsed;
            AddScannedItemToListButton.IsEnabled = false;
            _scannedReturnItem = null;
            ReturnArticleTextBox.Focus();

            RefreshReturnQueue();
        }

        // Removes selected row from the return queue
        private void RemoveFromReturnQueueButton_Click(object sender, RoutedEventArgs e)
        {
            HashSet<int> removeIds = ReturnQueueDataGrid.SelectedItems
                .OfType<ReturnQueueRow>()
                .Select(row => row.EquipmentItemID)
                .ToHashSet();

            _returnQueue.RemoveAll(item => removeIds.Contains(item.EquipmentItemID));
            RefreshReturnQueue();
        }

        // Updates the return table and text of articles
        private void RefreshReturnQueue()
        {
            ReturnQueueDataGrid.ItemsSource = null;
            ReturnQueueDataGrid.ItemsSource = _returnQueue.ToList();

            ReturnQueueSummaryTextBlock.Text = _returnQueue.Count == 0
                ? "Ingen utrustning i listan."
                : $"{_returnQueue.Count} artikel/artiklar redo för inlämning.";

            ConfirmAllReturnsButton.IsEnabled = _returnQueue.Count > 0;
        }

        // Final approval of all articles in the list
        private void ConfirmAllReturnsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_returnQueue.Count == 0) return;

            var returns = _returnQueue.Select(r => (r.EquipmentItemID, r.Condition, r.IsDamaged)).ToList();

            var result = _skiShopController.ReturnMultipleEquipmentItems(returns);

            if (result.Success)
            {
                MessageBox.Show(
                    $"Tack! {_returnQueue.Count} artikel/artiklar är återlämnade och redo för nästa fjälläventyr.",
                    "Återlämning klar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _returnQueue.Clear();
                RefreshReturnQueue();

                LoadAvailableItems();

                if (_selectedBooking != null)
                {
                    int bId = _selectedBooking.BookingID;
                    var updated = _skiShopController.FindActiveBookings(bId.ToString()).FirstOrDefault(b => b.BookingID == bId);
                    SelectBooking(updated);
                }
            }
            else
            {
                MessageBox.Show($"Kunde inte slutföra återlämningen:\n{result.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}

