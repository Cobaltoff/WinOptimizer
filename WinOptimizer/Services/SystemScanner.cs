using Microsoft.Win32;
using System.Management;
using System.Runtime.InteropServices;
using WinOptimizer.Models;

namespace WinOptimizer.Services;

// Sistem bilgilerini WMI ve Registry'den toplayan servis.
// Program açılışında ve "Yeniden Tara" butonunda çağrılır.
public class SystemScanner
{
    public event Action<string>? ScanProgress;

    public async Task<SystemInfo> ScanAsync(CancellationToken ct = default)
    {
        var info = new SystemInfo();

        await Task.Run(() =>
        {
            Report("İşletim sistemi taranıyor...");
            ScanOs(info);

            ct.ThrowIfCancellationRequested();
            Report("İşlemci taranıyor...");
            ScanCpu(info);

            ct.ThrowIfCancellationRequested();
            Report("RAM taranıyor...");
            ScanRam(info);

            ct.ThrowIfCancellationRequested();
            Report("GPU taranıyor...");
            ScanGpu(info);

            ct.ThrowIfCancellationRequested();
            Report("Diskler taranıyor...");
            ScanDisks(info);

            ct.ThrowIfCancellationRequested();
            Report("Anakart taranıyor...");
            ScanMotherboard(info);

            ct.ThrowIfCancellationRequested();
            Report("Ağ taranıyor...");
            ScanNetwork(info);

            ct.ThrowIfCancellationRequested();
            Report("Monitörler taranıyor...");
            ScanMonitors(info);

            Report("Tarama tamamlandı ✓");
        }, ct);

        return info;
    }

    // ── İŞLETİM SİSTEMİ ──────────────────────────────────────────────────────

    private static void ScanOs(SystemInfo info)
    {
        try
        {
            const string regPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

            info.OsName = RegistryHelper.GetString(
                RegistryHive.LocalMachine, regPath, "ProductName");

            info.OsEdition = RegistryHelper.GetString(
                RegistryHive.LocalMachine, regPath, "EditionID");

            info.OsVersion = RegistryHelper.GetString(
                RegistryHive.LocalMachine, regPath, "DisplayVersion"); // 23H2 gibi

            info.OsBuild = RegistryHelper.GetString(
                RegistryHive.LocalMachine, regPath, "CurrentBuildNumber");

            info.Architecture = RuntimeInformation.OSArchitecture.ToString();
        }
        catch { /* Sessizce geç */ }
    }

    // ── İŞLEMCİ ──────────────────────────────────────────────────────────────

    private static void ScanCpu(SystemInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed " +
                "FROM Win32_Processor");

