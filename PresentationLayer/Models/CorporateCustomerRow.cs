using System.ComponentModel;

namespace PresentationLayer.Models
{
    public class CorporateCustomerRow : INotifyPropertyChanged
    {
        private decimal _creditLimit;
        private decimal _discountRate;
        private bool _isApproved;

        public int CustomerId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string OrgNumber { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status => IsApproved ? "Godkänd" : "Väntar";

        // Original values to track changes
        public decimal OriginalCreditLimit { get; set; }
        public decimal OriginalDiscountRate { get; set; }
        public bool OriginalIsApproved { get; set; }

        public decimal CreditLimit
        {
            get => _creditLimit;
            set
            {
                if (_creditLimit != value)
                {
                    _creditLimit = value;
                    OnPropertyChanged(nameof(CreditLimit));
                    OnPropertyChanged(nameof(IsChanged));
                }
            }
        }

        public decimal DiscountRate
        {
            get => _discountRate;
            set
            {
                if (_discountRate != value)
                {
                    _discountRate = value;
                    OnPropertyChanged(nameof(DiscountRate));
                    OnPropertyChanged(nameof(IsChanged));
                }
            }
        }

        public bool IsApproved
        {
            get => _isApproved;
            set
            {
                if (_isApproved != value)
                {
                    _isApproved = value;
                    OnPropertyChanged(nameof(IsApproved));
                    OnPropertyChanged(nameof(IsChanged));
                }
            }
        }

        // Help property, to see if the row changes
        public bool IsChanged =>
            CreditLimit != OriginalCreditLimit ||
            DiscountRate != OriginalDiscountRate ||
            IsApproved != OriginalIsApproved;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
