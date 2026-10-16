namespace BusinessLayer
{
    /// <summary>
    /// VAT rates per article type. All prices in the system include VAT.
    /// </summary>
    public static class VatRates
    {
        public const decimal Accommodation = 0.12m;
        public const decimal SkiLesson = 0.06m;
        public const decimal EquipmentRental = 0.25m;
        public const decimal MeetingRoom = 0.25m;
        public const decimal CancellationInsurance = 0m;

        /// <summary>
        /// Splits a price including VAT into the amount excluding VAT and the VAT amount.
        /// </summary>
        public static (decimal amountExclVat, decimal vatAmount) SplitVat(decimal amountInclVat, decimal vatRate)
        {
            decimal amountExclVat = Math.Round(amountInclVat / (1 + vatRate), 2, MidpointRounding.AwayFromZero);
            return (amountExclVat, amountInclVat - amountExclVat);
        }
    }
}
