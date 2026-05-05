using System.IO;
using System.Text.Json;

namespace WinOptimizer;

// Tüm uygulama metinlerini yöneten dil sistemi.
// Yeni dil eklemek için sadece yeni bir Dictionary bloğu ekle.
public static class LanguageManager
{
    // Mevcut dil — başlangıçta Türkçe
    public static string CurrentLanguage { get; private set; } = "tr";

    // Dil değiştiğinde UI bu eventi dinler ve günceller
    public static event Action? LanguageChanged;

    // Ayar dosyası konumu — program kapanırken kaydedilir
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinOptimizer", "settings.json");

    // ── TÜRKÇE METİNLER ──────────────────────────────────────────────────────
    private static readonly Dictionary<string, string> Turkish = new()
    {
        // Üst bar
        ["app_title"] = "WinOptimizer",
        ["btn_rescan"] = "🔄 Yeniden Tara",
        ["btn_about"] = "ℹ️ Hakkında",

        // Sol menü
        ["lbl_system_status"] = "Sistem Durumu",
        ["lbl_categories"] = "KATEGORİLER",

        // Kategoriler
        ["cat_performance"] = "Performans",
        ["cat_display"] = "Ekran",
        ["cat_bloatware"] = "Bloatware",
        ["cat_startup"] = "Başlangıç",
        ["cat_privacy"] = "Gizlilik",
        ["cat_cleanup"] = "Disk",
        ["cat_network"] = "Ağ",
        ["cat_windows11"] = "Windows 11",

        // Butonlar
        ["btn_select_recommended"] = "⭐ Önerilenleri Seç",
        ["btn_toggle_all"] = "☑️ Tümünü Seç/Kaldır",
        ["btn_apply"] = "⚡ Seçilenleri Uygula",
        ["btn_restore"] = "↩️ Geri Yükle",
        ["btn_save_log"] = "📄 Logu Kaydet",

        // Durum metinleri
        ["status_ready"] = "Hazır",
        ["status_scanning"] = "Sistem taranıyor...",
        ["status_applying"] = "Uygulanıyor...",
        ["status_done"] = "Tamamlandı",
        ["status_scan_done"] = "Tarama tamamlandı ✓",

        // Kart etiketleri
        ["lbl_what_it_does"] = "Ne yapar:",
        ["lbl_benefit"] = "Faydası:",
        ["lbl_risk"] = "⚠️ Risk:",
        ["lbl_reversible"] = "Geri alınır:",
        ["lbl_yes"] = "Evet ✅",
        ["lbl_no"] = "Hayır ⚠️",
        ["lbl_recommended"] = "ÖNERİLEN",
        ["lbl_applied"] = "✅ UYGULANDII",

        // Sonuç paneli
        ["result_success"] = "✅ Başarılı",
        ["result_failed"] = "❌ Başarısız",
        ["result_restart"] = "⚠️ Değişiklikler için bilgisayarı yeniden başlatın.",

        // Hakkında
        ["about_title"] = "Hakkında",
        ["about_text"] =
            "WinOptimizer v1.0\n\n" +
            "Windows 10/11 için sistem optimizasyon aracı.\n\n" +
            "⚠️ Her işlem öncesi otomatik geri yükleme noktası oluşturulur.\n" +
            "Tüm değişiklikler geri alınabilir.",

        // Geri yükleme
        ["restore_title"] = "Geri Yükleme",
        ["restore_text"] =
            "Geri yükleme için:\n\n" +
            "1. Başlat → Sistem Geri Yükleme\n" +
            "2. 'WinOptimizer Backup' adlı noktayı seçin\n" +
            "3. Geri yükleyin",

        // Tarama mesajları
        ["scan_os"] = "İşletim sistemi taranıyor...",
        ["scan_cpu"] = "İşlemci taranıyor...",
        ["scan_ram"] = "RAM taranıyor...",
        ["scan_gpu"] = "GPU taranıyor...",
        ["scan_disk"] = "Diskler taranıyor...",
        ["scan_board"] = "Anakart taranıyor...",
        ["scan_network"] = "Ağ taranıyor...",
        ["scan_monitor"] = "Monitörler taranıyor...",
        ["scan_complete"] = "Tarama tamamlandı ✓",

        // Optimizasyon metinleri — Türkçe
        ["opt_powerplan_name"] = "Ultimate Performance Güç Planı",
        ["opt_powerplan_short"] = "Maksimum performans için güç planını değiştir",
        ["opt_powerplan_what"] = "Windows'un gizli 'Ultimate Performance' güç planını etkinleştirir.",
        ["opt_powerplan_benefit"] = "Masaüstü oyunlarda tutarlı yüksek performans.",
        ["opt_powerplan_risk"] = "Dizüstü bilgisayarlarda pil ömrü kısalır.",

        ["opt_visualfx_name"] = "Görsel Efektleri Kapat",
        ["opt_visualfx_short"] = "Animasyon ve şeffaflık efektlerini devre dışı bırak",
        ["opt_visualfx_what"] = "Animasyonları, şeffaflığı ve pencere gölgelerini kapatır.",
        ["opt_visualfx_benefit"] = "CPU/GPU kullanımı düşer, arayüz tepkileri hızlanır.",
        ["opt_visualfx_risk"] = "Arayüz görsel olarak daha sade görünür.",

        ["opt_gamemode_name"] = "Oyun Modu + HAGS",
        ["opt_gamemode_short"] = "Oyun modunu ve donanım hızlandırmalı GPU zamanlamasını aç",
        ["opt_gamemode_what"] = "Oyun Modu arka plan görevlerini azaltır. HAGS GPU zamanlamasını iyileştirir.",
        ["opt_gamemode_benefit"] = "FPS kararlılığı artar, taklamalar azalır.",
        ["opt_gamemode_risk"] = "HAGS için yeniden başlatma gerekir.",

        ["opt_telemetry_name"] = "Telemetriyi Kapat",
        ["opt_telemetry_short"] = "Microsoft'a gönderilen veri toplamayı devre dışı bırak",
        ["opt_telemetry_what"] = "Windows'un Microsoft'a gönderdiği kullanım ve tanılama verilerini kapatır.",
        ["opt_telemetry_benefit"] = "Arka plan ağ trafiği azalır, gizlilik artar.",
        ["opt_telemetry_risk"] = "Windows Update bazı özellikler için telemetri gerektirebilir.",

        ["opt_cortana_name"] = "Cortana'yı Devre Dışı Bırak",
        ["opt_cortana_short"] = "Cortana ve arama telemetrisini kapat",
        ["opt_cortana_what"] = "Cortana'nın çalışmasını ve Microsoft'a arama verisi göndermesini engeller.",
        ["opt_cortana_benefit"] = "RAM kullanımı düşer, gizlilik artar.",
        ["opt_cortana_risk"] = "Cortana sesli asistan özelliği kullanılamaz.",

        ["opt_nagle_name"] = "Nagle Algoritmasını Kapat",
        ["opt_nagle_short"] = "Online oyunlarda ping ve gecikmeyi azalt",
        ["opt_nagle_what"] = "TCP paketlerini birleştiren Nagle algoritmasını kapatır.",
        ["opt_nagle_benefit"] = "Online oyunlarda ping kararlılığı artar.",
        ["opt_nagle_risk"] = "Yüksek hacimli transferlerde çok küçük verimlilik kaybı.",

        ["opt_tempfiles_name"] = "Geçici Dosyaları Temizle",
        ["opt_tempfiles_short"] = "Temp klasörleri, WU cache ve DNS cache temizle",
        ["opt_tempfiles_what"] = "%TEMP%, Windows\\Temp, Windows Update cache ve DNS cache temizler.",
        ["opt_tempfiles_benefit"] = "Disk alanı açılır, bozuk DNS kayıtları temizlenir.",
        ["opt_tempfiles_risk"] = "Yok — geçici dosyalar silinmek için tasarlanmıştır.",

        ["opt_classicmenu_name"] = "Klasik Sağ Tık Menüsü",
        ["opt_classicmenu_short"] = "Windows 10 tarzı tam sağ tık menüsüne dön",
        ["opt_classicmenu_what"] = "Windows 11'in 'Daha fazla seçenek göster' adımını kaldırır.",
        ["opt_classicmenu_benefit"] = "Daha hızlı ve pratik sağ tık menüsü.",
        ["opt_classicmenu_risk"] = "Windows 11'in yeni menü tasarımı kaybolur.",

        ["opt_taskbaralign_name"] = "Görev Çubuğunu Sola Hizala",
        ["opt_taskbaralign_short"] = "Başlat butonunu ve ikonları sola taşı",
        ["opt_taskbaralign_what"] = "Görev çubuğu ikonlarını Windows 10 gibi sola hizalar.",
        ["opt_taskbaralign_benefit"] = "Windows 10 alışkanlığı olanlar için tanıdık arayüz.",
        ["opt_taskbaralign_risk"] = "Yok — sadece görsel bir değişiklik.",

        ["opt_appx_name"] = "Microsoft Bloatware Kaldır",
        ["opt_appx_short"] = "Gereksiz önceden yüklenmiş uygulamaları sil",
        ["opt_appx_what"] = "Hava Durumu, Haberler, Solitaire, Groove Music gibi uygulamaları kaldırır.",
        ["opt_appx_benefit"] = "Disk alanı açılır, RAM kullanımı ve başlangıç süresi azalır.",
        ["opt_appx_risk"] = "Kaldırılan uygulamalar Microsoft Store'dan tekrar indirilebilir.",

        ["opt_onedrive_name"] = "OneDrive'ı Tamamen Kaldır 🔥",
        ["opt_onedrive_short"] = "OneDrive'ı sistemden kökünden sil",
        ["opt_onedrive_what"] = "OneDrive'ı kapatır, kaldırır, klasörlerini ve registry girdilerini siler.",
        ["opt_onedrive_benefit"] = "Disk alanı açılır, arka plan senkronizasyonu durur.",
        ["opt_onedrive_risk"] = "GERİ ALINAMAZ! Senkronize edilmemiş dosyalar kaybolabilir!",

        ["opt_startup_name"] = "Başlangıç Programlarını Yönet",
        ["opt_startup_short"] = "Windows açılışında çalışan programları devre dışı bırak",
        ["opt_startup_what"] = "Registry ve Startup klasöründeki başlangıç girdilerini devre dışı bırakır.",
        ["opt_startup_benefit"] = "Windows açılış süresi kısalır, RAM kullanımı azalır.",
        ["opt_startup_risk"] = "Sistem kritik programlar otomatik korunur.",

        ["opt_refreshrate_name"] = "Yenileme Hızını Maksimuma Ayarla",
        ["opt_refreshrate_short"] = "Tüm monitörleri en yüksek Hz değerine ayarla",
        ["opt_refreshrate_what"] = "Bağlı tüm monitörlerin desteklediği en yüksek yenileme hızını aktif eder.",
        ["opt_refreshrate_benefit"] = "Daha akıcı görüntü, göz yorgunluğu azalır.",
        ["opt_refreshrate_risk"] = "Desteklenmeyen değer seçilirse ekran geçici kararabilir.",

        // Hakkında ve geri yükleme
        ["about_title"] = "Hakkında",
        ["about_text"] = "WinOptimizer v1.0\n\nWindows 10/11 için sistem optimizasyon aracı.\n\n⚠️ Her işlem öncesi otomatik geri yükleme noktası oluşturulur.\nTüm değişiklikler geri alınabilir.",
        ["restore_title"] = "Geri Yükleme",
        ["restore_text"] = "Geri yükleme için:\n\n1. Başlat → Sistem Geri Yükleme\n2. 'WinOptimizer Backup' adlı noktayı seçin\n3. Geri yükleyin",

        // Boş liste
        ["empty_category"] = "Bu kategori için henüz optimizasyon yok.",
    };

