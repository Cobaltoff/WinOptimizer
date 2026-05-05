using System.Management;
using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Network;

// Nagle algoritmasını kapatır — online oyunlarda ping azalır.
public class NagleOptimization : IOptimization
{
    public string Id => "network_nagle";
    public string Name => "Nagle Algoritmasını Kapat";
    public string ShortDescription => "Online oyunlarda ping ve gecikmeyi azalt";
    public string WhatItDoes =>
        "TCP paketlerini birleştiren Nagle algoritmasını kapatır. " +
        "Her paket hemen gönderilir, bekleme olmaz.";
    public string Benefit =>
        "Online oyunlarda ping kararlılığı artar, " +
        "ani gecikme (spike) sorunları azalır.";
    public string Risk =>
        "Yüksek hacimli veri transferlerinde (dosya indirme) " +
        "çok küçük bir verimlilik kaybı olabilir.";
    public bool IsReversible => true;
    public bool IsRecommended => true;
    public OptimizationCategory Category => OptimizationCategory.Network;
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
                progress.Report("Ağ adaptörleri taranıyor...");

                // Tüm aktif ağ adaptörlerini bul
                var interfaces = GetNetworkInterfaces();

                if (interfaces.Count == 0)
                {
                    return new OptimizationResult(false,
                        "Aktif ağ adaptörü bulunamadı.");
                }

                foreach (var iface in interfaces)
                {
                    progress.Report($"Nagle kapatılıyor: {iface}");

                    string path =
                        $@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{iface}";

                    // TcpAckFrequency = 1 → her ACK hemen gönderilir
                    RegistryHelper.SetValue(
                        RegistryHive.LocalMachine, path,
                        "TcpAckFrequency", 1);

                    // TCPNoDelay = 1 → Nagle kapalı
                    RegistryHelper.SetValue(
                        RegistryHive.LocalMachine, path,
                        "TCPNoDelay", 1);
                }

                // Global TCP ayarları
                RegistryHelper.SetValue(
                    RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                    "TCPNoDelay", 1);

                progress.Report($"✅ {interfaces.Count} adaptörde Nagle kapatıldı.");
                return new OptimizationResult(true,
                    $"Nagle algoritması {interfaces.Count} adaptörde kapatıldı.");
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
                progress.Report("Nagle ayarları varsayılana döndürülüyor...");

                var interfaces = GetNetworkInterfaces();

                foreach (var iface in interfaces)
                {
                    string path =
                        $@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{iface}";

                    RegistryHelper.DeleteValue(
                        RegistryHive.LocalMachine, path, "TcpAckFrequency");
                    RegistryHelper.DeleteValue(
                        RegistryHive.LocalMachine, path, "TCPNoDelay");
                }

                RegistryHelper.DeleteValue(
                    RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                    "TCPNoDelay");

                progress.Report("✅ Nagle ayarları sıfırlandı.");
                return new OptimizationResult(true, "Nagle ayarları varsayılana döndürüldü.");
            }
            catch (Exception ex)
            {
                return new OptimizationResult(false, $"Hata: {ex.Message}");
            }
        }, ct);
    }

    // Aktif ağ adaptörlerinin GUID'lerini döndür
    private static List<string> GetNetworkInterfaces()
    {
        var list = new List<string>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT SettingID FROM Win32_NetworkAdapterConfiguration " +
                "WHERE IPEnabled=True");

            foreach (ManagementObject obj in searcher.Get())
            {
                string? guid = obj["SettingID"]?.ToString();
                if (!string.IsNullOrEmpty(guid))
                    list.Add(guid);
            }
        }
        catch { }
        return list;
    }
}