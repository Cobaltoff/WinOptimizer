using WinOptimizer.Models;

namespace WinOptimizer.Optimizations;

// Tüm optimizasyon sınıflarının uygulaması gereken arayüz.
// Yeni optimizasyon eklemek için sadece bu arayüzü implement et.
public interface IOptimization
{
    string Id { get; }
    string Name { get; }
    string ShortDescription { get; }
    string WhatItDoes { get; }
    string Benefit { get; }
    string Risk { get; }
    bool IsReversible { get; }
    bool IsRecommended { get; }
    OptimizationCategory Category { get; }
    OptimizationRisk RiskLevel { get; }

    // Sisteme göre bu optimizasyon gösterilsin mi?
    bool IsApplicable(SystemInfo info);

    // Optimizasyonu uygula — progress ile canlı log yaz
    Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default);

    // Geri al
    Task<OptimizationResult> RevertAsync(
        IProgress<string> progress,
        CancellationToken ct = default);
}

// Her işlemin sonucu bu record ile döner
public record OptimizationResult(
    bool Success,
    string Message,
    bool RequiresRestart = false);