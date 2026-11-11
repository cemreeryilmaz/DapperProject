namespace SalesPulse.DataGen;

public sealed record CitySeed(int CityId, string Name, string Region, int Weight);

public static class Catalog
{
    public static readonly string[] FirstNames =
    [
        "Elif", "Zeynep", "Ayşe", "Fatma", "Merve", "Hatice", "Zehra", "Ece",
        "Selin", "Defne", "Elanur", "İrem", "Büşra", "Derya", "Gizem", "Ceren",
        "Ahmet", "Mehmet", "Mustafa", "Ali", "Hüseyin", "Emre", "Burak", "Can",
        "Yusuf", "Ömer", "Kerem", "Deniz", "Onur", "Serkan", "Hakan", "Cem",
        "Aslı", "Naz", "Melis", "Seda", "Pelin", "Yağmur", "Ebru", "Hande"
    ];

    public static readonly string[] LastNames =
    [
        "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Yıldız", "Yıldırım", "Öztürk",
        "Aydın", "Özdemir", "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin", "Koç",
        "Kurt", "Özkan", "Şimşek", "Polat", "Korkmaz", "Çakır", "Karaca", "Acar",
        "Erdoğan", "Yavuz", "Keskin", "Güneş", "Aksoy", "Bulut", "Kaplan", "Taş",
        "Uçar", "Sarı", "Bozkurt", "Duman", "Eren", "Sezer", "Ateş", "Akın"
    ];

    public static readonly string[] Brands =
    [
        "Pera", "Lumen", "Ada", "Mira", "Noya", "Kuzey", "Ege", "Defne",
        "Aras", "Sera", "Mavi", "Toprak", "Vega", "Lina", "Rota", "Minik",
        "Kent", "Nova", "Duru", "Pinari"
    ];

    public static readonly string[] Categories =
    [
        "Telefon ve Aksesuar",
        "Bilgisayar",
        "Televizyon ve Ses",
        "Beyaz Eşya",
        "Küçük Ev Aletleri",
        "Kadın Giyim",
        "Erkek Giyim",
        "Çocuk Giyim",
        "Ayakkabı",
        "Çanta ve Aksesuar",
        "Saat ve Takı",
        "Kozmetik",
        "Kişisel Bakım",
        "Spor ve Outdoor",
        "Kitap",
        "Kırtasiye",
        "Oyuncak",
        "Bebek",
        "Ev Tekstili",
        "Mobilya",
        "Mutfak",
        "Bahçe",
        "Market",
        "Pet Shop",
        "Otomotiv",
        "Yapı Market",
        "Oyun ve Hobi",
        "Müzik",
        "Sağlık",
        "Ofis"
    ];

