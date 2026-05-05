using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Cleanup;

// Geçici dosyaları, Windows Update önbelleğini ve DNS cache'i temizler.
public class TempFilesOptimization : IOptimization
{
    public string Id => "cleanup_tempfiles";
    public string Name => "Geçici Dosyaları Temizle";
    public string ShortDescription => "Temp klasörleri, WU cache ve DNS cache temizle";
    public string WhatItDoes =>
        "%TEMP%, C:\\Windows\\Temp klasörlerindeki geçici dosyaları, " +
        "Windows Update indirme önbelleğini ve DNS cache'i temizler.";
    public string Benefit =>
        "Disk alanı açılır, sistem daha hızlı tarama yapar, " +
        "bozuk DNS kayıtları temizlenir.";
    public string Risk => "Yok — geçici dosyalar silinmek için tasarlanmıştır.";
    public bool IsReversible => false;
    public bool IsRecommended => true;
    public OptimizationCategory Category => OptimizationCategory.Cleanup;
    public OptimizationRisk RiskLevel => OptimizationRisk.None;
    public bool IsApplicable(SystemInfo info) => true;

    public async Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        long totalDeleted = 0;

        try
        {
            // %TEMP% klasörü
            progress.Report("Kullanıcı temp klasörü temizleniyor...");
            totalDeleted += CleanDirectory(
                Environment.GetEnvironmentVariable("TEMP") ?? "", progress);

            ct.ThrowIfCancellationRequested();

            // Windows Temp
            progress.Report("Windows temp klasörü temizleniyor...");
            totalDeleted += CleanDirectory(
                @"C:\Windows\Temp", progress);

            ct.ThrowIfCancellationRequested();

            // Windows Update Cache
            progress.Report("Windows Update önbelleği temizleniyor...");
            await CleanWindowsUpdateCacheAsync(progress, ct);

            ct.ThrowIfCancellationRequested();

            // DNS Cache
            progress.Report("DNS önbelleği temizleniyor...");
            await PowerShellRunner.RunExeAsync(
                "ipconfig", "/flushdns", progress);

            // Geri Dönüşüm Kutusu
            progress.Report("Geri dönüşüm kutusu boşaltılıyor...");
            await EmptyRecycleBinAsync(progress);

            double mb = totalDeleted / (1024.0 * 1024.0);
            progress.Report($"✅ Temizleme tamamlandı. {mb:F1} MB alan açıldı.");

            return new OptimizationResult(true,
                $"Temizleme tamamlandı. {mb:F1} MB alan açıldı.");
        }
        catch (OperationCanceledException)
        {
            return new OptimizationResult(false, "İşlem iptal edildi.");
        }
        catch (Exception ex)
        {
            return new OptimizationResult(false, $"Hata: {ex.Message}");
        }
    }

    // Geri alma desteklenmiyor — dosyalar kalıcı silindi
    public Task<OptimizationResult> RevertAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        return Task.FromResult(new OptimizationResult(
            false, "Geçici dosya temizliği geri alınamaz."));
    }

    // ── YARDIMCI METODLAR ────────────────────────────────────────────────────

    private static long CleanDirectory(string path, IProgress<string> progress)
    {
        long deleted = 0;

        if (!Directory.Exists(path)) return 0;

        try
        {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var info = new FileInfo(file);
                    deleted += info.Length;
                    File.Delete(file);
                }
                catch { /* Kullanımda olan dosyaları atla */ }
            }

            // Boş klasörleri sil
            foreach (var dir in Directory.GetDirectories(path))
            {
                try { Directory.Delete(dir, recursive: true); }
                catch { }
            }
        }
        catch { }

        double mb = deleted / (1024.0 * 1024.0);
        if (mb > 0.1)
            progress.Report($"  {Path.GetFileName(path)}: {mb:F1} MB temizlendi");

        return deleted;
    }

    private static async Task CleanWindowsUpdateCacheAsync(
        IProgress<string> progress,
        CancellationToken ct)
    {
        // Windows Update servisini durdur
        await PowerShellRunner.RunAsync(
            "Stop-Service wuauserv -Force", progress, ct);

        // Cache klasörünü temizle
        CleanDirectory(
            @"C:\Windows\SoftwareDistribution\Download", progress);

        // Servisi tekrar başlat
        await PowerShellRunner.RunAsync(
            "Start-Service wuauserv", progress, ct);
    }

    private static async Task EmptyRecycleBinAsync(IProgress<string> progress)
    {
        try
        {
            await PowerShellRunner.RunAsync(
                "Clear-RecycleBin -Force -ErrorAction SilentlyContinue",
                progress);
        }
        catch { }
    }
}