namespace SshKeyManager;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        PasswordOptions.Load(); // language + password generator from %AppData%
        Application.Run(new MainForm());
    }
}
