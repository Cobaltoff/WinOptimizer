using System.Windows;
using WinOptimizer.ViewModels;

namespace WinOptimizer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;

        // Dil değişince tüm UI'ı güncelle
        LanguageManager.LanguageChanged += () =>
        {
            Dispatcher.Invoke(() =>
            {
                UpdateUITexts();
                if (DataContext is MainViewModel vm)
                    vm.RefreshLanguage();
            });
        };
        Loaded += (_, _) => UpdateUITexts();
        Loaded += async (_, _) =>
            await viewModel.ScanSystemCommand.ExecuteAsync(null);
    }

    private void UpdateUITexts()
    {
        BtnRescan.Content = LanguageManager.Get("btn_rescan");
        BtnAbout.Content = LanguageManager.Get("btn_about");
        BtnSelectRecommended.Content = LanguageManager.Get("btn_select_recommended");
        BtnToggleAll.Content = LanguageManager.Get("btn_toggle_all");
        BtnSelectRecommended2.Content = LanguageManager.Get("btn_select_recommended");
        BtnApply.Content = LanguageManager.Get("btn_apply");
        BtnRestore.Content = LanguageManager.Get("btn_restore");
        BtnSaveLog.Content = LanguageManager.Get("btn_save_log");
        LblSystemStatus.Text = LanguageManager.Get("lbl_system_status");
        LblCategories.Text = LanguageManager.Get("lbl_categories");
        LblScanning.Text = LanguageManager.Get("status_scanning");
        LblRestartWarning.Text = LanguageManager.Get("result_restart");
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            LanguageManager.Get("about_text"),
            LanguageManager.Get("about_title"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            LanguageManager.Get("restore_text"),
            LanguageManager.Get("restore_title"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // Dil butonları
    private void LangTR_Click(object sender, RoutedEventArgs e)
        => LanguageManager.SetLanguage("tr");

    private void LangEN_Click(object sender, RoutedEventArgs e)
        => LanguageManager.SetLanguage("en");
}