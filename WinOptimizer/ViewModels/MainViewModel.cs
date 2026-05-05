using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinOptimizer.Models;
using WinOptimizer.Optimizations;
using WinOptimizer.Services;

namespace WinOptimizer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SystemScanner _scanner;
    private readonly RestorePointManager _restoreManager;
    private readonly LoggingService _logger;
    private readonly IEnumerable<IOptimization> _optimizations;

    public MainViewModel(
        SystemScanner scanner,
        RestorePointManager restoreManager,
        LoggingService logger,
        IEnumerable<IOptimization> optimizations)
    {
        _scanner = scanner;
        _restoreManager = restoreManager;
        _logger = logger;
        _optimizations = optimizations;

        _logger.OnNewEntry += entry =>
        {
            App.Current.Dispatcher.Invoke(() =>
                LogLines.Add($"[{entry.Timestamp:HH:mm:ss}] {entry.Message}"));
        };
    }

    // ── PROPERTIES ────────────────────────────────────────────────────────────

    [ObservableProperty] private SystemInfo? _systemInfo;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isApplying;
    [ObservableProperty] private string _statusText = "Hazır";
    [ObservableProperty] private string _selectedCategory = "Performans";
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private bool _isDarkTheme = true;
    [ObservableProperty] private string _scanMessage = "";
    [ObservableProperty] private string _resultSummary = "";
    [ObservableProperty] private bool _showResultPanel;
    [ObservableProperty] private bool _requiresRestart;
    // Dil etiketleri — XAML bu property'lere bind olur
    [ObservableProperty] private string _lblWhatItDoes = LanguageManager.Get("lbl_what_it_does");
    [ObservableProperty] private string _lblBenefit = LanguageManager.Get("lbl_benefit");
    [ObservableProperty] private string _lblRisk = LanguageManager.Get("lbl_risk");
    [ObservableProperty] private string _lblReversible = LanguageManager.Get("lbl_reversible");
    [ObservableProperty] private string _lblYes = LanguageManager.Get("lbl_yes");
    [ObservableProperty] private string _lblNo = LanguageManager.Get("lbl_no");
    [ObservableProperty] private string _recommendedLabel = LanguageManager.Get("lbl_recommended");
    [ObservableProperty] private string _appliedLabel = LanguageManager.Get("lbl_applied");

    public ObservableCollection<string> LogLines { get; } = new();
    public ObservableCollection<OptimizationItemViewModel> CurrentItems { get; } = new();

    public ObservableCollection<CategoryMenuItem> Categories { get; } = new()
    {
        new("cat_performance", "⚡", OptimizationCategory.Performance),
        new("cat_display",     "🖥️", OptimizationCategory.Display),
        new("cat_bloatware",   "🗑️", OptimizationCategory.Bloatware),
        new("cat_startup",     "🚀", OptimizationCategory.Startup),
        new("cat_privacy",     "🔒", OptimizationCategory.Privacy),
        new("cat_cleanup",     "💾", OptimizationCategory.Cleanup),
        new("cat_network",     "🌐", OptimizationCategory.Network),
        new("cat_windows11",   "🪟", OptimizationCategory.Windows11),
    };

    // ── KOMUTLAR ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ScanSystemAsync()
    {
        IsScanning = true;
        StatusText = LanguageManager.Get("status_scanning");
        LogLines.Clear();

        _scanner.ScanProgress += msg => ScanMessage = msg;

        try
        {
            SystemInfo = await _scanner.ScanAsync();
            StatusText = LanguageManager.Get("status_scan_done");
            _logger.Success("Sistem taraması tamamlandı.");
            LoadCategory(SelectedCategory);
        }
        catch (Exception ex)
        {
            StatusText = "Tarama hatası";
            _logger.Error($"Tarama hatası: {ex.Message}");
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void SelectCategory(string categoryName)
    {
        SelectedCategory = categoryName;
        LoadCategory(categoryName);
    }

    [RelayCommand]
    private async Task ApplySelectedAsync()
    {
        var selected = CurrentItems
            .Where(i => i.IsSelected && i.IsEnabled)
            .ToList();

        if (selected.Count == 0)
        {
            StatusText = "Hiçbir optimizasyon seçilmedi.";
            return;
        }

        IsApplying = true;
        ShowResultPanel = false;
        RequiresRestart = false;
        StatusText = $"{selected.Count} optimizasyon uygulanıyor...";

        _logger.Info("Sistem geri yükleme noktası oluşturuluyor...");
        var restoreProgress = new Progress<string>(_logger.Info);
        await _restoreManager.CreateRestorePointAsync(restoreProgress);

        int success = 0, failed = 0;

        for (int i = 0; i < selected.Count; i++)
        {
            var item = selected[i];
            ProgressValue = (int)((i + 1.0) / selected.Count * 100);
            StatusText = $"Uygulanıyor: {item.Name}";

            var appProgress = new Progress<string>(_logger.Info);

            try
            {
                var opt = _optimizations.FirstOrDefault(o => o.Id == item.OptimizationId);

                if (opt == null)
                {
                    _logger.Warning($"{item.Name} bulunamadı, atlanıyor.");
                    failed++;
                    continue;
                }

                var result = await opt.ApplyAsync(appProgress);

                if (result.Success)
                {
                    success++;
                    item.IsApplied = true;
                    _logger.Success($"✅ {item.Name}: {result.Message}");
                    if (result.RequiresRestart) RequiresRestart = true;
                }
                else
                {
                    failed++;
                    _logger.Error($"❌ {item.Name}: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.Error($"❌ {item.Name}: {ex.Message}");
            }
        }

        ResultSummary = $"✅ Başarılı: {success}   ❌ Başarısız: {failed}";
        if (RequiresRestart)
            ResultSummary += "\n⚠️ Bazı değişiklikler için yeniden başlatma gerekiyor.";

        ShowResultPanel = true;
        IsApplying = false;
        ProgressValue = 0;
        StatusText = LanguageManager.Get("status_done");
    }

    [RelayCommand]
    private void SelectRecommended()
    {
        foreach (var item in CurrentItems)
            item.IsSelected = item.IsRecommended && item.IsEnabled;
    }

    [RelayCommand]
    private void ToggleSelectAll()
    {
        bool anyUnselected = CurrentItems.Any(i => !i.IsSelected && i.IsEnabled);
        foreach (var item in CurrentItems)
            if (item.IsEnabled)
                item.IsSelected = anyUnselected;
    }

    [RelayCommand]
    private async Task SaveLogAsync()
    {
        string path = await _logger.SaveToDesktopAsync();
        StatusText = $"Log kaydedildi: {path}";
    }

    // ── YARDIMCI ─────────────────────────────────────────────────────────────

    private void LoadCategory(string categoryName)
    {
        CurrentItems.Clear();

        var category = Categories
            .FirstOrDefault(c => c.Name == categoryName)?.Category
            ?? OptimizationCategory.Performance;

        var items = _optimizations
            .Where(o => o.Category == category)
            .Where(o => SystemInfo == null || o.IsApplicable(SystemInfo));

        foreach (var opt in items)
        {
            // ID'den dil anahtarı üret: "perf_powerplan" → "opt_powerplan"
            string key = "opt_" + opt.Id.Split('_').Last();

            CurrentItems.Add(new OptimizationItemViewModel
            {
                OptimizationId = opt.Id,
                Name = LanguageManager.Get(key + "_name"),
                ShortDescription = LanguageManager.Get(key + "_short"),
                WhatItDoes = LanguageManager.Get(key + "_what"),
                Benefit = LanguageManager.Get(key + "_benefit"),
                RiskDescription = LanguageManager.Get(key + "_risk"),
                IsReversible = opt.IsReversible,
                IsRecommended = opt.IsRecommended,
                IsSelected = opt.IsRecommended,
                Risk = opt.RiskLevel,
                IsEnabled = true
            });
        }
    }

    // Dil değişince çağrılır
    public void RefreshLanguage()
    {
        StatusText = LanguageManager.Get("status_ready");

        // Etiketleri güncelle
        LblWhatItDoes = LanguageManager.Get("lbl_what_it_does");
        LblBenefit = LanguageManager.Get("lbl_benefit");
        LblRisk = LanguageManager.Get("lbl_risk");
        LblReversible = LanguageManager.Get("lbl_reversible");
        LblYes = LanguageManager.Get("lbl_yes");
        LblNo = LanguageManager.Get("lbl_no");
        RecommendedLabel = LanguageManager.Get("lbl_recommended");
        AppliedLabel = LanguageManager.Get("lbl_applied");

        // Kategori isimlerini güncelle
        var selectedKey = Categories
            .FirstOrDefault(c => c.Name == SelectedCategory)?.Key
            ?? "cat_performance";

        foreach (var cat in Categories)
            cat.Name = LanguageManager.Get(cat.Key);

        SelectedCategory = LanguageManager.Get(selectedKey);
        LoadCategory(SelectedCategory);
        OnPropertyChanged(nameof(Categories));
    }
}

// ── YARDIMCI SINIFLAR ────────────────────────────────────────────────────────

public partial class OptimizationItemViewModel : ObservableObject
{
    public string OptimizationId { get; set; } = "";
    public string Name { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string WhatItDoes { get; set; } = "";
    public string Benefit { get; set; } = "";
    public string RiskDescription { get; set; } = "";
    public bool IsReversible { get; set; }
    public bool IsRecommended { get; set; }
    public OptimizationRisk Risk { get; set; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isApplied;
    [ObservableProperty] private bool _isEnabled = true;
}

public class CategoryMenuItem : System.ComponentModel.INotifyPropertyChanged
{
    public string Key { get; set; } = "";
    public string Icon { get; set; } = "";
    public OptimizationCategory Category { get; set; }

    private string _name = "";
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            PropertyChanged?.Invoke(this,
                new System.ComponentModel.PropertyChangedEventArgs(nameof(Name)));
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public CategoryMenuItem(string key, string icon, OptimizationCategory category)
    {
        Key = key;
        Name = LanguageManager.Get(key);
        Icon = icon;
        Category = category;
    }
}