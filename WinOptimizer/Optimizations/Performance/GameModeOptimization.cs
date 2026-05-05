using Microsoft.Win32;
using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Performance;

// Windows Oyun Modunu ve Hardware-Accelerated GPU Scheduling'i açar.
public class GameModeOptimization : IOptimization
{
    public string Id => "perf_gamemode";
    public string Name => "Oyun Modu + HAGS";
    public string ShortDescription => "Oyun modunu ve donanım hızlandırmalı GPU zamanlamasını aç";
    public string WhatItDoes =>
        "Oyun Modu: oyun çalışırken Windows arka plan görevlerini azaltır. " +
        "HAGS: GPU'nun kendi zamanlama işini CPU yerine kendisi yapmasını sağlar.";
    public string Benefit =>
        "FPS kararlılığı artar, taklamalar azalır. " +
        "HAGS özellikle modern GPU'larda (RTX 30/40, RX 6000+) gecikmeyi düşürür.";
    public string Risk =>
        "HAGS için yeniden başlatma gerekir. " +
        "Çok eski GPU'larda HAGS sorun çıkarabilir.";
    public bool IsReversible => true;
    public bool IsRecommended => true;
    public OptimizationCategory Category => OptimizationCategory.Performance;
    public OptimizationRisk RiskLevel => OptimizationRisk.Low;
    public bool IsApplicable(SystemInfo info) => true;

    private const string GameBarPath =
        @"Software\Microsoft\GameBar";
    private const string HagsPath =
        @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";

    public async Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                progress.Report("Oyun Modu etkinleştiriliyor...");

                // Oyun Modunu aç
                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser, GameBarPath,
                    "AllowAutoGameMode", 1);

                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser, GameBarPath,
                    "AutoGameModeEnabled", 1);

                progress.Report("Hardware-Accelerated GPU Scheduling etkinleştiriliyor...");

                // HAGS aç (2 = etkin)
                RegistryHelper.SetValue(
                    RegistryHive.LocalMachine, HagsPath,
                    "HwSchMode", 2);

                progress.Report("✅ Oyun Modu ve HAGS etkinleştirildi.");
                return new OptimizationResult(
                    true,
                    "Oyun Modu ve HAGS etkinleştirildi.",
                    RequiresRestart: true); // HAGS için restart gerekir
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
                progress.Report("Oyun Modu varsayılana döndürülüyor...");

                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser, GameBarPath,
                    "AllowAutoGameMode", 0);

                RegistryHelper.SetValue(
                    RegistryHive.CurrentUser, GameBarPath,
                    "AutoGameModeEnabled", 0);

                // HAGS kapat (1 = varsayılan)
                RegistryHelper.SetValue(
                    RegistryHive.LocalMachine, HagsPath,
                    "HwSchMode", 1);

                progress.Report("✅ Oyun Modu ve HAGS kapatıldı.");
                return new OptimizationResult(
                    true, "Oyun Modu ve HAGS kapatıldı.", RequiresRestart: true);
            }
            catch (Exception ex)
            {
                return new OptimizationResult(false, $"Hata: {ex.Message}");
            }
        }, ct);
    }
}