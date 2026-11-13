using AutoClickSelector.UI.Forms;

namespace AutoClickSelector;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Contains("--selftest"))
        {
            string stepFile = Path.Combine(AppContext.BaseDirectory, "selftest-step.txt");
            try
            {
                File.WriteAllText(stepFile, "start\n");

                using (var form = new MainForm())
                {
                    File.AppendAllText(stepFile, "constructed\n");

                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-32000, -32000);
                    form.Show();

                    for (int i = 0; i < 6; i++)
                    {
                        Application.DoEvents();
                        Thread.Sleep(40);
                    }

                    form.Hide();
                    File.AppendAllText(stepFile, "shown\n");
                }

                File.AppendAllText(stepFile, "disposed\n");
                Environment.ExitCode = 0;
            }
            catch (Exception exception)
            {
                Environment.ExitCode = 1;
                try
                {
                    File.AppendAllText(stepFile, "FAILED: " + exception + "\n");
                }
                catch (Exception)
                {
                }
            }

            return;
        }

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => ReportFatal(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            ReportFatal(args.ExceptionObject as Exception);

        Application.Run(new MainForm());
    }

    private static void ReportFatal(Exception? exception)
    {
        string detail = exception?.GetBaseException().Message
            ?? AutoClickSelector.Services.Localization.T("Không có thông tin chi tiết.");
        string text = AutoClickSelector.Services.Localization.F(
            "Ứng dụng gặp lỗi không mong đợi:\n\n{0}\n\nNếu lỗi lặp lại, hãy gửi nội dung này cho nhà phát triển.",
            detail);

        try
        {
            MessageBox.Show(text, "Auto Click Selector", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception)
        {
        }
    }
}
