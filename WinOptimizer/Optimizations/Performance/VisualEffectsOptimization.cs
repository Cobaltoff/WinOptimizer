using Microsoft.Win32;
using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Performance;

// Windows görsel efektlerini "En İyi Performans" moduna alır.
// Animasyonları, şeffaflığı ve gölgeleri kapatır.
public class VisualEffectsOptimization : IOptimization
{
    public string Id => "perf_visualeffects";
    public string Name => "Görsel Efektleri Kapat";
    public string ShortDescription => "Animasyon ve şeffaflık efektlerini devre dışı bırak";
    public string WhatItDoes =>
        "Windows'un görsel animasyonlarını, şeffaflık efektlerini ve " +
        "pencere gölgelerini kapatır. Arayüz sade görünür.";
    public string Benefit =>
        "CPU ve GPU kullanımını düşürür, arayüz tepkileri hızlanır. " +
        "Özellikle düşük RAM'li sistemlerde belirgin fark yaratır.";
    public string Risk => "Arayüz görsel olarak daha sade görünür, animasyonlar kaybolur.";
    public bool IsReversible => true;
    public bool IsRecommended => true;
    public OptimizationCategory Category => OptimizationCategory.Performance;
    public OptimizationRisk RiskLevel => OptimizationRisk.None;
    public bool IsApplicable(SystemInfo info) => true;

    // Değiştirilecek registry ayarları
    private const string VisualFxPath =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
    private const string PersonalizePath =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string DwmPath =
        @"Software\Microsoft\Windows\DWM";

    public async Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                progress.Report("Görsel efektler kapatılıyor...");

                // Tüm görsel efektleri kapat (2 = En İyi Performans)
                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser, VisualFxPath,
                    "VisualFXSetting", 2);

                progress.Report("Şeffaflık efekti kapatılıyor...");

                // Şeffaflığı kapat
                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser, PersonalizePath,
                    "EnableTransparency", 0);

                // Aero Peek kapat
                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser, DwmPath,
                    "EnableAeroPeek", 0);

                // Animasyonları kapat
                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser,
                    @"Control Panel\Desktop\WindowMetrics",
                    "MinAnimate", "0",
                    RegistryValueKind.String);

                progress.Report("✅ Görsel efektler kapatıldı.");
                return new OptimizationResult(true,
                    "Görsel efektler kapatıldı. Oturum kapatıp açınca tam etki görülür.");
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
                progress.Report("Görsel efektler varsayılana döndürülüyor...");

                // 0 = Windows'un kendi seçimi
                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser, VisualFxPath,
                    "VisualFXSetting", 0);

                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser, PersonalizePath,
                    "EnableTransparency", 1);

                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser, DwmPath,
                    "EnableAeroPeek", 1);

                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser,
                    @"Control Panel\Desktop\WindowMetrics",
                    "MinAnimate", "1",
                    RegistryValueKind.String);

                progress.Report("✅ Görsel efektler varsayılana döndürüldü.");
                return new OptimizationResult(true, "Görsel efektler sıfırlandı.");
            }
            catch (Exception ex)
            {
                return new OptimizationResult(false, $"Hata: {ex.Message}");
            }
        }, ct);
    }
}