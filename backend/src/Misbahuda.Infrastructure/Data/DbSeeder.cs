using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        await context.Database.MigrateAsync();

        if (!await context.Users.AnyAsync())
        {
            logger.LogInformation("Seeding default users...");

            var superAdmin = new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                FullName = "Super Administrator",
                Email = "superadmin@misbahuda.com",
                PhoneNumber = "+923001234567",
                WhatsAppNumber = "+923001234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@2026!"),
                Role = UserRole.SuperAdmin,
                IsActive = true,
                IsEmailVerified = true
            };

            var admin = new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                FullName = "Administrator",
                Email = "admin@misbahuda.com",
                PhoneNumber = "+923001234568",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@2026!"),
                Role = UserRole.Admin,
                IsActive = true,
                IsEmailVerified = true
            };

            await context.Users.AddRangeAsync(superAdmin, admin);
            await context.SaveChangesAsync();

            logger.LogInformation("Default users seeded successfully.");
        }

        if (!await context.Hotels.AnyAsync())
        {
            logger.LogInformation("Seeding Misbah ul Hoda hotels from 2025 guide...");

            // ── Karbala: Hotel Al-Jaber (PDF page 6) ──────────────────
            var hotel = new Hotel
            {
                Name = "Hotel Al-Jaber",
                Address = "Al Najareen Street, near Imam Hussain Shrine",
                City = "Karbala",
                PhoneNumber = "+9647801022244",
                ContactPerson = "Al-Jaber Management",
                TotalBuildings = 1,
                Latitude = 32.6157, Longitude = 44.0287,
                NightsLabel = "4 nights (Nights 9–12)",
                NearHaram = "10 min walk to Haram of Imam Hussain (AS)",
                HaramDistanceText = "~900 m from Haram of Imam Hussain (AS)",
                HaramLatitude = 32.6167, HaramLongitude = 44.0323,
                IconEmoji = "🌹", ColorClass = "border-red-800 bg-red-950",
                Amenities = "Breakfast & Dinner,WiFi,Air conditioning,Rooftop access,Prayer hall",
                Tips = "On Arbaeen day, start for Haram very early (before Fajr) to get good access. The crowds are immense but beautiful.",
                AdhanFajr = "4:00 AM", JamatFajr = "4:55 AM",
                AdhanZuhr = "12:05 PM", JamatZuhr = "12:55 PM",
                AdhanAsr = "3:45 PM", JamatAsr = "3:55 PM",
                AdhanMaghrib = "7:18 PM", JamatMaghrib = "7:28 PM",
                AdhanIsha = "8:55 PM", JamatIsha = "9:10 PM",
            };

            await context.Hotels.AddAsync(hotel);
            await context.SaveChangesAsync();

            var building = new Building
            {
                Name = "Building A",
                TotalFloors = 5,
                HotelId = hotel.Id
            };

            await context.Buildings.AddAsync(building);
            await context.SaveChangesAsync();

            var floors = Enumerable.Range(1, 5).Select(f => new Floor
            {
                FloorNumber = f,
                Label = $"Floor {f}",
                BuildingId = building.Id
            }).ToList();

            await context.Floors.AddRangeAsync(floors);
            await context.SaveChangesAsync();

            var rooms = new List<Room>();
            foreach (var floor in floors)
            {
                for (int r = 1; r <= 10; r++)
                {
                    rooms.Add(new Room
                    {
                        RoomNumber = $"A{floor.FloorNumber}{r:D2}",
                        BedCapacity = 4,
                        IsForFamily = r <= 3,
                        FloorId = floor.Id
                    });
                }
            }

            await context.Rooms.AddRangeAsync(rooms);
            await context.SaveChangesAsync();

            // ── Kadhimiyah: Hotel Al-Khafaji (PDF page 4) ─────────────
            var hotelKadhimiyah = new Hotel
            {
                Name = "Hotel Al-Khafaji",
                Address = "Shareh Babul Murad, near Al-Khafaji Sign, Kadhimiyah",
                City = "Kadhimiyah, Baghdad",
                PhoneNumber = "+9647713332249",
                ContactPerson = "Al-Khafaji Management",
                TotalBuildings = 1,
                Latitude = 33.3807, Longitude = 44.3392,
                NightsLabel = "2 nights (Nights 1–3)",
                NearHaram = "5 min walk to Haram of Kadhimain",
                HaramDistanceText = "~400 m from Kadhimain Shrine",
                HaramLatitude = 33.3815, HaramLongitude = 44.3387,
                IconEmoji = "🕌", ColorClass = "border-gold-700 bg-gold-950",
                Amenities = "Breakfast included,WiFi,Air conditioning,24hr reception,Prayer hall",
                Tips = "The Haram is walking distance. Best to go for Fajr Ziyarat before the crowds arrive.",
                AdhanFajr = "4:05 AM", JamatFajr = "5:00 AM",
                AdhanZuhr = "12:10 PM", JamatZuhr = "1:00 PM",
                AdhanAsr = "3:50 PM", JamatAsr = "4:00 PM",
                AdhanMaghrib = "7:22 PM", JamatMaghrib = "7:30 PM",
                AdhanIsha = "9:00 PM", JamatIsha = "9:15 PM",
            };
            await context.Hotels.AddAsync(hotelKadhimiyah);
            await context.SaveChangesAsync();

            var bldgK = new Building { Name = "Main Building", TotalFloors = 4, HotelId = hotelKadhimiyah.Id };
            await context.Buildings.AddAsync(bldgK);
            await context.SaveChangesAsync();

            var floorsK = Enumerable.Range(1, 4).Select(f => new Floor
            { FloorNumber = f, Label = $"Floor {f}", BuildingId = bldgK.Id }).ToList();
            await context.Floors.AddRangeAsync(floorsK);
            await context.SaveChangesAsync();

            var roomsK = new List<Room>();
            foreach (var fl in floorsK)
                for (int r = 1; r <= 8; r++)
                    roomsK.Add(new Room { RoomNumber = $"K{fl.FloorNumber}{r:D2}", BedCapacity = 4, IsForFamily = r <= 2, FloorId = fl.Id });
            await context.Rooms.AddRangeAsync(roomsK);
            await context.SaveChangesAsync();

            // ── Najaf: Hotel Luluat Al-Bahar (PDF page 5) ─────────────
            var hotelNajaf = new Hotel
            {
                Name = "Hotel Luluat Al-Bahar",
                Address = "Beginning of Al-Rasool Street, Al-Najaf Al-Ashraf",
                City = "Najaf",
                PhoneNumber = "+9647800179999",
                ContactPerson = "Luluat Al-Bahar Management",
                TotalBuildings = 1,
                Latitude = 31.9977, Longitude = 44.3120,
                NightsLabel = "3 nights (Nights 4–6)",
                NearHaram = "8 min walk to Haram of Imam Ali (AS)",
                HaramDistanceText = "~700 m from Haram of Imam Ali (AS)",
                HaramLatitude = 31.9960, HaramLongitude = 44.3155,
                IconEmoji = "🏨", ColorClass = "border-primary-700 bg-primary-950",
                Amenities = "Breakfast included,WiFi,Air conditioning,Luggage storage,Prayer hall",
                Tips = "Night Ziyarat of Imam Ali (AS) is especially spiritually powerful. Hotel staff speak Urdu.",
                AdhanFajr = "4:10 AM", JamatFajr = "5:05 AM",
                AdhanZuhr = "12:15 PM", JamatZuhr = "1:00 PM",
                AdhanAsr = "3:55 PM", JamatAsr = "4:05 PM",
                AdhanMaghrib = "7:25 PM", JamatMaghrib = "7:35 PM",
                AdhanIsha = "9:05 PM", JamatIsha = "9:20 PM",
            };
            await context.Hotels.AddAsync(hotelNajaf);
            await context.SaveChangesAsync();

            var bldgN = new Building { Name = "Main Building", TotalFloors = 5, HotelId = hotelNajaf.Id };
            await context.Buildings.AddAsync(bldgN);
            await context.SaveChangesAsync();

            var floorsN = Enumerable.Range(1, 5).Select(f => new Floor
            { FloorNumber = f, Label = $"Floor {f}", BuildingId = bldgN.Id }).ToList();
            await context.Floors.AddRangeAsync(floorsN);
            await context.SaveChangesAsync();

            var roomsN = new List<Room>();
            foreach (var fl in floorsN)
                for (int r = 1; r <= 8; r++)
                    roomsN.Add(new Room { RoomNumber = $"N{fl.FloorNumber}{r:D2}", BedCapacity = 4, IsForFamily = r <= 2, FloorId = fl.Id });
            await context.Rooms.AddRangeAsync(roomsN);
            await context.SaveChangesAsync();

            logger.LogInformation("Hotels seeded: Al-Jaber (Karbala), Al-Khafaji (Kadhimiyah), Luluat Al-Bahar (Najaf) — total 3 hotels.");
        }

        if (!await context.Scholars.AnyAsync())
        {
            logger.LogInformation("Seeding scholars from Misbah ul Hoda 2025 guide...");

            var scholars = new List<Scholar>
            {
                // ── Scholars ──────────────────────────────────────────────
                new Scholar
                {
                    FullName = "Shaykh Usama Abdulghani",
                    Title = "Shaykh",
                    Specialty = "Islamic Law & Jurisprudence",
                    Language = "English",
                    Location = "United States",
                    Bio = "Shaykh Usama AbdulGhani was born in Washington DC. After briefly studying Islam at a seminary in the US, he moved to Iran to pursue his studies at the Islamic Seminary in Qom. Shaykh Usama studied there for over 20 years. His studies focused on Islamic Law and Principles of Islamic Jurisprudence. Shaykh Usama currently resides in Dearborn, MI, USA with his family, where he plays a leading role in community building activities at the Hadi Institute. Shaykh Usama plays a vital role in his community by focusing on spiritual growth, youth education, family ethics and counseling. In addition, he frequently visits major cities throughout the world to deliver his message of justice, unity, respect and family values.",
                    Quote = "Knowledge without action is like a tree without fruit.",
                    Tags = "English Lectures,Youth Education,Family Ethics,USA Based,Hadi Institute,Islamic Law",
                    YoutubeSearch = "Shaykh Usama Abdulghani lectures",
                    IsReciter = false,
                    IsActive = true,
                    SortOrder = 1
                },
                new Scholar
                {
                    FullName = "Sayyid Hassan",
                    Title = "Sayyid",
                    Specialty = "Islamic Jurisprudence & Philosophy",
                    Language = "English",
                    Location = "United States",
                    Bio = "Sayyid Hassan was born in New York, USA. He completed his undergraduate degree in Finance before moving to the Islamic Seminary in Qom, Iran in 2010 to pursue full-time religious studies. He has since completed a Masters degree in Islamic Jurisprudence and Philosophy. Sayyid Hassan is known for his ability to connect classical Islamic scholarship with contemporary issues relevant to communities in the West.",
                    Tags = "English Lectures,USA Based,Islamic Philosophy,Jurisprudence,Contemporary Issues",
                    YoutubeSearch = "Sayyid Hassan Islamic lectures",
                    IsReciter = false,
                    IsActive = true,
                    SortOrder = 2
                },
                new Scholar
                {
                    FullName = "Sayyid Ali Zaidi",
                    Title = "Sayyid",
                    Specialty = "Islamic Jurisprudence",
                    Language = "English",
                    Location = "United Arab Emirates",
                    Bio = "Sayyid Ali Zaidi was born and raised in Dubai, UAE. He holds a degree in Engineering before pursuing Islamic studies at the Hawza in Qom, Iran since 2007. He has completed a Masters degree in Islamic Jurisprudence. Sayyid Ali Zaidi brings a unique perspective combining analytical thinking from his engineering background with deep Islamic scholarship, making complex religious concepts accessible to modern audiences.",
                    Tags = "English Lectures,UAE Based,Engineering Background,Islamic Jurisprudence,Qom Graduate",
                    YoutubeSearch = "Sayyid Ali Zaidi lectures",
                    IsReciter = false,
                    IsActive = true,
                    SortOrder = 3
                },
                new Scholar
                {
                    FullName = "Shk. Fadhl Abbas",
                    Title = "Shaykh",
                    Specialty = "Islamic Law & Philosophy",
                    Language = "English",
                    Location = "United Kingdom",
                    Bio = "Shaykh Fadhl Abbas holds an MSc from Imperial College London before dedicating himself to Islamic studies. He moved to Qom in 2013 to study at the prestigious Islamic Seminary. His studies focus on Islamic Law, Philosophy, and Quranic Sciences. Based in the United Kingdom, Shaykh Fadhl Abbas is known for his eloquent delivery and his ability to bridge the gap between Western academic thought and classical Islamic scholarship.",
                    Tags = "English Lectures,UK Based,Imperial College,Quran Sciences,Islamic Philosophy,Law",
                    YoutubeSearch = "Shaykh Fadhl Abbas lectures",
                    IsReciter = false,
                    IsActive = true,
                    SortOrder = 4
                },
                new Scholar
                {
                    FullName = "Syed Nusrat Bukhari",
                    Title = "Syed",
                    Specialty = "Religious Harmony & Ethics",
                    Language = "Urdu",
                    Location = "Pakistan",
                    Bio = "Syed Nusrat Bukhari has spent over 15 years studying at the Hawza in Qom, Iran. He is widely respected for his work in promoting religious harmony, world peace, and ethical values. His lectures particularly focus on youth motivation, moral development, and the importance of unity among Muslims. He has delivered lectures across Pakistan, Europe, and North America, inspiring thousands with his message of peace and spirituality.",
                    Tags = "Urdu Lectures,Pakistan Based,Religious Harmony,Youth Motivation,Ethics,World Peace",
                    YoutubeSearch = "Syed Nusrat Bukhari lectures",
                    IsReciter = false,
                    IsActive = true,
                    SortOrder = 5
                },
                new Scholar
                {
                    FullName = "Syed Zameer Jaffri",
                    Title = "Syed",
                    Specialty = "Contemporary Islamic Studies",
                    Language = "Urdu",
                    Location = "India",
                    Bio = "Syed Zameer Jaffri holds a B.Com degree from Mumbai and spent 7 years in business in the UK and India before answering the call to religious scholarship. He has been studying at the Hawza in Qom for 11 years. As a founding member of the Sabeel Media Group, he has worked tirelessly to spread Islamic knowledge through modern media. His lectures are delivered across India and internationally, covering topics from Islamic history to contemporary Muslim affairs.",
                    Tags = "Urdu Lectures,India Based,Sabeel Media,Business Background,Contemporary Issues,Media",
                    YoutubeSearch = "Syed Zameer Jaffri lectures",
                    IsReciter = false,
                    IsActive = true,
                    SortOrder = 6
                },

                // ── Reciters (Noha Khuwaan) ──────────────────────────────
                new Scholar
                {
                    FullName = "Sayed Zaigham Ali Shan",
                    Title = "Sayed",
                    Specialty = "English Noha & Marsiya",
                    Language = "English",
                    Location = "United Kingdom",
                    Bio = "Sayed Zaigham Ali Shan was born and raised in Manchester, England. He began his journey in English noha recitation at the age of 16 and has since become one of the most recognised English noha khuwaan in the world. He studied at a London university and has dedicated his life to spreading the message of Karbala through the medium of English poetry and recitation. His unique style and heartfelt delivery have moved audiences across the UK, USA, Canada, and beyond.",
                    Tags = "English Noha,UK Based,Manchester,Youth Reciter,English Marsiya",
                    YoutubeSearch = "Sayed Zaigham Ali Shan noha",
                    IsReciter = true,
                    IsActive = true,
                    SortOrder = 7
                },
                new Scholar
                {
                    FullName = "Sayed Raza Haider",
                    Title = "Sayed",
                    Specialty = "Urdu Noha & Marsiya",
                    Language = "Urdu",
                    Location = "Pakistan",
                    Bio = "Sayed Raza Haider is a highly respected Urdu Noha Khuwan who has been reciting since 2004. Originally from Pakistan, he moved to Ireland and then England in 2012, where he has since become a leading figure in Urdu noha recitation in the diaspora community. His powerful voice and deep connection to the tragedies of Karbala make his recitations deeply moving and spiritually uplifting for audiences across the UK and around the world.",
                    Tags = "Urdu Noha,Pakistan Based,UK Diaspora,Ireland,Majlis Reciter",
                    YoutubeSearch = "Sayed Raza Haider noha",
                    IsReciter = true,
                    IsActive = true,
                    SortOrder = 8
                },
                new Scholar
                {
                    FullName = "S. Murtaza Zaidi",
                    Title = "Sayed",
                    Specialty = "Marsiya & Classical Noha",
                    Language = "Urdu",
                    Location = "Pakistan",
                    Bio = "Sayed Murtaza Zaidi holds a degree in Chemical Engineering from NED University, Karachi. He has been deeply involved in Sozkhwani (the recitation of elegies) for over 18 years. He is a prominent member of Anjuman Baqir ul Uloom and the Pakistan Youth Literary Society. Coming from a long lineage of reciters, Sayed Murtaza brings depth, authenticity, and classical training to every recitation. His Marsiya recitations honour the centuries-old tradition of commemorating the martyrs of Karbala through classical Urdu verse, making each session a profound spiritual experience.",
                    Tags = "Classical Marsiya,Pakistan,Heritage Style,NED University,Sozkhwani,Anjuman Baqir ul Uloom",
                    YoutubeSearch = "Sayed Murtaza Zaidi marsiya",
                    IsReciter = true,
                    IsActive = true,
                    SortOrder = 9
                },
            };

            await context.Scholars.AddRangeAsync(scholars);
            await context.SaveChangesAsync();

            logger.LogInformation("Scholars seeded: {Count} entries added.", scholars.Count);
        }

        if (!await context.Tours.AnyAsync())
        {
            logger.LogInformation("Seeding Arbaeen 2026 tour and itinerary...");

            var tourId = Guid.Parse("10000000-0000-0000-0000-000000000001");

            var tour = new Tour
            {
                Id          = tourId,
                TourName    = "Arbaeen 2026",
                TourType    = TourType.Arbaeen,
                Year        = 2026,
                StartDate   = DateTime.SpecifyKind(new DateTime(2026, 7, 24), DateTimeKind.Utc),
                EndDate     = DateTime.SpecifyKind(new DateTime(2026, 8, 7), DateTimeKind.Utc),
                Status      = TourStatus.Upcoming,
                IsOpen      = true,
                Description = "15-day guided Arbaeen 2026 Ziyarat tour organized by Misbah ul Hoda. " +
                              "Visiting Kadhimiyyah (2 nights), Najaf (3 nights), and Karbala (8 nights). " +
                              "Includes nightly English & Urdu programmes, 3 meals daily, and local transportation.",
            };

            await context.Tours.AddAsync(tour);
            await context.SaveChangesAsync();

            // ── Packages ──────────────────────────────────────────────────────
            var packages = new List<TourPackage>
            {
                new TourPackage
                {
                    TourId      = tourId,
                    PackageName = "Standard Package",
                    Description = "Hotels, 3 meals daily, local transport, nightly programmes",
                    Price       = 3500,
                    Currency    = "GBP",
                    IsActive    = true,
                },
                new TourPackage
                {
                    TourId      = tourId,
                    PackageName = "Premium Package",
                    Description = "Premium hotels, 3 meals daily, private transport, nightly programmes, priority Ziyarat access",
                    Price       = 4500,
                    Currency    = "GBP",
                    IsActive    = true,
                },
                new TourPackage
                {
                    TourId      = tourId,
                    PackageName = "Land Only",
                    Description = "Hotels + meals only (no transport). Flights arranged independently.",
                    Price       = 2200,
                    Currency    = "GBP",
                    IsActive    = true,
                },
            };

            await context.TourPackages.AddRangeAsync(packages);
            await context.SaveChangesAsync();

            // ── 15-Day Itinerary ──────────────────────────────────────────────
            // Based on https://www.misbahulhoda.org/arbaeen-2026-itinerary/
            // Tour: Jul 24 – Aug 7, 2026
            // Kadhimiyyah: 2 nights | Najaf: 3 nights | Karbala: 8 nights
            static DateTime D(int month, int day) =>
                DateTime.SpecifyKind(new DateTime(2026, month, day), DateTimeKind.Utc);

            var schedule = new List<TourDaySchedule>
            {
                new TourDaySchedule { TourId=tourId, DayNumber=1,  Date=D(7,24),
                    City="London / Home City",   Country="UK",
                    Activity="Depart from home country. Group check-in at airport. Fly to Baghdad International Airport.",
                    ActivityUrdu="گھر سے روانگی۔ ایئرپورٹ پر گروپ چیک-ان۔",
                    Icon="✈️",  IsKeyDay=false, Notes="Ensure all documents — passport, visa, vaccination cards — are ready." },

                new TourDaySchedule { TourId=tourId, DayNumber=2,  Date=D(7,25),
                    City="Baghdad → Kadhimiyyah", Country="Iraq",
                    Hotel="Hotel Al-Khafaji",
                    Activity="Arrive Baghdad airport. Transfer to Kadhimiyyah. Check in to Hotel Al-Khafaji. Welcome dinner. Evening programme.",
                    ActivityUrdu="بغداد ایئرپورٹ آمد۔ کاظمیہ منتقلی۔ ہوٹل چیک-ان۔ استقبالیہ ڈنر۔",
                    Icon="🛬", IsKeyDay=true,  Notes="Keep group together at airport. Luggage will be loaded on coach." },

                new TourDaySchedule { TourId=tourId, DayNumber=3,  Date=D(7,26),
                    City="Kadhimiyyah",          Country="Iraq",
                    Hotel="Hotel Al-Khafaji",
                    Activity="Ziyarat of Imam Musa al-Kadhim (AS) & Imam Muhammad al-Jawad (AS). Morning & evening programmes. Free time for personal prayers.",
                    ActivityUrdu="امام موسیٰ کاظم (ع) اور امام محمد جواد (ع) کی زیارت۔ صبح و شام پروگرام۔",
                    Icon="🕌", IsKeyDay=false, Notes="The Haram is a 5-minute walk from the hotel." },

                new TourDaySchedule { TourId=tourId, DayNumber=4,  Date=D(7,27),
                    City="Kadhimiyyah → Najaf",  Country="Iraq",
                    Hotel="Hotel Luluat Al-Bahar",
                    Activity="Fajr Ziyarat. After breakfast, transfer by coach to Najaf al-Ashraf. Check in. Ziyarat of Imam Ali (AS).",
                    ActivityUrdu="فجر زیارت کے بعد نجف اشرف روانگی۔ ہوٹل لولوات الباحر چیک-ان۔ زیارت امام علی (ع)۔",
                    Icon="🚌", IsKeyDay=false, Notes="~2.5 hr coach journey from Kadhimiyyah to Najaf." },

                new TourDaySchedule { TourId=tourId, DayNumber=5,  Date=D(7,28),
                    City="Najaf",                Country="Iraq",
                    Hotel="Hotel Luluat Al-Bahar",
                    Activity="Full day in Najaf. Ziyarat of Imam Ali (AS) including night Ziyarat. Visit Wadi al-Salam cemetery. Evening programme with Maulana.",
                    ActivityUrdu="نجف میں مکمل دن۔ زیارت امام علی (ع)۔ وادی السلام قبرستان۔ مولانا کا پروگرام۔",
                    Icon="🏛️", IsKeyDay=false, Notes="Night Ziyarat of Imam Ali is deeply spiritual — highly recommended." },

                new TourDaySchedule { TourId=tourId, DayNumber=6,  Date=D(7,29),
                    City="Najaf",                Country="Iraq",
                    Hotel="Hotel Luluat Al-Bahar",
                    Activity="Visit Masjid Kufa & Masjid Sahla. Ziyarat of Muslim ibn Aqil (RA) in Kufa. Return to Najaf for evening programme.",
                    ActivityUrdu="مسجد کوفہ اور مسجد سہلہ کا دورہ۔ مسلم بن عقیل (ع) کی زیارت۔",
                    Icon="🕌", IsKeyDay=false, Notes="~20 min drive from Najaf to Kufa." },

                new TourDaySchedule { TourId=tourId, DayNumber=7,  Date=D(7,30),
                    City="Najaf → Karbala",      Country="Iraq",
                    Hotel="Hotel Al-Jaber",
                    Activity="Fajr Ziyarat of Imam Ali (AS). After breakfast, transfer by coach to Karbala. Check in to Hotel Al-Jaber. First Ziyarat of Imam Husayn (AS) & Hazrat Abbas (AS).",
                    ActivityUrdu="فجر زیارت کے بعد کربلا روانگی۔ ہوٹل الجابر چیک-ان۔ پہلی زیارت امام حسین (ع)۔",
                    Icon="🚌", IsKeyDay=true,  Notes="~1.5 hr coach journey. Keep passports accessible for checkpoints." },

                new TourDaySchedule { TourId=tourId, DayNumber=8,  Date=D(7,31),
                    City="Karbala",              Country="Iraq",
                    Hotel="Hotel Al-Jaber",
                    Activity="Full day Ziyarat. Visit Haram of Imam Husayn (AS) and Hazrat Abbas (AS). Evening Majlis with Maulana. Latmiyyat programme.",
                    ActivityUrdu="کربلا میں مکمل دن۔ امام حسین (ع) اور حضرت عباس (ع) کا حرم۔ مجلس و لطمیات۔",
                    Icon="🌹", IsKeyDay=false, Notes="Go for Fajr Ziyarat before the crowds arrive." },

                new TourDaySchedule { TourId=tourId, DayNumber=9,  Date=D(8,1),
                    City="Karbala",              Country="Iraq",
                    Hotel="Hotel Al-Jaber",
                    Activity="Ziyarat al-Arbaeen (early visit). Visit Shrine of Hazrat Ali al-Akbar (AS) and Hazrat Hurr (AS). Evening programme.",
                    ActivityUrdu="زیارت اربعین۔ حضرت علی اکبر (ع) اور حضرت حر (ع) کا مزار۔ شام پروگرام۔",
                    Icon="🕌", IsKeyDay=false, Notes="Visiting al-Hurr's shrine is recommended — it's 30 min from Karbala." },

                new TourDaySchedule { TourId=tourId, DayNumber=10, Date=D(8,2),
                    City="Karbala",              Country="Iraq",
                    Hotel="Hotel Al-Jaber",
                    Activity="Free day for personal Ziyarat and ibadah. Optional trip to Babylon ruins. Evening programme with Maulana.",
                    ActivityUrdu="ذاتی زیارت اور عبادت۔ اختیاری سفر بابل۔ مولانا کا پروگرام۔",
                    Icon="🏛️", IsKeyDay=false, Notes="This is a rest/free day for pilgrims to catch up on personal prayers." },

                new TourDaySchedule { TourId=tourId, DayNumber=11, Date=D(8,3),
                    City="Karbala",              Country="Iraq",
                    Hotel="Hotel Al-Jaber",
                    Activity="Arbaeen Walk preparation. Spiritual lecture on the meaning of Chehlum. Walk towards Haram. Full Majlis night programme.",
                    ActivityUrdu="پیدل چلنے کی تیاری۔ چہلم کی اہمیت پر لیکچر۔ حرم کی طرف پیدل سفر۔",
                    Icon="🚶", IsKeyDay=true,  Notes="Wear comfortable shoes. Carry water and snacks. Stay with the group." },

                new TourDaySchedule { TourId=tourId, DayNumber=12, Date=D(8,4),
                    City="Karbala",              Country="Iraq",
                    Hotel="Hotel Al-Jaber",
                    Activity="ARBAEEN WALK — The Great Walk to Karbala with millions of pilgrims. Arrive at Haram of Imam Husayn (AS) for Ziyarat of Chehlum.",
                    ActivityUrdu="اربعین واک — کروڑوں زائرین کے ساتھ کربلا کی طرف عظیم مارچ۔ زیارت چہلم۔",
                    Icon="🫀", IsKeyDay=true,  Notes="THE KEY DAY. Early Fajr start. Stay with group leader. Emergency contact numbers shared." },

                new TourDaySchedule { TourId=tourId, DayNumber=13, Date=D(8,5),
                    City="Karbala",              Country="Iraq",
                    Hotel="Hotel Al-Jaber",
                    Activity="ARBAEEN DAY (Chehlum of Imam Husayn AS — 20th Safar). Day of remembrance and Ziyarat. Special programme at Haram.",
                    ActivityUrdu="اربعین / چہلم امام حسین (ع) — ۲۰ صفر۔ یاد اور زیارت کا دن۔ حرم پر خصوصی پروگرام۔",
                    Icon="🌹", IsKeyDay=true,  Notes="Arbaeen 2026 — the holiest day of the tour. Millions will be present at the Haram." },

                new TourDaySchedule { TourId=tourId, DayNumber=14, Date=D(8,6),
                    City="Karbala → Baghdad",    Country="Iraq",
                    Hotel="Baghdad Airport Hotel",
                    Activity="Final Ziyarat of Imam Husayn (AS). Check out from hotel. Transfer by coach to Baghdad airport. Check-in for return flight.",
                    ActivityUrdu="آخری زیارت امام حسین (ع)۔ ہوٹل سے چیک آؤٹ۔ بغداد ایئرپورٹ روانگی۔",
                    Icon="🛫", IsKeyDay=false, Notes="Luggage will be collected early morning. Be ready by 8:00 AM." },

                new TourDaySchedule { TourId=tourId, DayNumber=15, Date=D(8,7),
                    City="Home City",             Country="UK",
                    Activity="Arrive back home. Tour ends. May Allah accept your Ziyarat — Ziyarat Maqboola!",
                    ActivityUrdu="وطن واپسی۔ سفر اختتام۔ اللہ تعالیٰ آپ کی زیارت قبول فرمائے۔",
                    Icon="🏠", IsKeyDay=false, Notes="زیارت مقبول!" },
            };

            await context.TourDaySchedules.AddRangeAsync(schedule);
            await context.SaveChangesAsync();

            logger.LogInformation("Arbaeen 2026 tour seeded: 1 tour, 3 packages, 15-day schedule.");
        }
    }
}