    public static readonly string[][] ProductItems =
    [
        ["Kulaklık", "Kılıf", "Şarj Aleti", "Powerbank", "Ekran Koruyucu", "Telefon Tutucu"],
        ["Dizüstü", "Monitör", "Klavye", "Mouse", "SSD", "Webcam"],
        ["Soundbar", "Bluetooth Hoparlör", "Kulaklık", "Kumanda", "HDMI Kablo", "Projeksiyon"],
        ["Buzdolabı", "Çamaşır Makinesi", "Bulaşık Makinesi", "Fırın", "Kurutucu", "Derin Dondurucu"],
        ["Blender", "Kahve Makinesi", "Ütü", "Süpürge", "Tost Makinesi", "Kettle"],
        ["Elbise", "Triko", "Ceket", "Gömlek", "Pantolon", "Etek"],
        ["Gömlek", "Sweat", "Ceket", "Pantolon", "Tişört", "Mont"],
        ["Eşofman", "Sweat", "Tişört", "Mont", "Pijama", "Yağmurluk"],
        ["Spor Ayakkabı", "Bot", "Sandalet", "Klasik Ayakkabı", "Terlik", "Koşu Ayakkabısı"],
        ["Sırt Çantası", "Omuz Çantası", "Cüzdan", "Kemer", "Şal", "Bere"],
        ["Kol Saati", "Küpe", "Kolye", "Bileklik", "Yüzük", "Broş"],
        ["Ruj", "Fondöten", "Maskara", "Nemlendirici", "Parfüm", "Allık"],
        ["Şampuan", "Diş Fırçası", "Tıraş Makinesi", "Saç Kurutma", "Krem", "Deodorant"],
        ["Yoga Matı", "Dambıl", "Kamp Çadırı", "Termos", "Bisiklet Kaskı", "Koşu Bandı"],
        ["Roman", "Deneme", "Çocuk Kitabı", "Ajanda", "Atlas", "Şiir"],
        ["Defter", "Kalem Seti", "Dosya", "Yapışkan Not", "Makas", "Boyama Kalemi"],
        ["Yapboz", "Lego Seti", "Peluş", "Araba", "Bebek", "Kutu Oyunu"],
        ["Biberon", "Bebek Bezi", "Emzik", "Park Yatak", "Mama Sandalyesi", "Battaniye"],
        ["Nevresim", "Havlu", "Perde", "Yastık", "Battaniye", "Halı"],
        ["Berjer", "Kitaplık", "Sehpa", "Gardırop", "Sandalye", "Çalışma Masası"],
        ["Tencere", "Tava", "Bıçak Seti", "Saklama Kabı", "Bardak", "Servis Tabağı"],
        ["Saksı", "Bahçe Makası", "Mangal", "Sulama Kabı", "Tohum", "Bahçe Işığı"],
        ["Zeytinyağı", "Makarna", "Çay", "Kahve", "Granola", "Reçel"],
        ["Kedi Maması", "Köpek Maması", "Tasma", "Oyuncak", "Taşıma Çantası", "Mama Kabı"],
        ["Paspas", "Silecek", "Araç Kokusu", "Şarj Kablosu", "Branda", "Organizatör"],
        ["Matkap", "Tornavida", "Ampul", "Priz", "Boya", "Metre"],
        ["Konsol Oyunu", "Puzzle", "Maket", "Kart Oyunu", "Drone", "RC Araba"],
        ["Gitar", "Ukulele", "Mikrofon", "Nota Sehpası", "Pena Seti", "Kulaklık"],
        ["Tansiyon Aleti", "Vitamin", "Termometre", "Bandaj", "Masaj Aleti", "Nebülizatör"],
        ["Ofis Koltuğu", "Evrak Rafı", "Lamba", "Zımba", "Etiket", "Mousepad"]
    ];

    public static readonly decimal[] BasePrice =
    [
        0,
        14500, 22000, 9000, 16000, 1800,
        650, 550, 280, 900, 420,
        1400, 320, 180, 750, 90,
        45, 220, 350, 280, 4500,
        380, 260, 75, 240, 680,
        190, 550, 420, 160, 210
    ];

