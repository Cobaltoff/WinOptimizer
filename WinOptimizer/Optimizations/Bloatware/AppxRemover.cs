using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Bloatware;

// Microsoft bloatware uygulamalarını kaldırır.
public class AppxRemover : IOptimization
{
    public string Id => "bloatware_appx";
    public string Name => "Microsoft Bloatware Kaldır";
    public string ShortDescription => "Gereksiz önceden yüklenmiş uygulamaları sil";
    public string WhatItDoes =>
        "Hava Durumu, Haberler, Solitaire, 3D Viewer, " +
        "Mixed Reality, Groove Music gibi uygulamaları kaldırır.";
    public string Benefit =>
        "Disk alanı açılır, RAM kullanımı azalır, " +
        "başlangıç süresi kısalır.";
    public string Risk =>
        "Kaldırılan uygulamalar Microsoft Store'dan tekrar indirilebilir.";
    public bool IsReversible => true;
    public bool IsRecommended => true;
    public OptimizationCategory Category => OptimizationCategory.Bloatware;
    public OptimizationRisk RiskLevel => OptimizationRisk.Low;
    public bool IsApplicable(SystemInfo info) => true;

    // Kaldırılacak uygulama listesi
    private static readonly string[] BloatwareApps =
    {
        "Microsoft.BingWeather",
        "Microsoft.BingNews",
        "Microsoft.BingFinance",
        "Microsoft.BingSports",
        "Microsoft.GetHelp",
        "Microsoft.Getstarted",
        "Microsoft.MicrosoftSolitaireCollection",
        "Microsoft.MicrosoftOfficeHub",
        "Microsoft.Office.OneNote",
        "Microsoft.People",
        "Microsoft.Print3D",
        "Microsoft.Microsoft3DViewer",
        "Microsoft.MixedReality.Portal",
        "Microsoft.SkypeApp",
        "Microsoft.Todos",
        "Microsoft.WindowsFeedbackHub",
        "Microsoft.WindowsMaps",
        "Microsoft.WindowsSoundRecorder",
        "Microsoft.Xbox.TCUI",
        "Microsoft.XboxApp",
        "Microsoft.XboxGameOverlay",
        "Microsoft.XboxGamingOverlay",
        "Microsoft.XboxIdentityProvider",
        "Microsoft.XboxSpeechToTextOverlay",
        "Microsoft.YourPhone",
        "Microsoft.ZuneMusic",
        "Microsoft.ZuneVideo",
        "MicrosoftTeams",
        "Clipchamp.Clipchamp",
        "Microsoft.PowerAutomateDesktop",
    };

    public async Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        int removed = 0;
        int failed = 0;

        foreach (var app in BloatwareApps)
        {
            ct.ThrowIfCancellationRequested();
            progress.Report($"Kaldırılıyor: {app}");

            // Kullanıcı için kaldır
            var (ok1, _) = await PowerShellRunner.RunAsync(
                $"Get-AppxPackage {app} | Remove-AppxPackage -ErrorAction SilentlyContinue",
                progress, ct);

            // Tüm kullanıcılar için provisioned paketi kaldır
            var (ok2, _) = await PowerShellRunner.RunAsync(
                $"Get-AppxProvisionedPackage -Online | " +
                $"Where-Object DisplayName -eq '{app}' | " +
                $"Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue",
                progress, ct);

            if (ok1 || ok2)
                removed++;
            else
                failed++;
        }

        progress.Report($"✅ {removed} uygulama kaldırıldı, {failed} atlandı.");
        return new OptimizationResult(true,
            $"{removed} bloatware uygulaması kaldırıldı.");
    }

    public async Task<OptimizationResult> RevertAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        progress.Report("Bloatware uygulamaları Microsoft Store'dan geri yüklenebilir.");
        progress.Report("Otomatik geri yükleme desteklenmiyor.");

        // Microsoft Store'u aç
        await PowerShellRunner.RunAsync(
            "Start-Process ms-windows-store:", progress, ct);

        return new OptimizationResult(false,
            "Bloatware geri yüklemesi desteklenmiyor. " +
            "Microsoft Store'dan manuel olarak indirebilirsiniz.");
    }
}