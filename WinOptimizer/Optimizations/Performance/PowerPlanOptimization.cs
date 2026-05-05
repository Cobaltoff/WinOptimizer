using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Performance;

// Güç planını "Ultimate Performance" olarak ayarlar.
// Dizüstü bilgisayarlarda pil uyarısı gösterir.
public class PowerPlanOptimization : IOptimization
{
    public string Id => "perf_powerplan";
    public string Name => "Ultimate Performance Güç Planı";
    public string ShortDescription => "Maksimum performans için güç planını değiştir";
    public string WhatItDoes =>
        "Windows'un gizli 'Ultimate Performance' güç planını etkinleştirir. " +
        "İşlemcinin hiçbir zaman düşük hıza geçmemesini sağlar.";
    public string Benefit =>
        "Özellikle masaüstü oyunlarda ve ağır iş yüklerinde tutarlı yüksek performans.";
    public string Risk =>
        "Dizüstü bilgisayarlarda pil ömrü önemli ölçüde kısalır.";
    public bool IsReversible => true;
    public bool IsRecommended => true;
    public OptimizationCategory Category => OptimizationCategory.Performance;
    public OptimizationRisk RiskLevel => OptimizationRisk.Low;

    // Dizüstü ise uyarı verilir ama gösterilir
    public bool IsApplicable(SystemInfo info) => true;

    public async Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        progress.Report("Ultimate Performance güç planı etkinleştiriliyor...");

        // Planı sisteme ekle
        var (added, addOutput) = await PowerShellRunner.RunExeAsync(
            "powercfg",
            "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61",
            progress);

        // Çıktıdan GUID'i oku
        string guid = ExtractGuid(addOutput);

        if (string.IsNullOrEmpty(guid))
        {
            // Plan zaten var olabilir — aktif etmeye çalış
            progress.Report("Plan zaten mevcut, aktif ediliyor...");
            var (ok, _) = await PowerShellRunner.RunExeAsync(
                "powercfg",
                "-setactive e9a42b02-d5df-448d-aa00-03f14749eb61",
                progress);

            return ok
                ? new OptimizationResult(true, "Ultimate Performance aktif edildi.", false)
                : new OptimizationResult(false, "Güç planı değiştirilemedi.");
        }

        // Yeni planı aktif et
        var (activated, _) = await PowerShellRunner.RunExeAsync(
            "powercfg", $"-setactive {guid}", progress);

        if (activated)
        {
            progress.Report("✅ Ultimate Performance güç planı aktif.");
            return new OptimizationResult(true, "Ultimate Performance güç planı etkinleştirildi.");
        }

        return new OptimizationResult(false, "Güç planı aktif edilemedi.");
    }

    public async Task<OptimizationResult> RevertAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        progress.Report("Güç planı varsayılana döndürülüyor...");

        // Dengeli plana dön (Windows varsayılanı)
        var (ok, _) = await PowerShellRunner.RunExeAsync(
            "powercfg",
            "-setactive 381b4222-f694-41f0-9685-ff5bb260df2e",
            progress);

        return ok
            ? new OptimizationResult(true, "Güç planı 'Dengeli' olarak sıfırlandı.")
            : new OptimizationResult(false, "Güç planı sıfırlanamadı.");
    }

    // powercfg çıktısından GUID'i çek
    private static string ExtractGuid(string output)
    {
        if (string.IsNullOrEmpty(output)) return "";
        var match = System.Text.RegularExpressions.Regex.Match(
            output,
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        return match.Success ? match.Value : "";
    }
}