    public static readonly CitySeed[] Cities =
    [
        new(1, "Adana", "Akdeniz", 2270),
        new(2, "Adıyaman", "Güneydoğu Anadolu", 632),
        new(3, "Afyonkarahisar", "Ege", 747),
        new(4, "Ağrı", "Doğu Anadolu", 511),
        new(5, "Amasya", "Karadeniz", 335),
        new(6, "Ankara", "İç Anadolu", 5782),
        new(7, "Antalya", "Akdeniz", 2696),
        new(8, "Artvin", "Karadeniz", 169),
        new(9, "Aydın", "Ege", 1148),
        new(10, "Balıkesir", "Marmara", 1257),
        new(11, "Bilecik", "Marmara", 228),
        new(12, "Bingöl", "Doğu Anadolu", 283),
        new(13, "Bitlis", "Doğu Anadolu", 353),
        new(14, "Bolu", "Karadeniz", 320),
        new(15, "Burdur", "Akdeniz", 273),
        new(16, "Bursa", "Marmara", 3194),
        new(17, "Çanakkale", "Marmara", 559),
        new(18, "Çankırı", "İç Anadolu", 196),
        new(19, "Çorum", "Karadeniz", 524),
        new(20, "Denizli", "Ege", 1056),
        new(21, "Diyarbakır", "Güneydoğu Anadolu", 1804),
        new(22, "Edirne", "Marmara", 414),
        new(23, "Elazığ", "Doğu Anadolu", 591),
        new(24, "Erzincan", "Doğu Anadolu", 239),
        new(25, "Erzurum", "Doğu Anadolu", 749),
        new(26, "Eskişehir", "İç Anadolu", 906),
        new(27, "Gaziantep", "Güneydoğu Anadolu", 2164),
        new(28, "Giresun", "Karadeniz", 450),
        new(29, "Gümüşhane", "Karadeniz", 144),
        new(30, "Hakkari", "Doğu Anadolu", 278),
        new(31, "Hatay", "Akdeniz", 1670),
        new(32, "Isparta", "Akdeniz", 445),
        new(33, "Mersin", "Akdeniz", 1916),
        new(34, "İstanbul", "Marmara", 15655),
        new(35, "İzmir", "Ege", 4479),
        new(36, "Kars", "Doğu Anadolu", 281),
        new(37, "Kastamonu", "Karadeniz", 378),
        new(38, "Kayseri", "İç Anadolu", 1441),
        new(39, "Kırklareli", "Marmara", 369),
        new(40, "Kırşehir", "İç Anadolu", 244),
        new(41, "Kocaeli", "Marmara", 2102),
        new(42, "Konya", "İç Anadolu", 2296),
        new(43, "Kütahya", "Ege", 580),
        new(44, "Malatya", "Doğu Anadolu", 812),
        new(45, "Manisa", "Ege", 1468),
        new(46, "Kahramanmaraş", "Akdeniz", 1177),
        new(47, "Mardin", "Güneydoğu Anadolu", 870),
        new(48, "Muğla", "Ege", 1048),
        new(49, "Muş", "Doğu Anadolu", 405),
        new(50, "Nevşehir", "İç Anadolu", 310),
        new(51, "Niğde", "İç Anadolu", 365),
        new(52, "Ordu", "Karadeniz", 763),
        new(53, "Rize", "Karadeniz", 344),
        new(54, "Sakarya", "Marmara", 1080),
        new(55, "Samsun", "Karadeniz", 1371),
        new(56, "Siirt", "Güneydoğu Anadolu", 331),
        new(57, "Sinop", "Karadeniz", 218),
        new(58, "Sivas", "İç Anadolu", 634),
        new(59, "Tekirdağ", "Marmara", 1142),
        new(60, "Tokat", "Karadeniz", 602),
        new(61, "Trabzon", "Karadeniz", 816),
        new(62, "Tunceli", "Doğu Anadolu", 84),
        new(63, "Şanlıurfa", "Güneydoğu Anadolu", 2170),
        new(64, "Uşak", "Ege", 375),
        new(65, "Van", "Doğu Anadolu", 1127),
        new(66, "Yozgat", "İç Anadolu", 418),
        new(67, "Zonguldak", "Karadeniz", 588),
        new(68, "Aksaray", "İç Anadolu", 433),
        new(69, "Bayburt", "Karadeniz", 85),
        new(70, "Karaman", "İç Anadolu", 260),
        new(71, "Kırıkkale", "İç Anadolu", 275),
        new(72, "Batman", "Güneydoğu Anadolu", 647),
        new(73, "Şırnak", "Güneydoğu Anadolu", 557),
        new(74, "Bartın", "Karadeniz", 203),
        new(75, "Ardahan", "Doğu Anadolu", 95),
        new(76, "Iğdır", "Doğu Anadolu", 203),
        new(77, "Yalova", "Marmara", 296),
        new(78, "Karabük", "Karadeniz", 252),
        new(79, "Kilis", "Güneydoğu Anadolu", 147),
        new(80, "Osmaniye", "Akdeniz", 559),
        new(81, "Düzce", "Karadeniz", 405)
    ];
}