            foreach (ManagementObject obj in searcher.Get())
            {
                info.CpuName = obj["Name"]?.ToString()?.Trim() ?? "";
                info.PhysicalCores = Convert.ToInt32(obj["NumberOfCores"]);
                info.LogicalProcessors = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                info.BaseClockGhz = Math.Round(
                    Convert.ToDouble(obj["MaxClockSpeed"]) / 1000.0, 1);
                break; // İlk işlemci yeterli
            }
        }
        catch { }
    }

    // ── RAM ───────────────────────────────────────────────────────────────────

    private static void ScanRam(SystemInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Tag, Capacity, Speed FROM Win32_PhysicalMemory");

            double totalBytes = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                double cap = Convert.ToDouble(obj["Capacity"]);
                totalBytes += cap;

                info.RamSlots.Add(new RamSlotInfo
                {
                    Tag = obj["Tag"]?.ToString() ?? "",
                    CapacityGb = Math.Round(cap / (1024 * 1024 * 1024), 1),
                    SpeedMhz = Convert.ToUInt32(obj["Speed"] ?? 0)
                });
            }

            info.TotalRamGb = Math.Round(totalBytes / (1024 * 1024 * 1024), 1);
        }
        catch { }
    }

    // ── GPU ───────────────────────────────────────────────────────────────────

    private static void ScanGpu(SystemInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, AdapterRAM, DriverVersion FROM Win32_VideoController");

            foreach (ManagementObject obj in searcher.Get())
            {
                // Temel Microsoft ekran adaptörlerini atla
                string name = obj["Name"]?.ToString() ?? "";
                if (name.Contains("Microsoft Basic")) continue;

                info.GpuName = name;
                double vramBytes = Convert.ToDouble(obj["AdapterRAM"] ?? 0);
                info.VramGb = Math.Round(vramBytes / (1024 * 1024 * 1024), 1);
                info.GpuDriverVersion = obj["DriverVersion"]?.ToString() ?? "";
                break;
            }
        }
        catch { }
    }

    // ── DİSKLER ───────────────────────────────────────────────────────────────

    private static void ScanDisks(SystemInfo info)
    {
        try
        {
            // Sürücü harflerini ve boyutlarını al
            using var driveSearcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Size, FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3");

            foreach (ManagementObject drive in driveSearcher.Get())
            {
                string letter = drive["DeviceID"]?.ToString() ?? "";
                double total = Convert.ToDouble(drive["Size"] ?? 0);
                double free = Convert.ToDouble(drive["FreeSpace"] ?? 0);

                info.Disks.Add(new DiskInfo
                {
                    DriveLetter = letter,
                    TotalGb = Math.Round(total / (1024 * 1024 * 1024), 1),
                    FreeGb = Math.Round(free / (1024 * 1024 * 1024), 1),
                    IsSsd = DetectSsd(letter),
                    FriendlyName = letter
                });
            }
        }
        catch { }
    }

    // SSD/HDD tespiti için Storage namespace WMI kullan
    private static bool DetectSsd(string driveLetter)
    {
        try
        {
            var scope = new ManagementScope(@"\\.\root\microsoft\windows\storage");
            scope.Connect();

            // Önce partition bul, sonra disk bul
            var query = new ObjectQuery(
                $"SELECT * FROM MSFT_PhysicalDisk");
            using var searcher = new ManagementObjectSearcher(scope, query);

            foreach (ManagementObject disk in searcher.Get())
            {
                // MediaType: 3=HDD, 4=SSD
                uint mediaType = Convert.ToUInt32(disk["MediaType"] ?? 0);
                if (mediaType == 4) return true;
            }
        }
        catch { }

        return false;
    }

    // ── ANAKART ───────────────────────────────────────────────────────────────

    private static void ScanMotherboard(SystemInfo info)
    {
        try
        {
            using var boardSearcher = new ManagementObjectSearcher(
                "SELECT Manufacturer, Product FROM Win32_BaseBoard");

            foreach (ManagementObject obj in boardSearcher.Get())
            {
                info.MotherboardManufacturer = obj["Manufacturer"]?.ToString() ?? "";
                info.MotherboardModel = obj["Product"]?.ToString() ?? "";
                break;
            }

            using var biosSearcher = new ManagementObjectSearcher(
                "SELECT SMBIOSBIOSVersion FROM Win32_BIOS");

            foreach (ManagementObject obj in biosSearcher.Get())
            {
                info.BiosVersion = obj["SMBIOSBIOSVersion"]?.ToString() ?? "";
                break;
            }
        }
        catch { }
    }

    // ── AĞ ────────────────────────────────────────────────────────────────────

    private static void ScanNetwork(SystemInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Description, IPAddress, DNSServerSearchOrder " +
                "FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=True");

            foreach (ManagementObject obj in searcher.Get())
            {
                info.NetworkAdapter = obj["Description"]?.ToString() ?? "";

                var ips = obj["IPAddress"] as string[];
                info.IpAddress = ips?.FirstOrDefault(ip => !ip.Contains(':')) ?? "";

                var dns = obj["DNSServerSearchOrder"] as string[];
                info.DnsServer = dns?.FirstOrDefault() ?? "";
                break;
            }
        }
        catch { }
    }

    // ── MONİTÖRLER ───────────────────────────────────────────────────────────

    private static void ScanMonitors(SystemInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, CurrentRefreshRate, MaxRefreshRate, " +
                "ScreenWidth, ScreenHeight FROM Win32_VideoController");

            foreach (ManagementObject obj in searcher.Get())
            {
                string name = obj["Name"]?.ToString() ?? "";
                if (name.Contains("Microsoft Basic")) continue;

                info.Monitors.Add(new MonitorInfo
                {
                    Name = name,
                    Width = Convert.ToInt32(obj["ScreenWidth"] ?? 0),
                    Height = Convert.ToInt32(obj["ScreenHeight"] ?? 0),
                    CurrentHz = Convert.ToInt32(obj["CurrentRefreshRate"] ?? 0),
                    MaxHz = Convert.ToInt32(obj["MaxRefreshRate"] ?? 0)
                });
            }

            // Monitör bulunamadıysa varsayılan ekle
            if (info.Monitors.Count == 0)
                info.Monitors.Add(new MonitorInfo { Name = "Bilinmiyor" });
        }
        catch { }
    }

    private void Report(string message) => ScanProgress?.Invoke(message);
}