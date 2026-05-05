using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Win11;

// Windows 11'de sağ tık menüsünü klasik (Win10) stiline döndürür.
public class ClassicContextMenuOptimization : IOptimization
{
    public string Id => "win11_classicmenu";
    public string Name => "Klasik Sağ Tık Menüsü";
    public string ShortDescription => "Windows 10 tarzı tam sağ tık menüsüne dön";
    public string WhatItDoes =>
        "Windows 11'in 'Daha fazla seçenek göster' adımını kaldırır. " +
        "Sağ tıkta tüm seçenekler direkt görünür.";
    public string Benefit =>
        "Daha hızlı ve pratik sağ tık menüsü. " +
        "Ek tıklama gerekmez.";
    public string Risk => "Windows 11'in yeni menü tasarımı kaybolur.";
    public bool IsReversible => true;
    public bool IsRecommended => true;
    public OptimizationCategory Category => OptimizationCategory.Windows11;
    public OptimizationRisk RiskLevel => OptimizationRisk.None;

    // Sadece Windows 11'de göster
    public bool IsApplicable(SystemInfo info) => info.IsWindows11;

    private const string KeyPath =
        @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";

    public async Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                progress.Report("Klasik sağ tık menüsü etkinleştiriliyor...");

                // Boş değer = klasik menü aktif
                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser,
                    KeyPath,
                    "",  // varsayılan değer
                    "",
                    RegistryValueKind.String);

                // Explorer'ı yeniden başlat
                progress.Report("Explorer yeniden başlatılıyor...");
                RestartExplorer();

                progress.Report("✅ Klasik sağ tık menüsü etkinleştirildi.");
                return new OptimizationResult(true,
                    "Klasik sağ tık menüsü etkinleştirildi.");
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
                progress.Report("Windows 11 sağ tık menüsü geri yükleniyor...");

                RegistryHelper.DeleteKey(
                    RegistryHive.CurrentUser,
                    KeyPath);

                RestartExplorer();

                progress.Report("✅ Windows 11 sağ tık menüsü geri yüklendi.");
                return new OptimizationResult(true,
                    "Windows 11 sağ tık menüsü geri yüklendi.");
            }
            catch (Exception ex)
            {
                return new OptimizationResult(false, $"Hata: {ex.Message}");
            }
        }, ct);
    }

    private static void RestartExplorer()
    {
        try
        {
            foreach (var p in System.Diagnostics.Process.GetProcessesByName("explorer"))
                p.Kill();
        }
        catch { }
    }
}