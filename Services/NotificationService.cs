using System.Media;
using AutoClickSelector.Core;
using AutoClickSelector.Models;

namespace AutoClickSelector.Services;

internal static class NotificationService
{
    private static NotifyIcon? trayIcon;

    public static void Attach(NotifyIcon icon) => trayIcon = icon;

    public static void Notify(string message) =>
        Show("Auto Click Selector", message, ToolTipIcon.Info, playSound: false);

    public static void Info(string title, string message) => Show(title, message, ToolTipIcon.Info, playSound: false);

    public static void Warning(string title, string message) => Show(title, message, ToolTipIcon.Warning, playSound: true);

    public static void Error(string title, string message) => Show(title, message, ToolTipIcon.Error, playSound: true);

    private static void Show(string title, string message, ToolTipIcon kind, bool playSound)
    {
        AppSettings settings = SettingsStore.Current;

        if (!settings.ShowNotifications)
            return;

        try
        {
            trayIcon?.ShowBalloonTip(6000, title, message, kind);
        }
        catch (Exception exception) when (exception is ObjectDisposedException or InvalidOperationException)
        {
        }

        if (playSound && settings.PlaySound)
            SystemSounds.Exclamation.Play();
    }
}
