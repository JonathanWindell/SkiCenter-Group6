using BusinessLayer.Controllers;
using EntityLayer;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Lets a system administrator view current season prices per week and register new prices.
    /// Old prices are kept as history and shown for the selected row.
    /// </summary>
    public partial class SeasonPriceView : Window
    {
        private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
        private static readonly Brush SuccessBrush = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));

        private readonly AdminController _adminController;

        public SeasonPriceView(AdminController adminController)
        {
            InitializeComponent();
            _adminController = adminController;

            FilterButton.Click += FilterButton_Click;
            ClearFilterButton.Click += ClearFilterButton_Click;
            SavePriceButton.Click += SavePriceButton_Click;
            PriceDataGrid.SelectionChanged += PriceDataGrid_SelectionChanged;

            Loaded += SeasonPriceView_Loaded;
        }

        /// <summary>
        /// Checks access before showing any data. Only system administrators may use this view.
        /// </summary>
        private void SeasonPriceView_Loaded(object sender, RoutedEventArgs e)
        {
            Staff? user = LoginController.CurrentLoggedInUser;

            if (user == null || !StaffController.CanChangeSeasonPrices(user.Role))
            {
                MessageBox.Show("Endast systemadministratörer har tillgång till säsongspriser.");
                Close();
                return;
            }

            LoadFilterOptions();
            LoadPrices();
        }

        /// <summary>
        /// Fills the accommodation type and price type filters with the values that have prices.
        /// Each option stores its filter value in Tag; null means "all".
        /// </summary>
        private void LoadFilterOptions()
        {
            TypeFilterComboBox.Items.Clear();
            PriceTypeFilterComboBox.Items.Clear();

            TypeFilterComboBox.Items.Add(new ComboBoxItem { Content = "Alla boendetyper", Tag = null });
            PriceTypeFilterComboBox.Items.Add(new ComboBoxItem { Content = "Alla pristyper", Tag = null });

            try
            {
                List<SeasonPrice> prices = _adminController.GetCurrentPrices().ToList();

                var accommodationTypes = prices
                    .Where(sp => sp.AccommodationType != null)
                    .Select(sp => sp.AccommodationType)
                    .DistinctBy(type => type.AccommodationTypeID)
                    .OrderBy(type => type.CategoryCode);

                foreach (AccommodationType type in accommodationTypes)
                {
                    TypeFilterComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = $"{type.CategoryCode} – {type.Description}",
                        Tag = type.AccommodationTypeID
                    });
                }

                IEnumerable<string> priceTypes = prices
                    .Select(sp => sp.PriceType)
                    .Distinct()
                    .OrderBy(priceType => priceType);

                foreach (string priceType in priceTypes)
                {
                    PriceTypeFilterComboBox.Items.Add(new ComboBoxItem { Content = priceType, Tag = priceType });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte hämta filteralternativ: {ex.Message}");
            }

            TypeFilterComboBox.SelectedIndex = 0;
            PriceTypeFilterComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Loads the current prices using the selected filters.
        /// </summary>
        private void LoadPrices()
        {
            FilterErrorTextBlock.Text = string.Empty;

            int? accommodationTypeId = (TypeFilterComboBox.SelectedItem as ComboBoxItem)?.Tag as int?;
            string? priceType = (PriceTypeFilterComboBox.SelectedItem as ComboBoxItem)?.Tag as string;

            int? week = null;
            string weekText = WeekFilterTextBox.Text.Trim();

            if (weekText.Length > 0)
            {
                if (!int.TryParse(weekText, out int parsedWeek) || parsedWeek < 1 || parsedWeek > 53)
                {
                    FilterErrorTextBlock.Text = "Vecka måste vara ett tal mellan 1 och 53.";
                    return;
                }

                week = parsedWeek;
            }

            try
            {
                PriceDataGrid.ItemsSource = _adminController.GetCurrentPrices(accommodationTypeId, priceType, week).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte hämta säsongspriser: {ex.Message}");
            }

            ClearSelection();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e) => LoadPrices();

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            TypeFilterComboBox.SelectedIndex = 0;
            PriceTypeFilterComboBox.SelectedIndex = 0;
            WeekFilterTextBox.Text = string.Empty;
            LoadPrices();
        }

        /// <summary>
        /// Shows the selected price in the edit panel together with its history.
        /// </summary>
        private void PriceDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PriceDataGrid.SelectedItem is not SeasonPrice selected)
            {
                ClearSelection();
                return;
            }

            string typeName = selected.AccommodationType?.CategoryCode ?? $"Boendetyp {selected.AccommodationTypeID}";

            string changedBy = selected.ChangedBy != null
                ? $"{selected.ChangedBy.FirstName} {selected.ChangedBy.LastName}"
                : "Ursprungligt pris";

            SelectedPriceTextBlock.Text = $"{typeName}, {selected.PriceType}, vecka {selected.WeekNumber}\n" +
                                          $"Nuvarande pris: {selected.Price:N2} kr\n" +
                                          $"Senast ändrad av: {changedBy}";
            NewPriceTextBox.Text = selected.Price.ToString("0.00", CultureInfo.GetCultureInfo("sv-SE"));
            NewPriceTextBox.IsEnabled = true;
            SavePriceButton.IsEnabled = true;
            PriceFeedbackTextBlock.Text = string.Empty;

            LoadHistory(selected.AccommodationTypeID, selected.PriceType, selected.WeekNumber);
        }

        private void LoadHistory(int accommodationTypeId, string priceType, int week)
        {
            try
            {
                HistoryDataGrid.ItemsSource = _adminController.GetPriceHistory(accommodationTypeId, priceType, week).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte hämta prishistorik: {ex.Message}");
            }
        }

        private void ClearSelection()
        {
            SelectedPriceTextBlock.Text = "Välj en rad i tabellen.";
            NewPriceTextBox.Text = string.Empty;
            NewPriceTextBox.IsEnabled = false;
            SavePriceButton.IsEnabled = false;
            HistoryDataGrid.ItemsSource = null;
        }

        /// <summary>
        /// Validates the entered price and registers it as a new price for the selected row.
        /// </summary>
        private void SavePriceButton_Click(object sender, RoutedEventArgs e)
        {
            if (PriceDataGrid.SelectedItem is not SeasonPrice selected)
            {
                return;
            }

            string priceText = NewPriceTextBox.Text.Trim();

            if (priceText.Length == 0)
            {
                ShowFeedback("Ange ett pris.", isError: true);
                return;
            }

            if (!TryParsePrice(priceText, out decimal newPrice))
            {
                ShowFeedback("Priset måste vara ett tal, t.ex. 4500 eller 4500,50.", isError: true);
                return;
            }

            if (newPrice < 0)
            {
                ShowFeedback("Priset kan inte vara negativt.", isError: true);
                return;
            }

            if (newPrice == 0)
            {
                ShowFeedback("Priset måste vara större än 0.", isError: true);
                return;
            }

            int accommodationTypeId = selected.AccommodationTypeID;
            string priceType = selected.PriceType;
            int week = selected.WeekNumber;

            (bool success, string message) result;

            try
            {
                result = _adminController.ChangeSeasonPrice(accommodationTypeId, priceType, week, newPrice);
            }
            catch (Exception ex)
            {
                ShowFeedback($"Ett fel uppstod när priset skulle sparas: {ex.Message}", isError: true);
                return;
            }

            if (!result.success)
            {
                ShowFeedback(result.message, isError: true);
                return;
            }

            // Reload and keep the same row selected so the new history is visible
            LoadPrices();
            SelectPrice(accommodationTypeId, priceType, week);
            ShowFeedback(result.message, isError: false);
        }

        private void SelectPrice(int accommodationTypeId, string priceType, int week)
        {
            if (PriceDataGrid.ItemsSource is not IEnumerable<SeasonPrice> prices)
            {
                return;
            }

            SeasonPrice? match = prices.FirstOrDefault(sp =>
                sp.AccommodationTypeID == accommodationTypeId && sp.PriceType == priceType && sp.WeekNumber == week);

            if (match != null)
            {
                PriceDataGrid.SelectedItem = match;
                PriceDataGrid.ScrollIntoView(match);
            }
        }

        /// <summary>
        /// Accepts both comma (4500,50) and dot (4500.50) as decimal separator, and spaces between thousands.
        /// </summary>
        private static bool TryParsePrice(string text, out decimal price)
        {
            string normalized = text.Replace(" ", "").Replace(" ", "").Replace(',', '.');

            return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out price);
        }

        private void ShowFeedback(string message, bool isError)
        {
            PriceFeedbackTextBlock.Text = message;
            PriceFeedbackTextBlock.Foreground = isError ? ErrorBrush : SuccessBrush;
        }
    }
}
