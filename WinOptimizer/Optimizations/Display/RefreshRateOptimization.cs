using System.Runtime.InteropServices;
using WinOptimizer.Models;
using WinOptimizer.Services;

namespace WinOptimizer.Optimizations.Display;

// Tüm monitörleri maksimum desteklenen yenileme hızına ayarlar.
public class RefreshRateOptimization : IOptimization
{
    public string Id => "display_refreshrate";
    public string Name => "Yenileme Hızını Maksimuma Ayarla";
    public string ShortDescription => "Tüm monitörleri en yüksek Hz değerine ayarla";
    public string WhatItDoes =>
        "Bağlı tüm monitörlerin desteklediği en yüksek " +
        "yenileme hızını otomatik olarak aktif eder.";
    public string Benefit =>
        "Daha akıcı görüntü, göz yorgunluğu azalır, " +
        "oyunlarda daha yüksek FPS limiti.";
    public string Risk =>
        "Monitörün desteklemediği bir değer seçilirse " +
        "ekran geçici olarak kararabilir, otomatik düzelir.";
    public bool IsReversible => true;
    public bool IsRecommended => true;
    public OptimizationCategory Category => OptimizationCategory.Display;
    public OptimizationRisk RiskLevel => OptimizationRisk.Low;
    public bool IsApplicable(SystemInfo info) => true;

    // Win32 API sabitleri
    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int CDS_UPDATEREGISTRY = 0x01;
    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const int DM_DISPLAYFREQUENCY = 0x400000;

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayDevices(
        string? lpDevice, uint iDevNum,
        ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettingsEx(
        string lpszDeviceName, int iModeNum,
        ref DEVMODE lpDevMode, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern int ChangeDisplaySettingsEx(
        string lpszDeviceName, ref DEVMODE lpDevMode,
        IntPtr hwnd, uint dwflags, IntPtr lParam);

    public async Task<OptimizationResult> ApplyAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            int updated = 0;

            try
            {
                progress.Report("Monitörler taranıyor...");

                uint deviceIndex = 0;
                var device = new DISPLAY_DEVICE();
                device.cb = Marshal.SizeOf(device);

                while (EnumDisplayDevices(null, deviceIndex, ref device, 0))
                {
                    deviceIndex++;

                    // Aktif monitör değilse atla
                    if ((device.StateFlags & 0x1) == 0)
                    {
                        device = new DISPLAY_DEVICE();
                        device.cb = Marshal.SizeOf(device);
                        continue;
                    }

                    string deviceName = device.DeviceName;
                    progress.Report($"Monitör: {deviceName}");

                    // Mevcut ayarları al
                    var currentMode = new DEVMODE();
                    currentMode.dmSize = (short)Marshal.SizeOf(currentMode);
                    EnumDisplaySettingsEx(deviceName, ENUM_CURRENT_SETTINGS,
                        ref currentMode, 0);

                    // Desteklenen maksimum Hz'i bul
                    int maxHz = FindMaxRefreshRate(deviceName,
                        currentMode.dmPelsWidth, currentMode.dmPelsHeight);

                    if (maxHz <= currentMode.dmDisplayFrequency)
                    {
                        progress.Report(
                            $"  Zaten maksimumda: {currentMode.dmDisplayFrequency}Hz");
                        device = new DISPLAY_DEVICE();
                        device.cb = Marshal.SizeOf(device);
                        continue;
                    }

                    progress.Report(
                        $"  {currentMode.dmDisplayFrequency}Hz → {maxHz}Hz");

                    // Yeni Hz uygula
                    var newMode = currentMode;
                    newMode.dmDisplayFrequency = maxHz;
                    newMode.dmFields |= DM_DISPLAYFREQUENCY;

                    int result = ChangeDisplaySettingsEx(
                        deviceName, ref newMode, IntPtr.Zero,
                        CDS_UPDATEREGISTRY, IntPtr.Zero);

                    if (result == DISP_CHANGE_SUCCESSFUL)
                    {
                        progress.Report($"  ✅ {maxHz}Hz ayarlandı.");
                        updated++;
                    }
                    else
                    {
                        progress.Report($"  ⚠️ Ayarlanamadı (kod: {result})");
                    }

                    device = new DISPLAY_DEVICE();
                    device.cb = Marshal.SizeOf(device);
                }

                return updated > 0
                    ? new OptimizationResult(true,
                        $"{updated} monitör maksimum yenileme hızına ayarlandı.")
                    : new OptimizationResult(true,
                        "Tüm monitörler zaten maksimum yenileme hızında.");
            }
            catch (Exception ex)
            {
                return new OptimizationResult(false, $"Hata: {ex.Message}");
            }
        }, ct);
    }

    public Task<OptimizationResult> RevertAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        progress.Report("Yenileme hızını manuel olarak Ekran Ayarları'ndan değiştirebilirsiniz.");
        return Task.FromResult(new OptimizationResult(
            true, "Ekran Ayarları > Gelişmiş ekran ayarları'ndan manuel değiştirebilirsiniz."));
    }

    private static int FindMaxRefreshRate(string deviceName, int width, int height)
    {
        int maxHz = 60;
        int modeIndex = 0;
        var mode = new DEVMODE();
        mode.dmSize = (short)Marshal.SizeOf(mode);

        while (EnumDisplaySettingsEx(deviceName, modeIndex, ref mode, 0))
        {
            if (mode.dmPelsWidth == width && mode.dmPelsHeight == height)
                if (mode.dmDisplayFrequency > maxHz)
                    maxHz = mode.dmDisplayFrequency;
            modeIndex++;
        }

        return maxHz;
    }

    // ── WIN32 STRUCT'LAR ─────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public uint dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public uint dmDisplayFlags;
        public int dmDisplayFrequency;
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }
}