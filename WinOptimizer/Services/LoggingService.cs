using System.IO;
using System.Text;

namespace WinOptimizer.Services;

// Uygulama loglarını bellekte tutar.
// İsteğe bağlı olarak masaüstüne .txt olarak kaydeder.
public class LoggingService
{
    private readonly List<LogEntry> _entries = new();
    private readonly object _lock = new();

    // Yeni log satırı ekle
    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Message = message,
            Level = level
        };

        lock (_lock)
            _entries.Add(entry);

        // UI'a bildir
        OnNewEntry?.Invoke(entry);
    }

    public void Info(string message) => Log(message, LogLevel.Info);
    public void Success(string message) => Log(message, LogLevel.Success);
    public void Warning(string message) => Log(message, LogLevel.Warning);
    public void Error(string message) => Log(message, LogLevel.Error);

    // Tüm logları temizle (yeni oturum için)
    public void Clear()
    {
        lock (_lock)
            _entries.Clear();
    }

    // Tüm logları döndür
    public IReadOnlyList<LogEntry> GetAll()
    {
        lock (_lock)
            return _entries.AsReadOnly();
    }

    // Masaüstüne .txt olarak kaydet
    public async Task<string> SaveToDesktopAsync()
    {
        try
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = $"WinOptimizer_Log_{DateTime.Now:yyyy-MM-dd_HH-mm}.txt";
            string path = Path.Combine(desktop, fileName);

            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine("  WinOptimizer — İşlem Raporu");
            sb.AppendLine($"  Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine();

            lock (_lock)
            {
                foreach (var entry in _entries)
                {
                    string prefix = entry.Level switch
                    {
                        LogLevel.Success => "✅",
                        LogLevel.Warning => "⚠️",
                        LogLevel.Error => "❌",
                        _ => "ℹ️"
                    };
                    sb.AppendLine($"[{entry.Timestamp:HH:mm:ss}] {prefix} {entry.Message}");
                }
            }

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
            return path;
        }
        catch (Exception ex)
        {
            return $"Hata: {ex.Message}";
        }
    }

    // Yeni log geldiğinde UI bu event'i dinler
    public event Action<LogEntry>? OnNewEntry;

    // İstatistik: başarılı / uyarı / hata sayısı
    public (int Success, int Warning, int Error) GetStats()
    {
        lock (_lock)
        {
            return (
                _entries.Count(e => e.Level == LogLevel.Success),
                _entries.Count(e => e.Level == LogLevel.Warning),
                _entries.Count(e => e.Level == LogLevel.Error)
            );
        }
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = "";
    public LogLevel Level { get; set; }
}

public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error
}