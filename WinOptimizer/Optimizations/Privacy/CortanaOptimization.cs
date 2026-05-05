using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Privacy;

// Cortana'yı ve Windows Search telemetrisini devre dışı bırakır.
public class CortanaOptimization : IOptimization
{
    public string Id => "privacy_cortana";
    public string Name => "Cortana'yı Devre Dışı Bırak";
    public string ShortDescription => "Cortana ve arama telemetrisini kapat";
    public string WhatItDoes =>
        "Cortana'nın çalışmasını ve Microsoft'a arama verisi " +
        "göndermesini engeller.";
    public string Benefit =>
        "RAM kullanımı düşer, gizlilik artar, " +
        "arka plan ağ trafiği azalır.";
    public string Risk => "Cortana sesli asistan özelliği kullanılamaz hale gelir.";
    public bool IsReversible => true;
    public bool IsRecommended => true;
    public OptimizationCategory Category => OptimizationCategory.Privacy;
    public OptimizationRisk RiskLevel => OptimizationRisk.Low;
    public bool IsApplicable(SystemInfo info) => true;

    public async Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                progress.Report("Cortana devre dışı bırakılıyor...");

                // Cortana'yı kapat
                RegistryHelper.SetValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                    "AllowCortana", 0);

                // Cortana'nın kilit ekranında çalışmasını engelle
                RegistryHelper.SetValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                    "AllowCortanaAboveLock", 0);

                // Bing aramasını kapat
                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Search",
                    "BingSearchEnabled", 0);

                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Search",
                    "CortanaConsent", 0);

                progress.Report("✅ Cortana devre dışı bırakıldı.");
                return new OptimizationResult(true,
                    "Cortana devre dışı bırakıldı.");
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
                progress.Report("Cortana ayarları varsayılana döndürülüyor...");

                RegistryHelper.DeleteValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                    "AllowCortana");

                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Search",
                    "BingSearchEnabled", 1);

                progress.Report("✅ Cortana ayarları sıfırlandı.");
                return new OptimizationResult(true, "Cortana ayarları sıfırlandı.");
            }
            catch (Exception ex)
            {
                return new OptimizationResult(false, $"Hata: {ex.Message}");
            }
        }, ct);
    }
}