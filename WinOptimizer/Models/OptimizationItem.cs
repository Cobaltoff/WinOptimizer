namespace WinOptimizer.Models;

public enum OptimizationCategory
{
    Performance,
    Display,
    Bloatware,
    Startup,
    Privacy,
    Cleanup,
    Network,
    Windows11
}

public enum OptimizationRisk
{
    None,    // Güvenli, geri alınabilir
    Low,     // Düşük risk
    Medium,  // Orta risk
    High     // Geri alınamaz veya kritik
}

// Her optimizasyon toggle'ı bu model ile temsil edilir.
public class OptimizationItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string ShortDescription { get; set; } = "";

    // Tooltip içeriği
    public string WhatItDoes { get; set; } = "";
    public string Benefit { get; set; } = "";
    public string RiskDescription { get; set; } = "";
    public bool IsReversible { get; set; } = true;

    public OptimizationCategory Category { get; set; }
    public OptimizationRisk Risk { get; set; } = OptimizationRisk.None;

    // UI durumu
    public bool IsSelected { get; set; }
    public bool IsEnabled { get; set; } = true;      // false = kilitli
    public bool IsApplied { get; set; }
    public bool IsRecommended { get; set; }

    // Bazı ayarlar sadece SSD'de veya Win11'de gösterilir
    public Func<SystemInfo, bool>? ApplicabilityCheck { get; set; }
}