    // ── İNGİLİZCE METİNLER ───────────────────────────────────────────────────
    private static readonly Dictionary<string, string> English = new()
    {
        ["app_title"] = "WinOptimizer",
        ["btn_rescan"] = "🔄 Rescan",
        ["btn_about"] = "ℹ️ About",

        ["lbl_system_status"] = "System Status",
        ["lbl_categories"] = "CATEGORIES",

        ["cat_performance"] = "Performance",
        ["cat_display"] = "Display",
        ["cat_bloatware"] = "Bloatware",
        ["cat_startup"] = "Startup",
        ["cat_privacy"] = "Privacy",
        ["cat_cleanup"] = "Disk",
        ["cat_network"] = "Network",
        ["cat_windows11"] = "Windows 11",

        ["btn_select_recommended"] = "⭐ Select Recommended",
        ["btn_toggle_all"] = "☑️ Select/Deselect All",
        ["btn_apply"] = "⚡ Apply Selected",
        ["btn_restore"] = "↩️ Restore",
        ["btn_save_log"] = "📄 Save Log",

        ["status_ready"] = "Ready",
        ["status_scanning"] = "Scanning system...",
        ["status_applying"] = "Applying...",
        ["status_done"] = "Done",
        ["status_scan_done"] = "Scan complete ✓",

        ["lbl_what_it_does"] = "What it does:",
        ["lbl_benefit"] = "Benefit:",
        ["lbl_risk"] = "⚠️ Risk:",
        ["lbl_reversible"] = "Reversible:",
        ["lbl_yes"] = "Yes ✅",
        ["lbl_no"] = "No ⚠️",
        ["lbl_recommended"] = "RECOMMENDED",
        ["lbl_applied"] = "✅ APPLIED",

        ["result_success"] = "✅ Success",
        ["result_failed"] = "❌ Failed",
        ["result_restart"] = "⚠️ Restart your computer for changes to take effect.",

        ["about_title"] = "About",
        ["about_text"] =
            "WinOptimizer v1.0\n\n" +
            "System optimization tool for Windows 10/11.\n\n" +
            "⚠️ A restore point is automatically created before each operation.\n" +
            "All changes are reversible.",

        ["restore_title"] = "Restore",
        ["restore_text"] =
            "To restore:\n\n" +
            "1. Start → System Restore\n" +
            "2. Select 'WinOptimizer Backup'\n" +
            "3. Restore",

        ["scan_os"] = "Scanning operating system...",
        ["scan_cpu"] = "Scanning CPU...",
        ["scan_ram"] = "Scanning RAM...",
        ["scan_gpu"] = "Scanning GPU...",
        ["scan_disk"] = "Scanning disks...",
        ["scan_board"] = "Scanning motherboard...",
        ["scan_network"] = "Scanning network...",
        ["scan_monitor"] = "Scanning monitors...",
        ["scan_complete"] = "Scan complete ✓",

        // Optimizasyon metinleri — English
        ["opt_powerplan_name"] = "Ultimate Performance Power Plan",
        ["opt_powerplan_short"] = "Switch to maximum performance power plan",
        ["opt_powerplan_what"] = "Activates Windows hidden 'Ultimate Performance' power plan.",
        ["opt_powerplan_benefit"] = "Consistent high performance in desktop games.",
        ["opt_powerplan_risk"] = "Battery life decreases on laptops.",

        ["opt_visualfx_name"] = "Disable Visual Effects",
        ["opt_visualfx_short"] = "Turn off animations and transparency effects",
        ["opt_visualfx_what"] = "Disables animations, transparency and window shadows.",
        ["opt_visualfx_benefit"] = "Lower CPU/GPU usage, faster UI response.",
        ["opt_visualfx_risk"] = "UI looks more plain without animations.",

        ["opt_gamemode_name"] = "Game Mode + HAGS",
        ["opt_gamemode_short"] = "Enable game mode and hardware accelerated GPU scheduling",
        ["opt_gamemode_what"] = "Game Mode reduces background tasks. HAGS improves GPU scheduling.",
        ["opt_gamemode_benefit"] = "Better FPS stability, fewer stutters.",
        ["opt_gamemode_risk"] = "Restart required for HAGS.",

        ["opt_telemetry_name"] = "Disable Telemetry",
        ["opt_telemetry_short"] = "Turn off Microsoft data collection",
        ["opt_telemetry_what"] = "Disables usage and diagnostic data sent to Microsoft.",
        ["opt_telemetry_benefit"] = "Less background network traffic, better privacy.",
        ["opt_telemetry_risk"] = "Windows Update may need telemetry for some features.",

        ["opt_cortana_name"] = "Disable Cortana",
        ["opt_cortana_short"] = "Turn off Cortana and search telemetry",
        ["opt_cortana_what"] = "Prevents Cortana from running and sending search data.",
        ["opt_cortana_benefit"] = "Lower RAM usage, better privacy.",
        ["opt_cortana_risk"] = "Cortana voice assistant becomes unavailable.",

        ["opt_nagle_name"] = "Disable Nagle Algorithm",
        ["opt_nagle_short"] = "Reduce ping and latency in online games",
        ["opt_nagle_what"] = "Disables the Nagle algorithm that buffers TCP packets.",
        ["opt_nagle_benefit"] = "Better ping stability in online games.",
        ["opt_nagle_risk"] = "Very minor efficiency loss in bulk transfers.",

        ["opt_tempfiles_name"] = "Clean Temporary Files",
        ["opt_tempfiles_short"] = "Clean temp folders, WU cache and DNS cache",
        ["opt_tempfiles_what"] = "Cleans %TEMP%, Windows\\Temp, Windows Update cache and DNS cache.",
        ["opt_tempfiles_benefit"] = "Frees disk space, clears corrupt DNS records.",
        ["opt_tempfiles_risk"] = "None — temp files are designed to be deleted.",

        ["opt_classicmenu_name"] = "Classic Right-Click Menu",
        ["opt_classicmenu_short"] = "Restore Windows 10 style full context menu",
        ["opt_classicmenu_what"] = "Removes the 'Show more options' step in Windows 11.",
        ["opt_classicmenu_benefit"] = "Faster and more practical right-click menu.",
        ["opt_classicmenu_risk"] = "Windows 11 new menu design is lost.",

        ["opt_taskbaralign_name"] = "Align Taskbar to Left",
        ["opt_taskbaralign_short"] = "Move Start button and icons to the left",
        ["opt_taskbaralign_what"] = "Aligns taskbar icons to the left like Windows 10.",
        ["opt_taskbaralign_benefit"] = "More familiar interface for Windows 10 users.",
        ["opt_taskbaralign_risk"] = "None — visual change only.",

        ["opt_appx_name"] = "Remove Microsoft Bloatware",
        ["opt_appx_short"] = "Remove unnecessary pre-installed apps",
        ["opt_appx_what"] = "Removes Weather, News, Solitaire, Groove Music and more.",
        ["opt_appx_benefit"] = "Frees disk, reduces RAM and startup time.",
        ["opt_appx_risk"] = "Removed apps can be reinstalled from Microsoft Store.",

        ["opt_onedrive_name"] = "Remove OneDrive Completely 🔥",
        ["opt_onedrive_short"] = "Completely remove OneDrive from system",
        ["opt_onedrive_what"] = "Kills, uninstalls OneDrive and removes all its folders and registry entries.",
        ["opt_onedrive_benefit"] = "Frees disk space, stops background sync.",
        ["opt_onedrive_risk"] = "IRREVERSIBLE! Unsynced files may be lost!",

        ["opt_startup_name"] = "Manage Startup Programs",
        ["opt_startup_short"] = "Disable programs that run at Windows startup",
        ["opt_startup_what"] = "Disables startup entries from Registry and Startup folder.",
        ["opt_startup_benefit"] = "Faster Windows boot, lower RAM usage.",
        ["opt_startup_risk"] = "Critical system programs are automatically protected.",

        ["opt_refreshrate_name"] = "Set Maximum Refresh Rate",
        ["opt_refreshrate_short"] = "Set all monitors to highest supported Hz",
        ["opt_refreshrate_what"] = "Automatically activates the highest refresh rate supported by each monitor.",
        ["opt_refreshrate_benefit"] = "Smoother image, less eye strain.",
        ["opt_refreshrate_risk"] = "Screen may briefly go black if unsupported rate is selected.",

        ["about_title"] = "About",
        ["about_text"] = "WinOptimizer v1.0\n\nSystem optimization tool for Windows 10/11.\n\n⚠️ A restore point is automatically created before each operation.\nAll changes are reversible.",
        ["restore_title"] = "Restore",
        ["restore_text"] = "To restore:\n\n1. Start → System Restore\n2. Select 'WinOptimizer Backup'\n3. Restore",

        ["empty_category"] = "No optimizations available for this category.",
    };

    // ── ANA METOD ─────────────────────────────────────────────────────────────

    // Metin anahtarından çeviriyi döndür
    public static string Get(string key)
    {
        var dict = CurrentLanguage == "tr" ? Turkish : English;
        return dict.TryGetValue(key, out string? val) ? val : key;
    }

    // Dili değiştir ve UI'ı güncelle
    public static void SetLanguage(string lang)
    {
        if (CurrentLanguage == lang) return;
        CurrentLanguage = lang;
        SaveSettings();
        LanguageChanged?.Invoke();
    }

    // ── AYAR KAYDETME ─────────────────────────────────────────────────────────

    public static void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            string json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (settings != null && settings.TryGetValue("language", out string? lang))
                CurrentLanguage = lang;
        }
        catch { }
    }

    private static void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var settings = new Dictionary<string, string> { ["language"] = CurrentLanguage };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings));
        }
        catch { }
    }
}