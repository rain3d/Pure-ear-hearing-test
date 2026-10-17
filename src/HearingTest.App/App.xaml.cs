// ============================================================================
// 純音聽力測驗系統 (Pure-Tone HearingTest)
// Author: rain3d (https://github.com/rain3d)
// License: Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)
// 免費提供個人自主監測與非營利學術研究使用；嚴格禁止任何商業營利與付費銷售用途。
// ============================================================================

using System.Configuration;
using System.Data;
using System.Windows;

namespace HearingTest.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HearingTest.App.Localization.LocalizationService.Initialize();
        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"應用程式發生未預期錯誤：\n{args.Exception.Message}\n\n詳細追蹤：\n{args.Exception}", "應用程式錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}

