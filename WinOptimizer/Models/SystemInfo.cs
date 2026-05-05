namespace WinOptimizer.Models;

// Sistem tarama sonucunu tutan ana model.
// SystemScanner bu sınıfı doldurup ViewModel'e iletir.
public class SystemInfo
{
    // --- İşletim Sistemi ---
    public string OsName { get; set; } = "";
    public string OsEdition { get; set; } = "";      // Home, Pro, Enterprise
    public string OsVersion { get; set; } = "";
    public string OsBuild { get; set; } = "";
    public string Architecture { get; set; } = "";   // x64, ARM64

    // --- İşlemci ---
    public string CpuName { get; set; } = "";
    public int PhysicalCores { get; set; }
    public int LogicalProcessors { get; set; }
    public double BaseClockGhz { get; set; }

    // --- RAM ---
    public double TotalRamGb { get; set; }
    public List<RamSlotInfo> RamSlots { get; set; } = new();

    // --- GPU ---
    public string GpuName { get; set; } = "";
    public double VramGb { get; set; }
    public string GpuDriverVersion { get; set; } = "";

    // --- Monitörler ---
    public List<MonitorInfo> Monitors { get; set; } = new();

    // --- Diskler ---
    public List<DiskInfo> Disks { get; set; } = new();

    // --- Anakart ---
    public string MotherboardManufacturer { get; set; } = "";
    public string MotherboardModel { get; set; } = "";
    public string BiosVersion { get; set; } = "";

    // --- Ağ ---
    public string NetworkAdapter { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string DnsServer { get; set; } = "";

    // Hesaplanan özellikler
    public bool HasSsd => Disks.Any(d => d.IsSsd);
    public bool IsWindows11 =>
        int.TryParse(OsBuild, out int b) && b >= 22000;
    public bool IsLaptop { get; set; }
}

public class RamSlotInfo
{
    public string Tag { get; set; } = "";
    public double CapacityGb { get; set; }
    public uint SpeedMhz { get; set; }
}

public class MonitorInfo
{
    public string Name { get; set; } = "";
    public string DeviceName { get; set; } = "";  // örn: \\.\DISPLAY1
    public int Width { get; set; }
    public int Height { get; set; }
    public int CurrentHz { get; set; }
    public int MaxHz { get; set; }
    public bool SupportsHdr { get; set; }
}

public class DiskInfo
{
    public string DriveLetter { get; set; } = "";
    public string FriendlyName { get; set; } = "";
    public bool IsSsd { get; set; }
    public bool IsNvme { get; set; }
    public double TotalGb { get; set; }
    public double FreeGb { get; set; }
}