using BusinessLayer.Controllers;
using EntityLayer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace PresentationLayer.Views
{
    public partial class EquipmentMgmtView : Window
    {
        private readonly ObservableCollection<EquipmentItem> _previewItems = new();
        private readonly SkiShopController _skiShopController;
        private List<Equipment> _equipmentModels = new();

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

        // Fills ComboBoxes with data
        private void PopulateComboBoxes()
        {
            // 1. Skick (Conditions)
            EquipmentConditionComboBox.Items.Clear();
            foreach (var condition in Enum.GetValues(typeof(EquipmentCondition)))
            {
                EquipmentConditionComboBox.Items.Add(condition);
            }
            EquipmentConditionComboBox.SelectedIndex = 0;


            // 2. Modeller & Kategorier
            var result = _skiShopController.GetAllEquipmentWithStock();
            _equipmentModels.Clear();
            foreach (var eq in result)
            {
                _equipmentModels.Add(eq);
            }

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


            // 3. Status i formuläret
            EquipmentStatusComboBox.ItemsSource = null;
            EquipmentStatusComboBox.ItemsSource = Enum.GetValues(typeof(EquipmentStatus));
            EquipmentStatusComboBox.SelectedIndex = 0;


            // 4. Status i sökfiltret
            var filterStatuses = new List<object> { "Alla statusar" };
            filterStatuses.AddRange(Enum.GetValues(typeof(EquipmentStatus)).Cast<object>());
            StatusFilterComboBox.ItemsSource = null;
            StatusFilterComboBox.ItemsSource = filterStatuses;
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
            if (StatusFilterComboBox.SelectedIndex > 0 && StatusFilterComboBox.SelectedItem is EquipmentStatus filterStatus)
            {
                matchesStatus = equipment.Status == filterStatus;
            }

            return matchesCategory && matchesStatus;
        }

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

            // Collect all validation errors before saving
            var errors = new List<string>();
            string articleNumber = EquipmentNumberTextBox.Text.Trim();
            string size = EquipmentSizeTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(articleNumber))
            {
                errors.Add("Ange artikelnummer.");
            }
            else
            {
                foreach (var item in _previewItems)
                {
                    if (item.ArticleNumber.Equals(articleNumber, StringComparison.OrdinalIgnoreCase)
                    && (_editingItem == null || item.EquipmentItemID != _editingItem.EquipmentItemID))
                    {
                        errors.Add("Artikelnumret används redan.");
                        break;
                    }
                }
            }

            if (EquipmentCategoryComboBox.SelectedItem == null)
                errors.Add("Välj kategori.");

            if (EquipmentDescriptionComboBox.SelectedItem == null)
                errors.Add("Välj utrustningstyp.");

            if (string.IsNullOrWhiteSpace(size))
                errors.Add("Ange storlek eller längd.");

            if (EquipmentConditionComboBox.SelectedItem == null)
                errors.Add("Ange skick.");

            if (EquipmentStatusComboBox.SelectedItem == null)
                errors.Add("Välj status.");

            if (errors.Count > 0)
            {
                // Display every error message
                MarkInvalidFields(errors);
                ShowFeedback("Kontrollera de markerade fälten.", false);
                return;
            }

            string selectedCategory = (string)EquipmentCategoryComboBox.SelectedItem;
            string selectedDescription = (string)EquipmentDescriptionComboBox.SelectedItem;
            var status = (EquipmentStatus)EquipmentStatusComboBox.SelectedItem;
            var condition = (EquipmentCondition)EquipmentConditionComboBox.SelectedItem;

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

            EquipmentCategoryComboBox.SelectedItem = selectedItem.Equipment?.Category;
            EquipmentDescriptionComboBox.SelectedItem = selectedItem.Equipment?.Description;

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

        private void ShowFeedback(string message, bool isSuccess)
        {
            EquipmentFeedbackTextBlock.Text = message;
        }

        // Highlight invalid fields and show their specific errors on hover
        private void MarkInvalidFields(List<string> errors)
        {
            SetFieldError(EquipmentNumberTextBox, errors, "Ange artikelnummer.", "Artikelnumret används redan.");
            SetFieldError(EquipmentCategoryComboBox, errors, "Välj kategori.");
            SetFieldError(EquipmentDescriptionComboBox, errors, "Välj utrustningstyp.");
            SetFieldError(EquipmentSizeTextBox, errors, "Ange storlek eller längd.");
            SetFieldError(EquipmentConditionComboBox, errors, "Ange skick.");
            SetFieldError(EquipmentStatusComboBox, errors, "Välj status.");
        }

        private void SetFieldError(Control field, List<string> errors, params string[] messages)
        {
            foreach (string message in messages)
            {
                if (errors.Contains(message))
                {
                    field.ToolTip = message;
                    return;
                }
            }

            field.SetResourceReference(Control.BorderBrushProperty, "InputBorder");
            field.ToolTip = null;
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
    }
}