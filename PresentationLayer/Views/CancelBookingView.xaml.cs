using System.Windows;

namespace PresentationLayer.Views
{
    public partial class CancelBookingView : Window
    {
        #region Constructor

        public CancelBookingView(
        string bookingSummary,
        string refundInformation)
        {
            InitializeComponent();

            BookingSummaryTextBlock.Text = bookingSummary;
            RefundInformationTextBlock.Text = refundInformation;

            KeepBookingButton.Click += KeepBookingButton_Click;
            ConfirmCancellationButton.Click +=
            ConfirmCancellationButton_Click;

            // Focus the safe action when the dialog opens.
            Loaded += (_, _) => KeepBookingButton.Focus();
        }

        #endregion

        #region Actions

        private void KeepBookingButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ConfirmCancellationButton_Click(object sender, RoutedEventArgs e)
        {
            // Return confirmation to the booking view.
            DialogResult = true;
        }

        #endregion
    }
}