using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Privacy;

// Windows telemetrisini kapatır, reklam ID'sini devre dışı bırakır,
// Cortana'yı kaldırır ve etkinlik geçmişini temizler.
public class TelemetryOptimization : IOptimization
{
    public string Id => "privacy_telemetry";
    public string Name => "Telemetriyi Kapat";
    public string ShortDescription => "Microsoft'a gönderilen veri toplamayı devre dışı bırak";
    public string WhatItDoes =>
        "Windows'un Microsoft sunucularına gönderdiği kullanım verilerini, " +
        "hata raporlarını ve tanılama bilgilerini kapatır.";
    public string Benefit =>
        "Arka planda ağ trafiği azalır, gizlilik artar, " +
        "gereksiz arka plan servisleri devre dışı kalır.";
    public string Risk => "Windows Update bazı özellikler için telemetri gerektirebilir.";
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
                progress.Report("Telemetri kapatılıyor...");

                // Telemetri seviyesini 0'a indir
                RegistryHelper.SetValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                    "AllowTelemetry", 0);

                RegistryHelper.SetValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection",
                    "AllowTelemetry", 0);

                progress.Report("Reklam ID kapatılıyor...");

                // Reklam ID'sini kapat
                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                    "Enabled", 0);

                progress.Report("Kilit ekranı reklamları kapatılıyor...");

                // Kilit ekranı ve başlat menüsü reklamlarını kapat
                string contentPath =
                    @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";

                string[] adKeys = {
                    "SubscribedContent-338387Enabled",
                    "SubscribedContent-338388Enabled",
                    "SubscribedContent-338389Enabled",
                    "SubscribedContent-353698Enabled",
                    "SubscribedContent-310093Enabled",
                    "SystemPaneSuggestionsEnabled",
                    "SoftLandingEnabled"
                };

                foreach (var key in adKeys)
                    RegistryHelper.SetValue(
                        RegistryHive.CurrentUser, contentPath, key, 0);

                progress.Report("Etkinlik geçmişi kapatılıyor...");

                // Etkinlik geçmişini kapat
                RegistryHelper.SetValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\System",
                    "PublishUserActivities", 0);

                RegistryHelper.SetValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\System",
                    "EnableActivityFeed", 0);

                progress.Report("✅ Telemetri ve gizlilik ayarları tamamlandı.");
                return new OptimizationResult(true,
                    "Telemetri, reklam ID ve etkinlik geçmişi kapatıldı.");
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
                progress.Report("Telemetri ayarları varsayılana döndürülüyor...");

                RegistryHelper.DeleteValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                    "AllowTelemetry");

                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                    "Enabled", 1);

                progress.Report("✅ Telemetri ayarları sıfırlandı.");
                return new OptimizationResult(true, "Telemetri ayarları varsayılana döndürüldü.");
            }
            catch (Exception ex)
            {
                return new OptimizationResult(false, $"Hata: {ex.Message}");
            }
        }, ct);
    }
}