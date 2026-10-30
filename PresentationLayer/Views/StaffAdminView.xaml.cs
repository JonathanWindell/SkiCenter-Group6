using BusinessLayer.Controllers;
using EntityLayer;
using PresentationLayer.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Lets the system administrator see every staff account, register new ones,
    /// and deactivate or reactivate accounts for staff who have quit, been dismissed or are on leave.
    /// </summary>
    public partial class StaffAdminView : Window
    {
        #region Fields and constructor

        private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
        private static readonly Brush SuccessBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x7A, 0x4F));

        private readonly StaffController _staffController;

        // Every account, as loaded from the database. The list shows a filtered view of these.
        private List<StaffRow> _allStaff = new();

        public StaffAdminView(StaffController staffController)
        {
            InitializeComponent();
            _staffController = staffController;

            // Populate dropdown with the roles' Swedish names. The role itself is stored in Tag.
            foreach (staffRole role in Enum.GetValues<staffRole>())
            {
                RoleComboBox.Items.Add(new ComboBoxItem { Content = StaffController.GetRoleDisplayName(role), Tag = role });
            }
            RoleComboBox.SelectedIndex = 0;

            StaffSearchTextBox.TextChanged += (_, _) => ApplyStaffFilter();
            ShowInactiveCheckBox.Click += (_, _) => ApplyStaffFilter();
            StaffDataGrid.SelectionChanged += (_, _) => UpdateToggleButton();

            LoadStaff();
        }

        #endregion

        #region Staff list

        /// <summary>
        /// Loads every staff account and shows it in the list.
        /// </summary>
        private void LoadStaff()
        {
            try
            {
                _allStaff = _staffController.GetAllStaff()
                    .Select(s => new StaffRow
                    {
                        StaffID = s.StaffID,
                        Name = $"{s.FirstName} {s.LastName}",
                        Email = s.Email,
                        Role = StaffController.GetRoleDisplayName(s.Role),
                        IsActive = s.IsActive
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                ShowListFeedback($"Kunde inte hämta personal: {ex.Message}", isSuccess: false);
                _allStaff = new List<StaffRow>();
            }

            ApplyStaffFilter();
        }

        /// <summary>
        /// Shows the accounts that match the search text, with or without deactivated accounts.
        /// </summary>
        private void ApplyStaffFilter()
        {
            string search = StaffSearchTextBox.Text.Trim();
            bool showInactive = ShowInactiveCheckBox.IsChecked == true;

            List<StaffRow> visible = _allStaff
                .Where(s => showInactive || s.IsActive)
                .Where(s => search.Length == 0
                    || s.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || s.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || s.Role.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            StaffDataGrid.ItemsSource = visible;

            int activeCount = _allStaff.Count(s => s.IsActive);
            StaffCountTextBlock.Text = $"{activeCount} aktiva · {_allStaff.Count - activeCount} inaktiverade";

            UpdateToggleButton();
        }

        /// <summary>
        /// The button deactivates an active account or reactivates a deactivated one.
        /// </summary>
        private void UpdateToggleButton()
        {
            if (StaffDataGrid.SelectedItem is StaffRow selected)
            {
                ToggleActiveButton.IsEnabled = true;
                ToggleActiveButton.Content = selected.IsActive ? "Inaktivera konto" : "Återaktivera konto";
            }
            else
            {
                ToggleActiveButton.IsEnabled = false;
                ToggleActiveButton.Content = "Inaktivera konto";
            }
        }

        #endregion

        #region Deactivate and reactivate

        /// <summary>
        /// Asks for confirmation, then deactivates or reactivates the selected account.
        /// </summary>
        private void ToggleActiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (StaffDataGrid.SelectedItem is not StaffRow selected)
            {
                return;
            }

            bool activate = !selected.IsActive;

            string question = activate
                ? $"Vill du återaktivera {selected.Name}? Personen kan då logga in igen."
                : $"Vill du inaktivera {selected.Name}?\n\nPersonen kan inte längre logga in. Bokningar och prishistorik som personen har gjort finns kvar.";

            MessageBoxResult answer = MessageBox.Show(
                question,
                activate ? "Återaktivera konto" : "Inaktivera konto",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            (bool success, string message) result;

            try
            {
                result = _staffController.SetStaffActive(selected.StaffID, activate);
            }
            catch (Exception ex)
            {
                ShowListFeedback($"Ett fel uppstod: {ex.Message}", isSuccess: false);
                return;
            }

            ShowListFeedback(result.message, result.success);

            if (result.success)
            {
                int staffId = selected.StaffID;
                LoadStaff();
                SelectStaff(staffId);
            }
        }

        private void SelectStaff(int staffId)
        {
            if (StaffDataGrid.ItemsSource is IEnumerable<StaffRow> rows)
            {
                StaffDataGrid.SelectedItem = rows.FirstOrDefault(r => r.StaffID == staffId);
            }
        }

        private void ShowListFeedback(string message, bool isSuccess)
        {
            StaffListFeedbackTextBlock.Text = message;
            StaffListFeedbackTextBlock.Foreground = isSuccess ? SuccessBrush : ErrorBrush;
        }

        #endregion

        #region Create staff

        /// <summary>
        /// Creates, hashes password and saves new User to database.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FeedbackTextBlock.Text = string.Empty;

            string firstName = FirstNameTextBox.Text;
            string lastName = LastNameTextBox.Text;
            string email = EmailTextBox.Text;

            string password = ShowPasswordCheckBox.IsChecked == true ? PasswordVisibleTextBox.Text : PasswordBox.Password;

            if ((RoleComboBox.SelectedItem as ComboBoxItem)?.Tag is not staffRole selectedRole)
            {
                FeedbackTextBlock.Text = "Välj en roll.";
                return;
            }

            // Send to StaffController for hashing and saving
            var result = _staffController.CreateStaff(firstName, lastName, email, password, selectedRole);

            if (result.success)
            {
                MessageBox.Show(result.message, "Konto skapat", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();

                // Show the new account in the list straight away
                LoadStaff();
            }
            else
            {
                FeedbackTextBlock.Text = result.message;
            }
        }

        /// <summary>
        /// Allows System Admin to view password in clear text before creating User.
        /// </summary>
        private void ShowPassword_Changed(object sender, RoutedEventArgs e)
        {
            if (ShowPasswordCheckBox.IsChecked == true)
            {
                // Copy password to TextBox and show it
                PasswordVisibleTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordVisibleTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                // Copy Password back to PasswordBox and show it
                PasswordBox.Password = PasswordVisibleTextBox.Text;
                PasswordVisibleTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Empties the registration form and hides the password again.
        /// </summary>
        private void ClearForm()
        {
            FirstNameTextBox.Clear();
            LastNameTextBox.Clear();
            EmailTextBox.Clear();
            PasswordBox.Clear();

            // Resets clear field and "un-check" viewing
            PasswordVisibleTextBox.Clear();
            ShowPasswordCheckBox.IsChecked = false;

            RoleComboBox.SelectedIndex = 0;
            FeedbackTextBlock.Text = string.Empty;
        }

        #endregion

        #region Navigation

        // The view now holds both the list and the form, so this clears the form instead of closing the window
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}
