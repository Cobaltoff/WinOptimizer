using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Bloatware;

// OneDrive'ı sistemden tamamen kaldırır.
// GERİ ALINAMAZ işlem — kullanıcıya çift onay gösterilmeli.
public class OneDriveRemover : IOptimization
{
    public string Id => "bloatware_onedrive";
    public string Name => "OneDrive'ı Tamamen Kaldır 🔥";
    public string ShortDescription => "OneDrive'ı sistemden kökünden sil";
    public string WhatItDoes =>
        "OneDrive'ı kapatır, kaldırır, klasörlerini siler, " +
        "Explorer'daki ikonunu gizler ve yeniden kurulmasını engeller.";
    public string Benefit =>
        "Disk alanı açılır, arka plan senkronizasyonu durur, " +
        "gizlilik artar.";
    public string Risk =>
        "GERİ ALINAMAZ! OneDrive'daki senkronize edilmemiş " +
        "dosyalar kaybolabilir. İşlem öncesi yedek alın!";
    public bool IsReversible => false;
    public bool IsRecommended => false;
    public OptimizationCategory Category => OptimizationCategory.Bloatware;
    public OptimizationRisk RiskLevel => OptimizationRisk.High;
    public bool IsApplicable(SystemInfo info) => true;

    public async Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        try
        {
            // 1. OneDrive process'lerini sonlandır
            progress.Report("OneDrive kapatılıyor...");
            await PowerShellRunner.RunExeAsync(
                "taskkill", "/f /im OneDrive.exe", progress);

            await Task.Delay(1000, ct);

            // 2. Kaldırıcıyı çalıştır
            progress.Report("OneDrive kaldırılıyor...");
            string uninstaller64 = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "SysWOW64", "OneDriveSetup.exe");

            string uninstallerLocal = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "OneDrive", "OneDriveSetup.exe");

            if (File.Exists(uninstaller64))
                await PowerShellRunner.RunExeAsync(
                    uninstaller64, "/uninstall", progress);
            else if (File.Exists(uninstallerLocal))
                await PowerShellRunner.RunExeAsync(
                    uninstallerLocal, "/uninstall", progress);

            await Task.Delay(2000, ct);

            // 3. Kalıntı klasörlerini sil
            progress.Report("Kalıntı klasörler siliniyor...");
            DeleteFolderSafely(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "OneDrive"), progress);

            DeleteFolderSafely(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "OneDrive"), progress);

            DeleteFolderSafely(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Microsoft OneDrive"), progress);

            DeleteFolderSafely(@"C:\OneDriveTemp", progress);

            // 4. Explorer'dan OneDrive ikonunu kaldır
            progress.Report("Explorer ikonu kaldırılıyor...");
            const string clsid = "{018D5C66-4533-4307-9B53-224DE2ED1FE6}";

            RegistryHelper.SetValue(
                RegistryHive.ClassesRoot,
                $@"CLSID\{clsid}\System.IsPinnedToNameSpaceTree",
                "", 0);

            RegistryHelper.SetValue(
                RegistryHive.ClassesRoot,
                $@"Wow6432Node\CLSID\{clsid}\System.IsPinnedToNameSpaceTree",
                "", 0);

            // 5. Group Policy ile yeniden kurulmasını engelle
            progress.Report("Yeniden kurulum engelleniyor...");
            RegistryHelper.SetValue(
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\OneDrive",
                "DisableFileSyncNGSC", 1);

            // 6. Scheduled task'ları kaldır
            progress.Report("Zamanlanmış görevler kaldırılıyor...");
            await PowerShellRunner.RunAsync(
                "Get-ScheduledTask -TaskPath '\\' | " +
                "Where-Object { $_.TaskName -like '*OneDrive*' } | " +
                "Unregister-ScheduledTask -Confirm:$false -ErrorAction SilentlyContinue",
                progress, ct);

            progress.Report("✅ OneDrive tamamen kaldırıldı.");
            return new OptimizationResult(true,
                "OneDrive sistemden tamamen kaldırıldı.",
                RequiresRestart: true);
        }
        catch (Exception ex)
        {
            return new OptimizationResult(false, $"Hata: {ex.Message}");
        }
    }

    public Task<OptimizationResult> RevertAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        progress.Report("OneDrive kaldırma işlemi geri alınamaz.");
        return Task.FromResult(new OptimizationResult(
            false,
            "OneDrive kaldırma işlemi geri alınamaz. " +
            "Microsoft sitesinden manuel kurulum yapabilirsiniz."));
    }

    private static void DeleteFolderSafely(string path, IProgress<string> progress)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                progress.Report($"  Silindi: {path}");
            }
        }
        catch (Exception ex)
        {
            progress.Report($"  Atlandı: {path} ({ex.Message})");
        }
    }
}