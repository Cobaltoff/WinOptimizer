using Microsoft.Win32;
using System.Diagnostics;
using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Startup;

// Başlangıç programlarını listeler ve devre dışı bırakır.
public class StartupManagerOptimization : IOptimization
{
    public string Id => "startup_manager";
    public string Name => "Başlangıç Programlarını Yönet";
    public string ShortDescription => "Windows açılışında çalışan programları devre dışı bırak";
    public string WhatItDoes =>
        "Registry ve Startup klasöründeki tüm başlangıç " +
        "girdilerini tarar ve gereksiz olanları devre dışı bırakır.";
    public string Benefit =>
        "Windows açılış süresi belirgin şekilde kısalır, " +
        "arka plan RAM kullanımı azalır.";
    public string Risk =>
        "Sistem kritik programlar otomatik korunur. " +
        "Yanlışlıkla kapatılan program tekrar açılabilir.";
    public bool IsReversible => true;
    public bool IsRecommended => true;
    public OptimizationCategory Category => OptimizationCategory.Startup;
    public OptimizationRisk RiskLevel => OptimizationRisk.Low;
    public bool IsApplicable(SystemInfo info) => true;

    // Korunacak sistem kritik girdiler
    private static readonly string[] ProtectedEntries =
    {
        "SecurityHealth",
        "WindowsDefender",
        "MsMpEng",
        "NvBackend",
        "IAStorIcon",
        "RtkAudUService",
    };

    public async Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                progress.Report("Başlangıç programları taranıyor...");
                var entries = GetStartupEntries();

                int disabled = 0;
                int skipped = 0;

                foreach (var entry in entries)
                {
                    ct.ThrowIfCancellationRequested();

                    // Sistem kritik girdiyi atla
                    if (IsProtected(entry.Name))
                    {
                        progress.Report($"  Korunuyor (atlandı): {entry.Name}");
                        skipped++;
                        continue;
                    }

                    progress.Report($"  Devre dışı bırakılıyor: {entry.Name}");

                    bool ok = DisableStartupEntry(entry);
                    if (ok) disabled++;
                }

                progress.Report(
                    $"✅ {disabled} program devre dışı, {skipped} sistem programı korundu.");

