using AutoClickSelector.UI.Forms;

namespace AutoClickSelector;

internal static class Program
{
    /// <summary>Điểm khởi chạy ứng dụng.</summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
