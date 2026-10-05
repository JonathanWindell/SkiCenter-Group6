using EntityLayer;

namespace DataLayer
{
    public static class DbInitializer
    {
        public static void Initialize(SkiCenterDbContext context)
        {
            if (context.Set<Equipment>().Any())
            {
                return;
            }

            var alpintSkidor = new Equipment("Skidor", "Alpint");
            var alpintPjaxor = new Equipment("Pjäxor", "Alpint");
            var alpintStavar = new Equipment("Stavar", "Alpint");
            var alpintPaket = new Equipment("Paket", "Alpint");

            var langdSkidor = new Equipment("Skidor", "Längd");
            var langdPjaxor = new Equipment("Pjäxor", "Längd");
            var langdStavar = new Equipment("Stavar", "Längd");
            var langdPaket = new Equipment("Paket", "Längd");

            var snowboard = new Equipment("Snowboard", "Snowboard");
            var snowboardskor = new Equipment("Skor", "Snowboard");
            var snowPaket = new Equipment("Paket", "Snowboard");

            var hjalm = new Equipment("Hjälm", "Hjälm");
            var skoterLynx = new Equipment("Lynx 50", "Skoter");
            var skoterYamaha = new Equipment("Yamaha Viking", "Skoter");
            var pulka = new Equipment("Nilapulka", "Pulka");

            context.Set<Equipment>().AddRange(
                alpintSkidor, alpintPjaxor, alpintStavar, alpintPaket,
                langdSkidor, langdPjaxor, langdStavar, langdPaket,
                snowboard, snowboardskor, snowPaket,
                hjalm, skoterLynx, skoterYamaha, pulka
            );
            context.SaveChanges();

            var prices = new List<EquipmentPriceMatrix>
            {

                new EquipmentPriceMatrix { EquipmentID = alpintSkidor.EquipmentID, Days = 1, TotalAmount = 130 },
                new EquipmentPriceMatrix { EquipmentID = alpintSkidor.EquipmentID, Days = 2, TotalAmount = 230 },
                new EquipmentPriceMatrix { EquipmentID = alpintSkidor.EquipmentID, Days = 3, TotalAmount = 330 },
                new EquipmentPriceMatrix { EquipmentID = alpintSkidor.EquipmentID, Days = 4, TotalAmount = 395 },
                new EquipmentPriceMatrix { EquipmentID = alpintSkidor.EquipmentID, Days = 5, TotalAmount = 445 },

                new EquipmentPriceMatrix { EquipmentID = alpintPjaxor.EquipmentID, Days = 1, TotalAmount = 115 },
                new EquipmentPriceMatrix { EquipmentID = alpintPjaxor.EquipmentID, Days = 2, TotalAmount = 195 },
                new EquipmentPriceMatrix { EquipmentID = alpintPjaxor.EquipmentID, Days = 3, TotalAmount = 255 },
                new EquipmentPriceMatrix { EquipmentID = alpintPjaxor.EquipmentID, Days = 4, TotalAmount = 315 },
                new EquipmentPriceMatrix { EquipmentID = alpintPjaxor.EquipmentID, Days = 5, TotalAmount = 375 },

                new EquipmentPriceMatrix { EquipmentID = skoterLynx.EquipmentID, Days = 1, TotalAmount = 1000 },
                new EquipmentPriceMatrix { EquipmentID = skoterLynx.EquipmentID, Days = 3, TotalAmount = 2750 },
                new EquipmentPriceMatrix { EquipmentID = skoterLynx.EquipmentID, Days = 5, TotalAmount = 5950 }

            };
            context.Set<EquipmentPriceMatrix>().AddRange(prices);
            context.SaveChanges();


            var items = new List<EquipmentItem>();

            for (int i = 1; i <= 350; i++)
                items.Add(new EquipmentItem { ArticleNumber = $"AS{i}", EquipmentID = alpintSkidor.EquipmentID, Size = "Standard", Status = EquipmentStatus.Available, Condition = EquipmentCondition.New });

            for (int i = 1; i <= 150; i++)
                items.Add(new EquipmentItem { ArticleNumber = $"LS{i}", EquipmentID = langdSkidor.EquipmentID, Size = "Standard", Status = EquipmentStatus.Available, Condition = EquipmentCondition.New });

            for (int i = 1; i <= 500; i++)
                items.Add(new EquipmentItem { ArticleNumber = $"AP{i}", EquipmentID = alpintPjaxor.EquipmentID, Size = "Standard", Status = EquipmentStatus.Available, Condition = EquipmentCondition.New });

            for (int i = 1; i <= 200; i++)
                items.Add(new EquipmentItem { ArticleNumber = $"LP{i}", EquipmentID = langdPjaxor.EquipmentID, Size = "Standard", Status = EquipmentStatus.Available, Condition = EquipmentCondition.New });

            for (int i = 1; i <= 85; i++)
                items.Add(new EquipmentItem { ArticleNumber = $"SB{i}", EquipmentID = snowboard.EquipmentID, Size = "Standard", Status = EquipmentStatus.Available, Condition = EquipmentCondition.New });

            for (int i = 1; i <= 90; i++)
                items.Add(new EquipmentItem { ArticleNumber = $"SS{i}", EquipmentID = snowboardskor.EquipmentID, Size = "Standard", Status = EquipmentStatus.Available, Condition = EquipmentCondition.New });

            for (int i = 1; i <= 15; i++)
                items.Add(new EquipmentItem { ArticleNumber = $"S{i}", EquipmentID = skoterLynx.EquipmentID, Size = "Standard", Status = EquipmentStatus.Available, Condition = EquipmentCondition.New });

            context.Set<EquipmentItem>().AddRange(items);
            context.SaveChanges();
        }
    }
}