using EntityLayer;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DataLayer
{
    public static class DbInitializer
    {
        public static void Initialize(SkiCenterDbContext context)
        {
            // =========================================================================
            // 1. KONTROLLERA OM KONFERENS REDAN FINNS
            // =========================================================================
            if (!context.Set<MeetingRoom>().Any())
            {
                // =========================================================================
                // 2. SKAPA DE 8 LOKALERNA (KLS1-3 och KLL1-5)
                // =========================================================================
                var rooms = new List<MeetingRoom>();

                // 3 Stora lokaler
                for (int i = 1; i <= 3; i++)
                {
                    rooms.Add(new MeetingRoom($"KLS{i}", 50, "Konferenslokal stor", MeetingRoomStatus.Available));
                }

                // 5 Lilla lokaler
                for (int i = 1; i <= 5; i++)
                {
                    rooms.Add(new MeetingRoom($"KLL{i}", 20, "Konferenslokal lilla", MeetingRoomStatus.Available));
                }

                context.Set<MeetingRoom>().AddRange(rooms);
                context.SaveChanges(); // Få databas-ID:n för lokalerna


                // =========================================================================
                // 3. SKAPA ALLA 816 SÄSONGSPRISER FÖR KONFERENSERNA
                // =========================================================================
                var weekSlots = new (int StartWeek, int EndWeek)[]
                {
        (51, 51), (52, 52), (1, 1), (2, 2), (3, 3), (4, 4), (5, 5), (6, 6),
        (7, 7), (8, 8), (9, 9), (10, 10), (11, 11), (12, 12), (13, 13),
        (14, 22), (23, 50)
                };

                var priceRows = new (string Description, string PriceType, decimal[] Prices)[]
                {
        // 50 pers (Stor lokal)
        ("Konferenslokal stor", "vecka", new decimal[] { 4500, 4500, 7500, 4500, 4500, 4500, 4500, 4500, 7500, 7500, 4500, 4500, 4500, 4500, 7500, 4500, 3500 }),
        ("Konferenslokal stor", "dygn",  new decimal[] { 1200, 1200, 1500, 1200, 1200, 1200, 1200, 1200, 1500, 1500, 1200, 1200, 1200, 1200, 1500, 1200,  800 }),
        ("Konferenslokal stor", "tim",   new decimal[] {  250,  250,  300,  250,  250,  250,  250,  250,  300,  300,  250,  250,  250,  250,  300,  250,  200 }),

        // 20 pers (Liten lokal)
        ("Konferenslokal lilla", "vecka", new decimal[] { 3500, 3500, 6000, 3500, 3500, 3500, 3500, 3500, 6000, 6000, 3500, 3500, 3500, 3500, 6000, 3500, 2500 }),
        ("Konferenslokal lilla", "dygn",  new decimal[] {  850,  850, 1150,  850,  850,  850,  850,  850, 1150, 1150,  850,  850,  850,  850, 1150,  850,  650 }),
        ("Konferenslokal lilla", "tim",   new decimal[] {  155,  155,  200,  155,  155,  155,  155,  155,  200,  200,  155,  155,  155,  155,  200,  155,  115 })
                };

                var meetingPrices = new List<MeetingRoomSeasonPrice>();
                var allSavedRooms = context.Set<MeetingRoom>().ToList();

                foreach (var row in priceRows)
                {
                    // Hitta alla rum som matchar typen (t.ex. alla KLS)
                    var matchingRooms = allSavedRooms.Where(r => r.Description == row.Description).ToList();

                    for (int i = 0; i < weekSlots.Length; i++)
                    {
                        var slot = weekSlots[i];
                        var price = row.Prices[i];

                        for (int w = slot.StartWeek; w <= slot.EndWeek; w++)
                        {
                            foreach (var room in matchingRooms)
                            {
                                meetingPrices.Add(new MeetingRoomSeasonPrice
                                {
                                    MeetingRoomID = room.MeetingRoomID,
                                    PriceType = row.PriceType,
                                    WeekNumber = w,
                                    Price = price
                                });
                            }
                        }
                    }
                }

                context.Set<MeetingRoomSeasonPrice>().AddRange(meetingPrices);
                context.SaveChanges();
            }
        }
    }
}