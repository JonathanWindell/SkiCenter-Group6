using EntityLayer;

namespace PresentationLayer.Models
{
    public class ReturnItemRow
    {
        public int EquipmentItemID { get; set; }
        public string ArticleNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int? BookingID { get; set; }
        public DateTime? PlannedReturnDate { get; set; }
        public bool IsDelayed => PlannedReturnDate.HasValue && DateTime.Today > PlannedReturnDate.Value.Date;
        public int DaysDelayed => IsDelayed ? (DateTime.Today - PlannedReturnDate!.Value.Date).Days : 0;
        public EquipmentCondition Condition { get; set; } = EquipmentCondition.BarelyUsed;
        public bool IsDamaged { get; set; } = false;
    }
}
