namespace PresentationLayer.Models
{
    public class PropertyRow
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public DateTime? NextAvailableDate { get; set; }

        // Original status, to detect changes
        public string OriginalStatus { get; set; } = string.Empty;

        // Choosen status in ComboBox
        public string Status { get; set; } = string.Empty;

        // Available choices for current row
        public List<string> AvailableStatuses { get; set; } = new();

        // Help property, to see if the row changes
        public bool IsChanged => Status != OriginalStatus;
    }
}
