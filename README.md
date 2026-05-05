# ⚡ WinOptimizer - Ultimate Windows Performance Tuner & Debloat Tool

[![Platform: Windows 10 | 11](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue.svg)]()
[![Built with: .NET 8 / C# 12](https://img.shields.io/badge/Built%20with-.NET%208%20%2F%20WPF-512BD4.svg)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)]()

**WinOptimizer** is a powerful, open-source, and strictly portable desktop utility designed to securely debloat, optimize, and speed up fresh Windows 10 and Windows 11 installations. 

No installation is required. It leaves zero traces, creates no background services, and automatically creates a **System Restore Point** before making any changes.

![WinOptimizer Screenshot](https://via.placeholder.com/800x450.png?text=WinOptimizer+Screenshot+Goes+Here) 
*(Note: Add a screenshot of your beautiful dark-mode UI here later!)*

## 🚀 Key Features

* **📦 100% Portable & Self-Contained:** Runs as a single `.exe` file. No dependencies, no `.NET` installation required on the target machine.
* **🛡️ Safe by Design:** Automatically creates a Windows System Restore Point before applying any tweaks. Rollback anytime!
* **🗑️ Ultimate Debloat & Nuke OneDrive:** 
  * Safely removes pre-installed Windows bloatware (Candy Crush, Xbox Game Bar, Cortana, etc.).
  * Features a special **"Nuke OneDrive"** option that completely uninstalls OneDrive, removes its registry keys, and unpins it from File Explorer forever.
* **⚙️ Hardware & Performance Tweaks:**
  * Unlocks maximum CPU core utilization (Fixes `msconfig` processor limitations).
  * Enables hidden "Ultimate Performance" power plans.
  * Optimizes Game Mode and Hardware-Accelerated GPU Scheduling for lower latency in games like CS2 and Rust.
* **🔒 Privacy & Telemetry Control:** Disables Windows tracking, ad IDs, and background activity history.
* **🌐 Network Optimization:** Disables Nagle's Algorithm (`TcpAckFrequency`) for improved ping in competitive multiplayer games.
* **🖥️ Windows 11 Specific Fixes:** Brings back the classic Windows 10 context menu and left-aligns the taskbar with one click.

## 🛠️ How to Use

1. Go to the [Releases](../../releases) page and download the latest `WinOptimizer.exe`.
2. Run the program (Administrator privileges are required to modify system settings).
3. The app will perform an automatic system scan.
4. Navigate through the categories on the left panel.
5. Check the tweaks you want to apply (Hover over the "ⓘ" icon to see exactly what each tweak does and its risks).
6. Click **"Apply Selected"**.

## 👨‍💻 Developer Notes

This project is built using **C# 12, .NET 8, and WPF** utilizing the MVVM pattern. The codebase is highly modular; adding a new optimization tweak is as simple as implementing the `IOptimization` interface.

```csharp
// Example of the clean architecture used
public interface IOptimization
{
    string Name { get; }
    string Benefit { get; }
    bool IsReversible { get; }
    Task<bool> ApplyAsync(IProgress<string> progress);
}
