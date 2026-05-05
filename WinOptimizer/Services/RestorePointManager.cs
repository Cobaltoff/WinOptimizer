using WinOptimizer.Services;

namespace WinOptimizer.Services;

// System Restore Point oluşturma ve geri yükleme işlemleri.
// Her "Seçilenleri Uygula" öncesinde otomatik çağrılır.
public class RestorePointManager
{
    // Restore point oluştur.
    // Windows son 24 saat içinde oluşturulduysa reddeder —
    // bu durumu geçici registry ayarıyla aşıyoruz.
    public async Task<(bool Success, string Message)> CreateRestorePointAsync(
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        progress.Report("Sistem geri yükleme noktası oluşturuluyor...");

        try
        {
            // 24 saat kısıtlamasını geçici olarak kaldır
            await DisableFrequencyLimitAsync();

            var (success, output) = await PowerShellRunner.RunAsync(
                "Checkpoint-Computer -Description 'WinOptimizer Backup' " +
                "-RestorePointType MODIFY_SETTINGS",
                progress, ct);

            // Kısıtlamayı geri getir
            await RestoreFrequencyLimitAsync();

            if (success)
            {
                progress.Report("✅ Geri yükleme noktası oluşturuldu.");
                return (true, "Geri yükleme noktası başarıyla oluşturuldu.");
            }
            else
            {
                // Sistem Geri Yükleme kapalı olabilir — kritik değil, devam et
                progress.Report("⚠️ Geri yükleme noktası oluşturulamadı (Sistem Geri Yükleme kapalı olabilir).");
                return (false, output);
            }
        }
        catch (Exception ex)
        {
            await RestoreFrequencyLimitAsync();
            progress.Report($"⚠️ Restore point hatası: {ex.Message}");
            return (false, ex.Message);
        }
    }

    // Mevcut restore point listesini getir
    public async Task<List<RestorePointInfo>> GetRestorePointsAsync()
    {
        var list = new List<RestorePointInfo>();

        try
        {
            var (success, output) = await PowerShellRunner.RunAsync(
                "Get-ComputerRestorePoint | Select-Object -Property " +
                "Description, CreationTime, SequenceNumber | ConvertTo-Json");

            if (!success || string.IsNullOrWhiteSpace(output)) return list;

            // Basit JSON parse — tam kütüphane kullanmıyoruz
            // Her satırı işle
            var lines = output.Split('\n');
            string desc = "", time = "", seq = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim().Trim(',');

                if (trimmed.Contains("\"Description\""))
                    desc = ExtractJsonValue(trimmed);
                else if (trimmed.Contains("\"CreationTime\""))
                    time = ExtractJsonValue(trimmed);
                else if (trimmed.Contains("\"SequenceNumber\""))
                {
                    seq = ExtractJsonValue(trimmed);
                    if (!string.IsNullOrEmpty(desc))
                    {
                        list.Add(new RestorePointInfo
                        {
                            Description = desc,
                            CreationTime = time,
                            SequenceNumber = int.TryParse(seq, out int s) ? s : 0
                        });
                        desc = time = seq = "";
                    }
                }
            }
        }
        catch { }

        return list;
    }

    // Belirli bir restore point'e geri dön
    public async Task<bool> RestoreToPointAsync(
        int sequenceNumber,
        IProgress<string> progress)
    {
        progress.Report($"Geri yükleme başlatılıyor (#{sequenceNumber})...");
        progress.Report("⚠️ Bilgisayar yeniden başlatılacak!");

        var (success, _) = await PowerShellRunner.RunAsync(
            $"Restore-Computer -RestorePoint {sequenceNumber}",
            progress);

        return success;
    }

    // ── YARDIMCI METODLAR ────────────────────────────────────────────────────

    // 24 saatlik restore point oluşturma kısıtlamasını geçici kaldır
    private static Task DisableFrequencyLimitAsync()
    {
        RegistryHelper.SetValue(
            Microsoft.Win32.RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore",
            "SystemRestorePointCreationFrequency",
            0);
        return Task.CompletedTask;
    }

    // Kısıtlamayı varsayılan değerine döndür (1440 dakika = 24 saat)
    private static Task RestoreFrequencyLimitAsync()
    {
        RegistryHelper.DeleteValue(
            Microsoft.Win32.RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore",
            "SystemRestorePointCreationFrequency");
        return Task.CompletedTask;
    }

    private static string ExtractJsonValue(string line)
    {
        var parts = line.Split(':');
        if (parts.Length < 2) return "";
        return parts[1].Trim().Trim('"').Trim(',').Trim('"');
    }
}

public class RestorePointInfo
{
    public string Description { get; set; } = "";
    public string CreationTime { get; set; } = "";
    public int SequenceNumber { get; set; }
}