using System.Windows.Forms;

namespace DVDify;

static class Program
{
    [STAThread]
    static void Main()
    {
        DebugLogger.Log("=== DVDify Application Starting ===");
        DebugLogger.Log($"OS Version: {Environment.OSVersion}");
        DebugLogger.Log($".NET Version: {Environment.Version}");
        DebugLogger.Log($"Working Directory: {Environment.CurrentDirectory}");
        
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        try
        {
            using (var context = new TrayApplicationContext())
            {
                DebugLogger.Log("Application context created, entering message loop");
                Application.Run(context);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"FATAL ERROR: {ex.GetType().Name}: {ex.Message}");
            DebugLogger.Log($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            DebugLogger.Log("=== DVDify Application Shutting Down ===");
        }
    }
}
