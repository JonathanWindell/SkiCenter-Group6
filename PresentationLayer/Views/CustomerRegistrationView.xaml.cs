using BusinessLayer.Controllers;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PresentationLayer.Views
{
    public partial class CustomerRegistrationView : Window
    {
        private readonly CustomerController _customerController;

        public CustomerRegistrationView(CustomerController customerController)
        {
            InitializeComponent();
            _customerController = customerController;
        }

        private void ShowFieldError(TextBlock errorBlock, string message)
        {
            errorBlock.Text = message;
            errorBlock.Visibility = Visibility.Visible;
        }

        private void ClearFieldErrors()
        {
            FirstNameError.Visibility = Visibility.Collapsed;
            LastNameError.Visibility = Visibility.Collapsed;
            AddressError.Visibility = Visibility.Collapsed;
            EmailError.Visibility = Visibility.Collapsed;
            PhoneError.Visibility = Visibility.Collapsed;

            CompanyNameError.Visibility = Visibility.Collapsed;
            OrgNumberError.Visibility = Visibility.Collapsed;
            ContactPersonError.Visibility = Visibility.Collapsed;

            ValidationMessage.Visibility = Visibility.Collapsed;
        }

        private void ShowValidationError(string message)
        {
            ValidationMessage.Text = message;
            ValidationMessage.Foreground = Brushes.Red;
            ValidationMessage.Visibility = Visibility.Visible;
        }

        private void CustomerTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CorporateFields == null)
                return;

            // Empty all fields when customer type is changed.
            FirstNameTextBox.Clear();
            LastNameTextBox.Clear();
            AddressTextBox.Clear();
            EmailTextBox.Clear();
            PhoneTextBox.Clear();

            CompanyNameTextBox.Clear();
            OrgNumberTextBox.Clear();
            ContactPersonTextBox.Clear();

            // Clear validation messages. 
            ClearFieldErrors();

            ValidationMessage.Text = "";
            ValidationMessage.Visibility = Visibility.Collapsed;

            // Only show corporate customer fields when corporate customer is chosen. 
            CorporateFields.Visibility = CustomerTypeComboBox.SelectedIndex == 1
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveCustomer_Click(object sender, RoutedEventArgs e)
        {
            ClearFieldErrors();

            bool isValid = true;
            bool isCorporate = CustomerTypeComboBox.SelectedIndex == 1;

            string email = EmailTextBox.Text.Trim();
            string phone = PhoneTextBox.Text.Trim();
            string address = AddressTextBox.Text.Trim();

            string firstName = FirstNameTextBox.Text.Trim();
            string lastName = LastNameTextBox.Text.Trim();

            string companyName = CompanyNameTextBox.Text.Trim();
            string orgNumber = OrgNumberTextBox.Text.Trim();
            string contactPerson = ContactPersonTextBox.Text.Trim();

            // Validation for both customer types. 
            if (string.IsNullOrWhiteSpace(address))
            {
                ShowFieldError(AddressError, "Ange adress.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowFieldError(EmailError, "Ange e-postadress.");
                isValid = false;
            }
            else if (!Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            {
                ShowFieldError(EmailError, "Ange en giltig e-postadress.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                ShowFieldError(PhoneError, "Ange telefonnummer.");
                isValid = false;
            }
            else if (!Regex.IsMatch(phone, @"^\+?[0-9\s-]{7,20}$"))
            {
                ShowFieldError(PhoneError, "Ange ett giltigt telefonnummer.");
                isValid = false;
            }

            // Validation based on customer type.
            if (isCorporate)
            {
                if (string.IsNullOrWhiteSpace(companyName))
                {
                    ShowFieldError(CompanyNameError, "Ange företagsnamn.");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(orgNumber))
                {
                    ShowFieldError(OrgNumberError, "Ange organisationsnummer.");
                    isValid = false;
                }
                else if (!Regex.IsMatch(orgNumber, @"^\d{6}-?\d{4}$"))
                {
                    ShowFieldError(OrgNumberError, "Ange organisationsnummer med 10 siffror.");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(contactPerson))
                {
                    ShowFieldError(ContactPersonError, "Ange kontaktperson.");
                    isValid = false;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(firstName))
                {
                    ShowFieldError(FirstNameError, "Ange förnamn.");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(lastName))
                {
                    ShowFieldError(LastNameError, "Ange efternamn.");
                    isValid = false;
                }
            }

            // Cancel if validation of frontend failed.
            if (!isValid)
                return;

            // Save to database via BusinessLayer
            bool saveSuccess = false;

            if (isCorporate)
            {
                saveSuccess = _customerController.RegisterCorporateCustomer(
                    companyName: companyName,
                    orgNumber: orgNumber,
                    contactPerson: contactPerson,
                    address: address,
                    email: email,
                    phoneNumber: phone
                );
            }
            else
            {
                saveSuccess = _customerController.RegisterPrivateCustomer(
                    firstName: firstName,
                    lastName: lastName,
                    address: address,
                    email: email,
                    phoneNumber: phone
                );
            }

            // Handle result
            if (saveSuccess)
            {
                ValidationMessage.Text = "Kunden har sparats i databasen!";
                ValidationMessage.Foreground = Brushes.Green;
                ValidationMessage.Visibility = Visibility.Visible;

                MessageBox.Show("Kunden registrerades framgångsrikt!", "Registrering klar", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            else
            {
                ShowValidationError("Kunde inte spara kunden. Kontrollera om e-post/telefon redan finns registrerat.");
            }
        }
    }
}