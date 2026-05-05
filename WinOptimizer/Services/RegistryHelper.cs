using Microsoft.Win32;

namespace WinOptimizer.Services;

// Registry okuma/yazma işlemleri için yardımcı sınıf.
// Tüm optimizasyon sınıfları bu sınıfı kullanır — doğrudan Registry'ye dokunmaz.
public static class RegistryHelper
{
    // ── OKUMA ────────────────────────────────────────────────────────────────

    public static object? GetValue(RegistryHive hive, string keyPath, string valueName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(keyPath, writable: false);
            return key?.GetValue(valueName);
        }
        catch
        {
            return null;
        }
    }

    public static string GetString(RegistryHive hive, string keyPath, string valueName, string defaultValue = "")
    {
        var val = GetValue(hive, keyPath, valueName);
        return val?.ToString() ?? defaultValue;
    }

    public static int GetInt(RegistryHive hive, string keyPath, string valueName, int defaultValue = 0)
    {
        var val = GetValue(hive, keyPath, valueName);
        if (val is int i) return i;
        if (int.TryParse(val?.ToString(), out int parsed)) return parsed;
        return defaultValue;
    }

    // ── YAZMA ────────────────────────────────────────────────────────────────

    public static bool SetValue(
        RegistryHive hive,
        string keyPath,
        string valueName,
        object value,
        RegistryValueKind kind = RegistryValueKind.DWord)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            // Anahtar yoksa oluştur
            using var key = baseKey.CreateSubKey(keyPath, writable: true);
            if (key == null) return false;
            key.SetValue(valueName, value, kind);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── SİLME ────────────────────────────────────────────────────────────────

    public static bool DeleteValue(RegistryHive hive, string keyPath, string valueName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(keyPath, writable: true);
            if (key == null) return true; // Zaten yok, sorun değil
            key.DeleteValue(valueName, throwOnMissingValue: false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool DeleteKey(RegistryHive hive, string keyPath)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            baseKey.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── YEDEKLEME ────────────────────────────────────────────────────────────

    // Değişiklik öncesi mevcut değeri yedekle
    // Dönen dictionary daha sonra RevertAsync'te kullanılır
    public static Dictionary<string, object?> BackupValues(
        RegistryHive hive,
        string keyPath,
        params string[] valueNames)
    {
        var backup = new Dictionary<string, object?>();
        foreach (var name in valueNames)
            backup[name] = GetValue(hive, keyPath, name);
        return backup;
    }

    // ── YARDIMCI ─────────────────────────────────────────────────────────────

    public static bool KeyExists(RegistryHive hive, string keyPath)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(keyPath);
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool ValueExists(RegistryHive hive, string keyPath, string valueName)
    {
        var val = GetValue(hive, keyPath, valueName);
        return val != null;
    }
}