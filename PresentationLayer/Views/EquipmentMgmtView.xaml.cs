using BusinessLayer.Controllers;
using EntityLayer;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace PresentationLayer.Views
{
    public partial class EquipmentMgmtView : Window
    {
        #region Fields and constructor

        private readonly ObservableCollection<EquipmentItem> _previewItems = new();
        private readonly SkiShopController _skiShopController;
        private List<Equipment> _equipmentModels = new();

        private static readonly Dictionary<EquipmentCondition, string> ConditionNames = new()
        {
            [EquipmentCondition.New] = "Ny",
            [EquipmentCondition.BarelyUsed] = "Knappt använd",
            [EquipmentCondition.Worn] = "Använd"
        };

        private static readonly Dictionary<EquipmentStatus, string> StatusNames = new()
        {
            [EquipmentStatus.Available] = "Tillgänglig",
            [EquipmentStatus.Rented] = "Uthyrd",
            [EquipmentStatus.Damaged] = "Skadad",
            [EquipmentStatus.Maintenance] = "Under underhåll"
        };

        // Tracks the current item being edited
        private EquipmentItem? _editingItem;

        public EquipmentMgmtView(SkiShopController skiShopController)
        {
            InitializeComponent();
            _skiShopController = skiShopController;

            AddEquipmentButton.Click += AddEquipmentButton_Click;
            CancelEquipmentButton.Click += CancelEquipmentButton_Click;
            SaveEquipmentButton.Click += SaveEquipmentButton_Click;
            EditEquipmentButton.Click += EditEquipmentButton_Click;
            DeleteEquipmentButton.Click += DeleteEquipmentButton_Click;
            EquipmentDataGrid.SelectionChanged += EquipmentDataGrid_SelectionChanged;

            EquipmentDataGrid.ItemsSource = _previewItems;
            CollectionViewSource.GetDefaultView(_previewItems).Filter = MatchesEquipmentFilters;

            CategoryFilterComboBox.SelectionChanged += EquipmentFilters_SelectionChanged;
            StatusFilterComboBox.SelectionChanged += EquipmentFilters_SelectionChanged;

            EquipmentCategoryComboBox.SelectionChanged += EquipmentCategoryComboBox_SelectionChanged;

            PopulateComboBoxes();

            // Load data from database
            LoadEquipmentData();

            EditEquipmentButton.IsEnabled = false;
            DeleteEquipmentButton.IsEnabled = false;
        }

        #endregion

        #region Loading and filters

        // Fills ComboBoxes with data
        private void PopulateComboBoxes()
        {
            EquipmentConditionComboBox.Items.Clear();
            foreach (EquipmentCondition condition in Enum.GetValues<EquipmentCondition>())
            {
                string name = ConditionNames.TryGetValue(condition, out string? val) ? val : condition.ToString();
                EquipmentConditionComboBox.Items.Add(new ComboBoxItem { Content = name, Tag = condition });
            }
            EquipmentConditionComboBox.SelectedIndex = 0;

            var result = _skiShopController.GetAllEquipmentWithStock();
            _equipmentModels.Clear();
            foreach (var eq in result) _equipmentModels.Add(eq);

            EquipmentCategoryComboBox.Items.Clear();
            CategoryFilterComboBox.Items.Clear();
            CategoryFilterComboBox.Items.Add("Alla kategorier");
            CategoryFilterComboBox.SelectedIndex = 0;

            foreach (var eq in _equipmentModels)
            {
                if (!EquipmentCategoryComboBox.Items.Contains(eq.Category))
                {
                    EquipmentCategoryComboBox.Items.Add(eq.Category);
                    CategoryFilterComboBox.Items.Add(eq.Category);
                }
            }

            EquipmentStatusComboBox.Items.Clear();
            foreach (EquipmentStatus status in Enum.GetValues<EquipmentStatus>())
            {
                string name = StatusNames.TryGetValue(status, out string? val) ? val : status.ToString();
                EquipmentStatusComboBox.Items.Add(new ComboBoxItem { Content = name, Tag = status });
            }
            EquipmentStatusComboBox.SelectedIndex = 0;

            StatusFilterComboBox.Items.Clear();
            StatusFilterComboBox.Items.Add(new ComboBoxItem { Content = "Alla statusar", Tag = null }); // null = ingen filtrering
            foreach (EquipmentStatus status in Enum.GetValues<EquipmentStatus>())
            {
                string name = StatusNames.TryGetValue(status, out string? val) ? val : status.ToString();
                StatusFilterComboBox.Items.Add(new ComboBoxItem { Content = name, Tag = status });
            }
            StatusFilterComboBox.SelectedIndex = 0;
        }

        // Loads equipment items
        private void LoadEquipmentData()
        {
            _previewItems.Clear();
            var items = _skiShopController.GetAllEquipmentItems();
            foreach (var item in items)
            {
                _previewItems.Add(item);
            }

            CollectionViewSource.GetDefaultView(_previewItems)?.Refresh();
        }

        // Enable further action once a option has been selected
        private void EquipmentCategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EquipmentDescriptionComboBox.Items.Clear();

            if (EquipmentCategoryComboBox.SelectedItem is string selectedCategory)
            {
                foreach (var eq in _equipmentModels)
                {
                    if (eq.Category == selectedCategory && !EquipmentDescriptionComboBox.Items.Contains(eq.Description))
                    {
                        EquipmentDescriptionComboBox.Items.Add(eq.Description);
                    }
                }

                EquipmentDescriptionComboBox.IsEnabled = true;
                if (EquipmentDescriptionComboBox.Items.Count > 0)
                {
                    EquipmentDescriptionComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                EquipmentDescriptionComboBox.IsEnabled = false;
            }
        }

        // Enable equipment actions when a row is selected.
        private void EquipmentDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = EquipmentDataGrid.SelectedItem != null;
            EditEquipmentButton.IsEnabled = hasSelection;
            DeleteEquipmentButton.IsEnabled = hasSelection;
        }

        private void EquipmentFilters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(_previewItems)?.Refresh();

            if (EquipmentDataGrid.Items.Count > 0)
            {
                EquipmentDataGrid.ScrollIntoView(EquipmentDataGrid.Items[0]);
            }
        }

        private bool MatchesEquipmentFilters(object item)
        {
            if (item is not EquipmentItem equipment)
                return false;

            string? category = CategoryFilterComboBox.SelectedItem as string;
            bool matchesCategory = CategoryFilterComboBox.SelectedIndex <= 0 ||
                                        equipment.Equipment?.Category == category;

            bool matchesStatus = true;
            if (StatusFilterComboBox.SelectedItem is ComboBoxItem selectedFilter && selectedFilter.Tag is EquipmentStatus filterStatus)
            {
                matchesStatus = equipment.Status == filterStatus;
            }

            return matchesCategory && matchesStatus;
        }

        #endregion

        #region Navigation

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Equipment form

        private void AddEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
            EquipmentFormPanel.Visibility = Visibility.Visible;
            EquipmentFormHeading.Text = "Lägg till utrustning";
            SaveEquipmentButton.Content = "Lägg till";
            EquipmentNumberTextBox.IsEnabled = true;
            EquipmentNumberTextBox.Focus();
        }

        private void CancelEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void SaveEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            EquipmentFeedbackTextBlock.Text = string.Empty;
            ClearFieldErrors();

            // Collect all validation errors before saving. Each error belongs to the field it concerns.
            var errors = new List<(Control Field, string Message)>();
            string articleNumber = EquipmentNumberTextBox.Text.Trim();
            string size = EquipmentSizeTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(articleNumber))
            {
                errors.Add((EquipmentNumberTextBox, "Ange ett artikelnummer."));
            }
            else
            {
                EquipmentItem? existing = _previewItems.FirstOrDefault(item =>
                    item.ArticleNumber.Equals(articleNumber, StringComparison.OrdinalIgnoreCase)
                    && (_editingItem == null || item.EquipmentItemID != _editingItem.EquipmentItemID));

                if (existing != null)
                {
                    // Tell which item already has the number, so the user knows what to look for
                    string existingName = existing.Equipment?.DisplayName ?? "en annan artikel";
                    errors.Add((EquipmentNumberTextBox,
                        $"Artikelnumret {existing.ArticleNumber} finns redan ({existingName}, storlek {existing.Size}). " +
                        "Varje artikel behöver ett eget nummer – ange ett nytt artikelnummer."));
                }
            }

            if (EquipmentCategoryComboBox.SelectedItem == null)
                errors.Add((EquipmentCategoryComboBox, "Välj en kategori."));

            if (EquipmentDescriptionComboBox.SelectedItem == null)
                errors.Add((EquipmentDescriptionComboBox, "Välj en utrustningstyp."));

            if (string.IsNullOrWhiteSpace(size))
                errors.Add((EquipmentSizeTextBox, "Ange storlek eller längd."));

            if (EquipmentConditionComboBox.SelectedItem == null)
                errors.Add((EquipmentConditionComboBox, "Välj skick."));

            if (EquipmentStatusComboBox.SelectedItem == null)
                errors.Add((EquipmentStatusComboBox, "Välj status."));

            if (errors.Count > 0)
            {
                // Mark the fields in red and show what is wrong, not just that something is wrong
                MarkInvalidFields(errors);
                ShowFeedback(errors.Count == 1
                    ? errors[0].Message
                    : "Rätta följande innan du sparar:\n• " + string.Join("\n• ", errors.Select(e => e.Message)), false);
                return;
            }

            string selectedCategory = (string)EquipmentCategoryComboBox.SelectedItem;
            string selectedDescription = (string)EquipmentDescriptionComboBox.SelectedItem;
            var status = (EquipmentStatus)((ComboBoxItem)EquipmentStatusComboBox.SelectedItem).Tag;
            var condition = (EquipmentCondition)((ComboBoxItem)EquipmentConditionComboBox.SelectedItem).Tag;

            Equipment? matchedModel = null;
            foreach (var model in _equipmentModels)
            {
                if (model.Category == selectedCategory && model.Description == selectedDescription)
                {
                    matchedModel = model;
                    break;
                }
            }

            if (matchedModel == null)
            {
                ShowFeedback("Kunde inte hitta vald utrustningsmodell.", false);
                return;
            }

            bool isNewItem = _editingItem == null;

            if (isNewItem)
            {
                bool success = _skiShopController.AddEquipmentItem(
                    matchedModel.EquipmentID,
                    articleNumber,
                    size,
                    status,
                    condition);

                if (!success)
                {
                    MessageBox.Show("Kunde inte lägga till utrustningen. Kontrollera att artikelnumret inte redan finns i databasen.",
                        "Fel vid sparande",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                _editingItem.Size = size;
                _editingItem.Condition = condition;
                _editingItem.Status = status;
                _editingItem.EquipmentID = matchedModel.EquipmentID;

                bool success = _skiShopController.UpdateEquipmentItem(
                    _editingItem.ArticleNumber,
                    matchedModel.EquipmentID,
                    size,
                    status,
                    condition);

                if (!success)
                {
                    ShowFeedback("Kunde inte uppdatera utrustningen i databasen.", false);
                    return;
                }
            }

            LoadEquipmentData();
            ResetForm();
            EquipmentFormPanel.Visibility = Visibility.Visible;
            ShowFeedback(isNewItem ? "Utrustningen har lagts till." : "Utrustningen har uppdaterats.", true);
        }

        private void EditEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentDataGrid.SelectedItem is not EquipmentItem selectedItem)
            {
                MessageBox.Show("Ingen rad är markerad i listan!");
                return;
            }

            ClearFieldErrors();

            // Display the selected item's details in the form.
            _editingItem = selectedItem;
            EquipmentFormPanel.Visibility = Visibility.Visible;
            EquipmentFormHeading.Text = "Ändra utrustning";
            SaveEquipmentButton.Content = "Spara ändringar";

            EquipmentNumberTextBox.Text = selectedItem.ArticleNumber;
            EquipmentNumberTextBox.IsEnabled = false;

            // Leta upp rätt ComboBoxItem baserat på Tag för att visa rätt i rullistan vid ändring
            foreach (ComboBoxItem item in EquipmentStatusComboBox.Items)
            {
                if ((EquipmentStatus)item.Tag == selectedItem.Status)
                    EquipmentStatusComboBox.SelectedItem = item;
            }

            foreach (ComboBoxItem item in EquipmentConditionComboBox.Items)
            {
                if ((EquipmentCondition)item.Tag == selectedItem.Condition)
                    EquipmentConditionComboBox.SelectedItem = item;
            }

            EquipmentStatusComboBox.SelectedItem = selectedItem.Status;
            EquipmentConditionComboBox.SelectedItem = selectedItem.Condition;
            EquipmentSizeTextBox.Text = selectedItem.Size;


            EquipmentFeedbackTextBlock.Text = string.Empty;
        }


        private void DeleteEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentDataGrid.SelectedItem is not EquipmentItem selectedItem)
            {
                MessageBox.Show("Välj en artikel i listan först.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Är du säker på att du vill ta bort utrustning med artikelnummer {selectedItem.ArticleNumber}?",
                "Ta bort utrustning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
                return;

            bool isDeleted = _skiShopController.DeleteEquipmentItem(selectedItem.ArticleNumber);

            if (isDeleted)
            {
                LoadEquipmentData();
                ResetForm();
                ShowFeedback($"Artikeln {selectedItem.ArticleNumber} har tagits bort.", true);
            }
            else
            {
                ShowFeedback("Kunde inte ta bort artikeln. Kontrollera att den inte är uthyrd.", false);
            }
        }

        #endregion

        #region Converters

        public class EquipmentConditionSwedishConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value?.ToString() switch
                {
                    "New" => "Ny",
                    "BarelyUsed" => "Knappt använd",
                    "Worn" => "Använd",
                    _ => value?.ToString() ?? ""
                };
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region Form feedback and validation

        private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
        private static readonly Brush SuccessBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x7A, 0x4F));

        // Errors in red, confirmations in green
        private void ShowFeedback(string message, bool isSuccess)
        {
            EquipmentFeedbackTextBlock.Text = message;
            EquipmentFeedbackTextBlock.Foreground = isSuccess ? SuccessBrush : ErrorBrush;
        }

        // Give each invalid field a red border, with its error message shown on hover
        private void MarkInvalidFields(List<(Control Field, string Message)> errors)
        {
            foreach ((Control field, string message) in errors)
            {
                field.BorderBrush = ErrorBrush;
                field.BorderThickness = new Thickness(2);
                field.ToolTip = message;
            }
        }

        // Restore normal borders and remove validation tooltips
        private void ClearFieldErrors()
        {
            Control[] fields =
            {
                EquipmentNumberTextBox,
                EquipmentCategoryComboBox,
                EquipmentDescriptionComboBox,
                EquipmentSizeTextBox,
                EquipmentConditionComboBox,
                EquipmentStatusComboBox
            };

            foreach (Control field in fields)
            {
                field.SetResourceReference(Control.BorderBrushProperty, "InputBorder");
                field.BorderThickness = new Thickness(1);
                field.ToolTip = null;
            }
        }

        // Clear the form and reset the status to Available
        private void ResetForm()
        {
            ClearFieldErrors();
            _editingItem = null;
            EquipmentDataGrid.SelectedItem = null;

            EquipmentFormPanel.Visibility = Visibility.Collapsed;

            EquipmentFormHeading.Text = "Lägg till utrustning";
            SaveEquipmentButton.Content = "Spara utrustning";
            EquipmentNumberTextBox.IsEnabled = true;

            EquipmentNumberTextBox.Clear();
            EquipmentCategoryComboBox.SelectedIndex = -1;
            EquipmentDescriptionComboBox.Items.Clear();
            EquipmentDescriptionComboBox.IsEnabled = false;
            EquipmentSizeTextBox.Clear();
            EquipmentConditionComboBox.SelectedIndex = 0;
            EquipmentStatusComboBox.SelectedIndex = 0;

            EquipmentFeedbackTextBlock.Text = string.Empty;
        }

        #endregion
    }
}