namespace PresentationLayer.Models
{
    /// <summary>
    /// One row in the staff list: an account with its Swedish role name and status.
    /// </summary>
    public class StaffRow
    {
        public int StaffID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Status => IsActive ? "Aktiv" : "Inaktiverad";
    }
}
