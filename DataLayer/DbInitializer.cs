using EntityLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataLayer
{
    public static class DbInitializer
    {
        public static void Initialize(SkiCenterDbContext context)
        {
            // Kontrollera om boendedata redan finns i databasen
            if (context.Set<AccommodationType>().Any())
            {
                return; // Redan seedat, avbryt omedelbart
            }

            // =========================================================================
            // STEG 1: SKAPA OCH SPARA BOENDETYPERNA (AccommodationType)
            // =========================================================================
            var lgh1 = new AccommodationType("LGH.I", "Lägenhet 50 kvm", 4);
            var lgh2 = new AccommodationType("LGH.II", "Lägenhet 20 kvm", 2);
            var camp = new AccommodationType("Camp", "Campingplats", 0);

            context.Set<AccommodationType>().AddRange(lgh1, lgh2, camp);
            context.SaveChanges(); // SQL Server genererar AccommodationTypeID:n


            // =========================================================================
            // STEG 2: LÄS IN OCH SKAPA SÄSONGSPRISER (SeasonPrices - 408 rader)
            // =========================================================================
            string priceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeedData", "SeasonPrices - SkiCenter.csv");

            if (File.Exists(priceFilePath))
            {
                var lines = File.ReadAllLines(priceFilePath).Skip(1); // Hoppa över header-raden
                var allTypes = context.Set<AccommodationType>().ToList();
                var seasonPrices = new List<SeasonPrice>();

                foreach (var rawLine in lines)
                {
                    if (string.IsNullOrWhiteSpace(rawLine)) continue;

                    var cleanLine = rawLine.Trim().Trim('"');
                    var cols = cleanLine.Split(',').Select(c => c.Trim('"', ' ')).ToArray();

                    if (cols.Length >= 4 &&
                        int.TryParse(cols[2], out int weekNumber) &&
                        decimal.TryParse(cols[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price))
                    {
                        string categoryCode = cols[0];
                        string priceType = cols[1];

                        var matchedType = allTypes.FirstOrDefault(t =>
                            t.CategoryCode.Equals(categoryCode, StringComparison.OrdinalIgnoreCase));

                        if (matchedType != null)
                        {
                            seasonPrices.Add(new SeasonPrice
                            {
                                AccommodationTypeID = matchedType.AccommodationTypeID,
                                WeekNumber = weekNumber,
                                PriceType = priceType,
                                Price = price
                            });
                        }
                    }
                }

                if (seasonPrices.Count > 0)
                {
                    context.Set<SeasonPrice>().AddRange(seasonPrices);
                    context.SaveChanges();
                }
            }


            // =========================================================================
            // STEG 3: SKAPA ALLA 210 FYSISKA BOENDEN (Accommodation)
            // =========================================================================
            var accommodations = new List<Accommodation>();

            // Lägenhet I: LI1 - LI50 (50 st)
            for (int i = 1; i <= 50; i++)
            {
                accommodations.Add(new Accommodation
                {
                    AccommodationNumber = $"LI{i}",
                    Status = AccommodationStatus.Available,
                    AccommodationTypeID = lgh1.AccommodationTypeID
                });
            }

            // Lägenhet II: LII1 - LII35 (35 st)
            for (int i = 1; i <= 35; i++)
            {
                accommodations.Add(new Accommodation
                {
                    AccommodationNumber = $"LII{i}",
                    Status = AccommodationStatus.Available,
                    AccommodationTypeID = lgh2.AccommodationTypeID
                });
            }

            // Camping: Camp1 - Camp125 (125 st)
            for (int i = 1; i <= 125; i++)
            {
                accommodations.Add(new Accommodation
                {
                    AccommodationNumber = $"Camp{i}",
                    Status = AccommodationStatus.Available,
                    AccommodationTypeID = camp.AccommodationTypeID
                });
            }

            context.Set<Accommodation>().AddRange(accommodations);
            context.SaveChanges();
        }
    }
}