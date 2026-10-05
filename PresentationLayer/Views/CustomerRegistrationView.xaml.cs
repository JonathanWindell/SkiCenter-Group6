using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for CustomerRegistrationView.xaml
    /// </summary>
    public partial class CustomerRegistrationView : Window
    {

        public CustomerRegistrationView()
        {
            InitializeComponent();
        }
        //show errors 
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



        private void SaveCustomer_Click(object sender, RoutedEventArgs e)
        {
            ClearFieldErrors();

            bool isValid = true;

            // Förnamn
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                ShowFieldError(FirstNameError, "Ange förnamn.");
                isValid = false;
            }

            // Efternamn
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                ShowFieldError(LastNameError, "Ange efternamn.");
                isValid = false;
            }

            // Adress
            if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
            {
                ShowFieldError(AddressError, "Ange adress.");
                isValid = false;
            }

            // E-postadress
            string email = EmailTextBox.Text.Trim();

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

            // Telefonnummer
            string phone = PhoneTextBox.Text.Trim();

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

            // Företagsuppgifter
            if (CustomerTypeComboBox.SelectedIndex == 1)
            {
                if (string.IsNullOrWhiteSpace(CompanyNameTextBox.Text))
                {
                    ShowFieldError(CompanyNameError, "Ange företagsnamn.");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(OrgNumberTextBox.Text))
                {
                    ShowFieldError(OrgNumberError, "Ange organisationsnummer.");
                    isValid = false;
                }
                else if (!Regex.IsMatch(
                    OrgNumberTextBox.Text.Trim(), @"^\d{6}-?\d{4}$"))
                {
                    ShowFieldError(
                        OrgNumberError,
                        "Ange organisationsnummer med 10 siffror.");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(ContactPersonTextBox.Text))
                {
                    ShowFieldError(ContactPersonError, "Ange kontaktperson.");
                    isValid = false;
                }
            }

            if (!isValid)
                return;

            // Frontend-valideringen lyckades.
            ValidationMessage.Text =
                "Uppgifterna är korrekt ifyllda och redo att sparas.";

            ValidationMessage.Foreground = Brushes.Green;
            ValidationMessage.Visibility = Visibility.Visible;
        }



        private void ShowValidationError(string message)
        {
            ValidationMessage.Text = message;

            ValidationMessage.Foreground =
                System.Windows.Media.Brushes.Red;

            ValidationMessage.Visibility = Visibility.Visible;
        }


        private void CustomerTypeComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (CorporateFields == null)
                return;

            // Töm alla fält när kundtypen ändras.
            FirstNameTextBox.Clear();
            LastNameTextBox.Clear();
            AddressTextBox.Clear();
            EmailTextBox.Clear();
            PhoneTextBox.Clear();

            CompanyNameTextBox.Clear();
            OrgNumberTextBox.Clear();
            ContactPersonTextBox.Clear();

            // Töm valideringsmeddelanden.
            ClearFieldErrors();

            // Dölj tidigare valideringsmeddelanden.
            ValidationMessage.Text = "";
            ValidationMessage.Visibility = Visibility.Collapsed;

            // Visa företagsfält endast för företagskunder.
            CorporateFields.Visibility =
                CustomerTypeComboBox.SelectedIndex == 1
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        //Avbryt knapppen
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }







    }
}
