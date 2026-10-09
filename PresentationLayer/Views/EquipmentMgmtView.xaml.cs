using EntityLayer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using BusinessLayer.Controllers;


namespace PresentationLayer.Views
{
    public partial class EquipmentMgmtView : Window
    {
        // Temporary items for UI testing without database access.
        private readonly ObservableCollection<EquipmentItem> _previewItems = new();
        private readonly SkiShopController _skiShopController;

        // Tracks the item currently being edited.
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

            CollectionViewSource.GetDefaultView(_previewItems).Filter =
                MatchesEquipmentFilters;

            CategoryFilterComboBox.SelectionChanged += EquipmentFilters_SelectionChanged;
            StatusFilterComboBox.SelectionChanged += EquipmentFilters_SelectionChanged;

            LoadCategories();
            LoadEquipmentData();

            EditEquipmentButton.IsEnabled = false;
            DeleteEquipmentButton.IsEnabled = false;
        }

        // Enable equipment actions when a row is selected.
        private void EquipmentDataGrid_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = EquipmentDataGrid.SelectedItem != null;

            EditEquipmentButton.IsEnabled = hasSelection;
            DeleteEquipmentButton.IsEnabled = hasSelection;
        }

        private void EditEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentDataGrid.SelectedItem is not EquipmentItem selectedItem)
                return;

            ClearFieldErrors();

            // Display the selected item's details in the form.
            _editingItem = selectedItem;
            EquipmentFormHeading.Text = "Ändra utrustning";
            EquipmentNumberTextBox.Text = selectedItem.EquipmentItemID.ToString();
            EquipmentCategoryComboBox.SelectedItem = selectedItem.Equipment?.Category;
            EquipmentSizeTextBox.Text = selectedItem.Size;
            EquipmentConditionTextBox.Text = selectedItem.Condition.ToString();

            EquipmentStatusComboBox.SelectedIndex = -1;

            foreach (ComboBoxItem option in EquipmentStatusComboBox.Items)
            {
                string statusstring = option.Content?.ToString() ?? string.Empty;
                if (Enum.TryParse<EquipmentStatus>(statusstring, out var parsedStatus)) { }

                if (parsedStatus == selectedItem.Status)
                {
                    EquipmentStatusComboBox.SelectedItem = option;
                    break;
                }
            }

            EquipmentFeedbackTextBlock.Text = string.Empty;
        }


        private void DeleteEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentDataGrid.SelectedItem is not EquipmentItem selectedItem)
                return;

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
                _previewItems.Remove(selectedItem);
                ResetForm();

                ShowFeedback($"Artikeln {selectedItem.ArticleNumber} har tagits bort.", true);
            }
            else
            {
                ShowFeedback("Kunde inte ta bort artikeln. Kontrollera att den inte är uthyrd.", false);
            }
        }

        private void EquipmentFilters_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            // Apply both filters whenever either selection changes.
            CollectionViewSource.GetDefaultView(_previewItems).Refresh();
        }

        private bool MatchesEquipmentFilters(object item)
        {
            if (item is not EquipmentItem equipment)
                return false;

            string? category = CategoryFilterComboBox.SelectedItem as string;
            string? statusstring =
                (StatusFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (Enum.TryParse<EquipmentStatus>(statusstring, out var parsedStatus)) { }

            bool matchesCategory =
                CategoryFilterComboBox.SelectedIndex <= 0 ||
                equipment.Equipment?.Category == category;

            bool matchesStatus =
                StatusFilterComboBox.SelectedIndex <= 0 ||
                equipment.Status == parsedStatus;


            return matchesCategory && matchesStatus;
        }

        private void AddEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
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

            // Collect all validation errors before saving.
            var errors = new List<string>();
            int itemNumber = 0;
            decimal pricePerDay = 0;

            string numberText = EquipmentNumberTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(numberText))
            {
                errors.Add("Ange exemplarnummer.");
            }
            else if (!int.TryParse(numberText, out itemNumber) || itemNumber <= 0)
            {
                errors.Add("Exemplarnumret måste vara ett positivt heltal.");
            }
            else
            {
                // Numeric IDs are used only for this preview.
                foreach (EquipmentItem item in _previewItems)
                {
                    if (item.EquipmentItemID == itemNumber && item != _editingItem)
                    {
                        errors.Add("Exemplarnumret används redan.");
                        break;
                    }
                }
            }

            if (EquipmentCategoryComboBox.SelectedItem == null)
                errors.Add("Välj kategori.");

            if (string.IsNullOrWhiteSpace(EquipmentSizeTextBox.Text))
                errors.Add("Ange storlek eller längd.");

            if (string.IsNullOrWhiteSpace(EquipmentConditionTextBox.Text))
                errors.Add("Ange skick.");

            if (EquipmentStatusComboBox.SelectedItem == null)
                errors.Add("Välj status.");

            if (errors.Count > 0)
            {
                // Display every error message.
                MarkInvalidFields(errors);
                ShowFeedback("Kontrollera de markerade fälten.", false);
                return;
            }

            string category = (string)EquipmentCategoryComboBox.SelectedItem;
            string statusString = ((ComboBoxItem)EquipmentStatusComboBox.SelectedItem).Content.ToString()!;

            // Category IDs are temporary preview values, not database IDs.
            int categoryId = EquipmentCategoryComboBox.SelectedIndex + 1;

            bool isNewItem = _editingItem == null;
            EquipmentItem itemToSave = _editingItem ?? new EquipmentItem();

            if (Enum.TryParse<EquipmentStatus>(statusString, out var parsedStatus))
            {
                itemToSave.Status = parsedStatus;
            }
            else
            {
                // Fallback if the string doesn't match any enum value
                itemToSave.Status = EquipmentStatus.Available;
            }

            if (Enum.TryParse<EquipmentCondition>(EquipmentConditionTextBox.Text, out var parsedCondition))
            {
                itemToSave.Condition = parsedCondition;
            }
            else
            {
                // Fallback if the string doesn't match any enum value
                itemToSave.Condition = EquipmentCondition.New;
            }

            itemToSave.EquipmentItemID = itemNumber;
            itemToSave.Size = EquipmentSizeTextBox.Text.Trim();
            itemToSave.EquipmentID = categoryId;
            itemToSave.Equipment = new Equipment(category, category)
            {
                EquipmentID = categoryId
            };

            if (isNewItem)
                _previewItems.Add(itemToSave);

            // Refresh rows because the entity does not notify property changes.
            EquipmentDataGrid.Items.Refresh();

            ResetForm();


            ShowFeedback(
                    isNewItem
                        ? "Utrustningen har lagts till i testvyn. Inte sparad i databasen."
                        : "Utrustningen har uppdaterats i testvyn. Inte sparad i databasen.",
                    true);
        }

        // Use red for errors and green for successful preview actions.
        private void ShowFeedback(string message, bool isSuccess)
        {
            EquipmentFeedbackTextBlock.Foreground = new SolidColorBrush(
                isSuccess
                    ? Color.FromRgb(21, 128, 61)
                    : Color.FromRgb(185, 28, 28));

            EquipmentFeedbackTextBlock.Text = message;
        }


        // Highlight invalid fields and show their specific errors on hover.
        private void MarkInvalidFields(List<string> errors)
        {
            SetFieldError(EquipmentNumberTextBox, errors,
                "Ange exemplarnummer.",
                "Exemplarnumret måste vara ett positivt heltal.",
                "Exemplarnumret används redan.");

            SetFieldError(EquipmentCategoryComboBox, errors,
                "Välj kategori.");

            SetFieldError(EquipmentSizeTextBox, errors,
                "Ange storlek eller längd.");

            SetFieldError(EquipmentConditionTextBox, errors,
                "Ange skick.");

            SetFieldError(EquipmentStatusComboBox, errors,
                "Välj status.");
        }

        private void SetFieldError(
            Control field, List<string> errors, params string[] messages)
        {
            foreach (string message in messages)
            {
                if (errors.Contains(message))
                {
                    field.BorderBrush =
                        new SolidColorBrush(Color.FromRgb(185, 28, 28));
                    field.ToolTip = message;
                    return;
                }
            }

            field.SetResourceReference(Control.BorderBrushProperty, "InputBorder");
            field.ToolTip = null;
        }

        // Restore normal borders and remove validation tooltips.
        private void ClearFieldErrors()
        {
            Control[] fields =
            { EquipmentNumberTextBox,  EquipmentCategoryComboBox,
                EquipmentSizeTextBox, EquipmentConditionTextBox,
                EquipmentStatusComboBox
            };

            foreach (Control field in fields)
            {
                field.SetResourceReference(Control.BorderBrushProperty, "InputBorder");
                field.ToolTip = null;
            }
        }



        // Clear the form and reset the status to Available.
        private void ResetForm()
        {
            ClearFieldErrors();
            _editingItem = null;
            EquipmentDataGrid.SelectedItem = null;
            EquipmentFormHeading.Text = "Lägg till utrustning";
            EquipmentNumberTextBox.Clear();
            EquipmentCategoryComboBox.SelectedIndex = -1;
            EquipmentSizeTextBox.Clear();
            EquipmentConditionTextBox.Clear();
            EquipmentStatusComboBox.SelectedIndex = 0;
            EquipmentFeedbackTextBlock.Text = string.Empty;
        }

        private void LoadCategories()
        {
            // Gets all categories to fill dropdown
            var categories = _skiShopController.GetAllEquipmentWithStock();

            EquipmentCategoryComboBox.Items.Clear();
            CategoryFilterComboBox.Items.Clear();

            CategoryFilterComboBox.Items.Add("Alla kategorier");
            CategoryFilterComboBox.SelectedIndex = 0;

            foreach (var eq in categories)
            {
                EquipmentCategoryComboBox.Items.Add(eq.Category);
                CategoryFilterComboBox.Items.Add(eq.Category);
            }
        }

        private void LoadEquipmentData()
        {
            _previewItems.Clear();

            // Gets all items via controller
            var items = _skiShopController.GetAllEquipmentItems();
            foreach (var item in items)
            {
                _previewItems.Add(item);
            }

            CollectionViewSource.GetDefaultView(_previewItems)?.Refresh();
        }
    }
}