                return new OptimizationResult(true,
                    $"{disabled} başlangıç programı devre dışı bırakıldı.");
            }
            catch (Exception ex)
            {
                return new OptimizationResult(false, $"Hata: {ex.Message}");
            }
        }, ct);
    }

    public async Task<OptimizationResult> RevertAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                progress.Report("Başlangıç programları yeniden etkinleştiriliyor...");
                EnableAllStartupEntries(progress);
                progress.Report("✅ Başlangıç programları etkinleştirildi.");
                return new OptimizationResult(true,
                    "Başlangıç programları varsayılana döndürüldü.");
            }
            catch (Exception ex)
            {
                return new OptimizationResult(false, $"Hata: {ex.Message}");
            }
        }, ct);
    }

    // ── YARDIMCI METODLAR ────────────────────────────────────────────────────

    public static List<StartupEntry> GetStartupEntries()
    {
        var entries = new List<StartupEntry>();

        // HKLM Run
        ReadRegistryStartup(
            RegistryHive.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            entries, StartupEntrySource.RegistryHklm);

        // HKCU Run
        ReadRegistryStartup(
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            entries, StartupEntrySource.RegistryHkcu);

        // Startup klasörü
        ReadStartupFolder(entries);

        return entries;
    }

    private static void ReadRegistryStartup(
        RegistryHive hive,
        string path,
        List<StartupEntry> entries,
        StartupEntrySource source)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(path, writable: false);
            if (key == null) return;

            foreach (var name in key.GetValueNames())
            {
                string value = key.GetValue(name)?.ToString() ?? "";
                entries.Add(new StartupEntry
                {
                    Name = name,
                    Command = value,
                    Source = source,
                    RegistryPath = path,
                    Hive = hive,
                    IsEnabled = true
                });
            }
        }
        catch { }
    }

    private static void ReadStartupFolder(List<StartupEntry> entries)
    {
        try
        {
            string userStartup = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup));

            string commonStartup = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup));

            foreach (var folder in new[] { userStartup, commonStartup })
            {
                if (!Directory.Exists(folder)) continue;

                foreach (var file in Directory.GetFiles(folder, "*.lnk"))
                {
                    entries.Add(new StartupEntry
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Command = file,
                        Source = StartupEntrySource.StartupFolder,
                        IsEnabled = true
                    });
                }
            }
        }
        catch { }
    }

    private static bool DisableStartupEntry(StartupEntry entry)
    {
        try
        {
            if (entry.Source == StartupEntrySource.StartupFolder)
            {
                // Startup klasöründeki kısayolu yeniden adlandır (.disabled ekle)
                if (File.Exists(entry.Command))
                {
                    File.Move(entry.Command, entry.Command + ".disabled");
                    return true;
                }
                return false;
            }

            // Registry girdisini sil (yedek al)
            using var baseKey = RegistryKey.OpenBaseKey(
                entry.Hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(entry.RegistryPath, writable: true);
            if (key == null) return false;

            // Yedek: disabled anahtarına taşı
            string disabledPath = entry.RegistryPath.Replace(
                "\\Run", "\\Run_Disabled_WinOptimizer");

            RegistryHelper.SetValue(
                entry.Hive, disabledPath,
                entry.Name, entry.Command,
                RegistryValueKind.String);

            key.DeleteValue(entry.Name, throwOnMissingValue: false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void EnableAllStartupEntries(IProgress<string> progress)
    {
        try
        {
            // HKLM yedekten geri yükle
            RestoreFromBackup(
                RegistryHive.LocalMachine,
                @"Software\Microsoft\Windows\CurrentVersion\Run_Disabled_WinOptimizer",
                @"Software\Microsoft\Windows\CurrentVersion\Run",
                progress);

            // HKCU yedekten geri yükle
            RestoreFromBackup(
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Run_Disabled_WinOptimizer",
                @"Software\Microsoft\Windows\CurrentVersion\Run",
                progress);

            // .disabled dosyalarını geri yükle
            string userStartup = Environment.GetFolderPath(
                Environment.SpecialFolder.Startup);

            foreach (var file in Directory.GetFiles(userStartup, "*.disabled"))
            {
                string original = file.Replace(".disabled", "");
                File.Move(file, original);
                progress.Report($"  Geri yüklendi: {Path.GetFileName(original)}");
            }
        }
        catch { }
    }

    private static void RestoreFromBackup(
        RegistryHive hive,
        string backupPath,
        string targetPath,
        IProgress<string> progress)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var backupKey = baseKey.OpenSubKey(backupPath, writable: true);
            if (backupKey == null) return;

            foreach (var name in backupKey.GetValueNames())
            {
                string value = backupKey.GetValue(name)?.ToString() ?? "";
                RegistryHelper.SetValue(
                    hive, targetPath, name, value, RegistryValueKind.String);
                progress.Report($"  Geri yüklendi: {name}");
            }

            // Yedek anahtarı sil
            baseKey.DeleteSubKeyTree(backupPath, throwOnMissingSubKey: false);
        }
        catch { }
    }

    private static bool IsProtected(string name) =>
        ProtectedEntries.Any(p =>
            name.Contains(p, StringComparison.OrdinalIgnoreCase));
}

// ── YARDIMCI SINIFLAR ────────────────────────────────────────────────────────

public class StartupEntry
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public StartupEntrySource Source { get; set; }
    public string RegistryPath { get; set; } = "";
    public RegistryHive Hive { get; set; }
    public bool IsEnabled { get; set; }
}

public enum StartupEntrySource
{
    RegistryHklm,
    RegistryHkcu,
    StartupFolder
}