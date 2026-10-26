using BusinessLayer.Controllers;
using EntityLayer;
using System.Windows;

namespace PresentationLayer.Views
{
    public partial class StaffAdminView : Window
    {
        private readonly StaffController _staffController;

        public StaffAdminView(StaffController staffController)
        {
            InitializeComponent();
            _staffController = staffController;

            // Populate dropdown with roles from Enum 
            RoleComboBox.ItemsSource = Enum.GetValues(typeof(staffRole));
            RoleComboBox.SelectedIndex = 0;
        }

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

            staffRole selectedRole = (staffRole)RoleComboBox.SelectedItem;

            // Send to StaffController for hashing and saving 
            var result = _staffController.CreateStaff(firstName, lastName, email, password, selectedRole);

            if (result.success)
            {
                MessageBox.Show(result.message, "Konto skapat", MessageBoxButton.OK, MessageBoxImage.Information);

                // Empty fields
                FirstNameTextBox.Clear();
                LastNameTextBox.Clear();
                EmailTextBox.Clear();
                PasswordBox.Clear();

                // Resets clear field and "un-check" viewing
                PasswordVisibleTextBox.Clear();
                ShowPasswordCheckBox.IsChecked = false;

                RoleComboBox.SelectedIndex = 0;
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


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}