using System.Windows.Forms;

namespace DVDify;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        using (var context = new TrayApplicationContext())
        {
            Application.Run(context);
        }
    }
}
