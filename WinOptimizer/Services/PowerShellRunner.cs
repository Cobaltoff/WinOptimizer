using System.Diagnostics;
using System.Text;

namespace WinOptimizer.Services;

// PowerShell komutlarını arka planda çalıştıran servis.
// Tüm PS komutları bu sınıf üzerinden geçer.
public static class PowerShellRunner
{
    // Tek satır PS komutu çalıştır, çıktıyı string olarak döndür
    public static async Task<(bool Success, string Output)> RunAsync(
        string command,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            progress?.Report($"PS: {command}");

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NonInteractive -NoProfile -ExecutionPolicy Bypass -Command \"{EscapeCommand(command)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                outputBuilder.AppendLine(e.Data);
                progress?.Report(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct);

            bool success = process.ExitCode == 0;
            string output = success
                ? outputBuilder.ToString().Trim()
                : errorBuilder.ToString().Trim();

            if (!success)
                progress?.Report($"⚠️ Hata: {output}");

            return (success, output);
        }
        catch (OperationCanceledException)
        {
            return (false, "İşlem iptal edildi.");
        }
        catch (Exception ex)
        {
            progress?.Report($"❌ {ex.Message}");
            return (false, ex.Message);
        }
    }

    // Birden fazla PS komutu arka arkaya çalıştır
    public static async Task<bool> RunManyAsync(
        IEnumerable<string> commands,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        foreach (var cmd in commands)
        {
            ct.ThrowIfCancellationRequested();
            var (success, _) = await RunAsync(cmd, progress, ct);
            if (!success) return false;
        }
        return true;
    }

    // bcdedit gibi sistem araçlarını doğrudan çalıştır
    public static async Task<(bool Success, string Output)> RunExeAsync(
        string exePath,
        string arguments,
        IProgress<string>? progress = null)
    {
        try
        {
            progress?.Report($"► {exePath} {arguments}");

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            bool success = process.ExitCode == 0;
            progress?.Report(success ? output.Trim() : $"⚠️ {error.Trim()}");
            return (success, success ? output.Trim() : error.Trim());
        }
        catch (Exception ex)
        {
            progress?.Report($"❌ {ex.Message}");
            return (false, ex.Message);
        }
    }

    // Tırnak sorunlarını önlemek için komutu temizle
    private static string EscapeCommand(string command)
        => command.Replace("\"", "\\\"");
}