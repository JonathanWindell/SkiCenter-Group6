using EntityLayer;

namespace DataLayer
{
    public class DbInitializer
    {
        public static void SeedData(SkiCenterDbContext context)
        {
            // Kontrollera om databasen redan har data, avbryt annars
            if (context.EquipmentItems.Any()) return;

            // Create EquipmentCategory
            var alpintCategory = new Equipment { Category = "Alpint", Description = "Skidor" };
            context.Equipment.Add(alpintCategory);
            context.SaveChanges();

            // 2. Läs din CSV-fil och skapa upp alla AS1, AS2, AS3...
            var lines = File.ReadAllLines("SeedData/SkidorAlpint.csv");

            foreach (var line in lines)
            {
                var itemCode = line.Trim(); // Ex: "AS1"
                if (!string.IsNullOrEmpty(itemCode))
                {
                    context.EquipmentItems.Add(new EquipmentItem
                    {
                        EquipmentID = alpintCategory.EquipmentID,
                        ArticleNumber = itemCode,
                        Status = equipmentStatus.Available,
                        Condition = equipmentCondition.Worn
                    });
                }
            }

            context.SaveChanges();
        }
    }
}
