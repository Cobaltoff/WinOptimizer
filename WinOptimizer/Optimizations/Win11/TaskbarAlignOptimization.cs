using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Win11;

// Görev çubuğunu sola hizalar (Windows 10 gibi).
public class TaskbarAlignOptimization : IOptimization
{
    public string Id => "win11_taskbaralign";
    public string Name => "Görev Çubuğunu Sola Hizala";
    public string ShortDescription => "Başlat butonunu ve ikonları sola taşı";
    public string WhatItDoes =>
        "Windows 11'de varsayılan olarak ortada olan görev çubuğu " +
        "ikonlarını Windows 10 gibi sola hizalar.";
    public string Benefit => "Windows 10 alışkanlığı olanlar için daha tanıdık arayüz.";
    public string Risk => "Yok — sadece görsel bir değişiklik.";
    public bool IsReversible => true;
    public bool IsRecommended => false;
    public OptimizationCategory Category => OptimizationCategory.Windows11;
    public OptimizationRisk RiskLevel => OptimizationRisk.None;
    public bool IsApplicable(SystemInfo info) => info.IsWindows11;

    private const string KeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

    public async Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                progress.Report("Görev çubuğu sola hizalanıyor...");

                // 0 = sol, 1 = orta (varsayılan)
                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser,
                    KeyPath,
                    "TaskbarAl", 0);

                progress.Report("✅ Görev çubuğu sola hizalandı.");
                return new OptimizationResult(true,
                    "Görev çubuğu sola hizalandı. Hemen etki eder.");
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
                progress.Report("Görev çubuğu ortaya döndürülüyor...");

                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser,
                    KeyPath,
                    "TaskbarAl", 1);

                progress.Report("✅ Görev çubuğu ortaya döndürüldü.");
                return new OptimizationResult(true, "Görev çubuğu ortaya döndürüldü.");
            }
            catch (Exception ex)
            {
                return new OptimizationResult(false, $"Hata: {ex.Message}");
            }
        }, ct);
    }
}