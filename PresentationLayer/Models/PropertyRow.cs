using System.ComponentModel;
using System.Net.NetworkInformation;

namespace PresentationLayer.Models
{
    public class PropertyRow : INotifyPropertyChanged
    {
        private string _status = string.Empty;

        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Capacity { get; set; }

        // Original status, to detect changes
        public string OriginalStatus { get; set; } = string.Empty;

        // Choosen status in ComboBox
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(IsChanged));
                }
            }
        }

        // Available choices for current row
        public List<string> AvailableStatuses { get; set; } = new();

        // Help property, to see if the row changes
        public bool IsChanged => Status != OriginalStatus;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
