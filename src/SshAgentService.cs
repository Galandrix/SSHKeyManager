using System.ComponentModel;

namespace SshKeyManager;

internal enum SshAgentStatus
{
    Missing,
    Stopped,
    Running,
}

internal static class SshAgentService
{
    // --- Windows service ssh-agent via sc.exe ---

    public static SshAgentStatus GetStatus()
    {
        var (exit, stdout, stderr) = RunSc("query ssh-agent", elevate: false);
        var text = stdout + "\n" + stderr;
        if (exit == 1060 || text.Contains("1060", StringComparison.Ordinal))
        {
            return SshAgentStatus.Missing;
        }

        if (text.Contains("RUNNING", StringComparison.OrdinalIgnoreCase)
            || text.Contains("START_PENDING", StringComparison.OrdinalIgnoreCase))
        {
            return SshAgentStatus.Running;
        }

        return exit == 0 ? SshAgentStatus.Stopped : SshAgentStatus.Missing;
    }

    public static string? Toggle()
    {
        return GetStatus() switch
        {
            SshAgentStatus.Missing => Lang.AgentServiceMissing,
            SshAgentStatus.Running => Stop(),
            _ => Start(),
        };
    }

    private static string? Start()
    {
        var (_, query, _) = RunSc("qc ssh-agent", elevate: false);
        if (query.Contains("DISABLED", StringComparison.OrdinalIgnoreCase))
        {
            var configError = Exec("config ssh-agent start= demand");
            if (configError is not null)
            {
                return configError;
            }
        }

        var startError = Exec("start ssh-agent");
        if (startError is not null)
        {
            return startError;
        }

        return WaitUntil(SshAgentStatus.Running, Lang.AgentDidNotStart);
    }

    private static string? Stop()
    {
        var stopError = Exec("stop ssh-agent");
        if (stopError is not null)
        {
            return stopError;
        }

        return WaitUntil(SshAgentStatus.Stopped, Lang.AgentDidNotStop);
    }

    private static string? WaitUntil(SshAgentStatus expected, string timeoutMessage)
    {
        var until = DateTime.UtcNow.AddSeconds(12);
        while (DateTime.UtcNow < until)
        {
            if (GetStatus() == expected)
            {
                return null;
            }

            Thread.Sleep(250);
        }

        return timeoutMessage;
    }

    private static string? Exec(string arguments)
    {
        var (exit, _, stderr) = RunSc(arguments, elevate: false);
        if (exit == 0 || IsAlreadyOk(arguments, exit))
        {
            return null;
        }

        if (exit is 5 or 1058)
        {
            try
            {
                (exit, _, stderr) = RunSc(arguments, elevate: true);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                return Lang.UacCancelled;
            }

            if (exit == 0 || IsAlreadyOk(arguments, exit))
            {
                return null;
            }
        }

        return string.IsNullOrWhiteSpace(stderr)
            ? Lang.ScFailed(exit)
            : stderr;
    }

    private static bool IsAlreadyOk(string arguments, int exit) =>
        (arguments.StartsWith("start ", StringComparison.OrdinalIgnoreCase) && exit == 1056)
        || (arguments.StartsWith("stop ", StringComparison.OrdinalIgnoreCase) && exit == 1062);

    private static (int Exit, string StdOut, string StdErr) RunSc(string arguments, bool elevate)
    {
        var sc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sc.exe");
        var start = new System.Diagnostics.ProcessStartInfo
        {
            FileName = sc,
            Arguments = arguments,
            UseShellExecute = elevate,
            Verb = elevate ? "runas" : string.Empty,
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            CreateNoWindow = !elevate,
            RedirectStandardOutput = !elevate,
            RedirectStandardError = !elevate,
        };
        using var process = System.Diagnostics.Process.Start(start);
        if (process is null)
        {
            return (-1, "", Lang.ScStartFailed);
        }

        var stdout = elevate ? "" : process.StandardOutput.ReadToEnd();
        var stderr = elevate ? "" : process.StandardError.ReadToEnd();
        if (!process.WaitForExit(20000))
        {
            try { process.Kill(true); } catch { /* ignore */ }
            return (-1, stdout, Lang.ScTimeout);
        }

        return (process.ExitCode, stdout.Trim(), stderr.Trim());
    }

    // --- ssh-add: load / unload a key ---

    public static string? AddIdentity(string privatePath, string? passphrase)
    {
        if (!File.Exists(privatePath))
        {
            return Lang.PrivateKeyMissing;
        }

        if (GetStatus() != SshAgentStatus.Running)
        {
            return Lang.StartAgentFirst;
        }

        var sshAdd = SshKeyScanner.FindTool("ssh-add.exe");
        if (sshAdd is null)
        {
            return Lang.SshAddMissing;
        }

        var (exit, _, stderr) = RunSshAdd(sshAdd, Quote(privatePath), passphrase);
        if (exit == 0)
        {
            return null;
        }

        return FriendlySshAddError(stderr, adding: true);
    }

    public static string? RemoveIdentity(string privatePath)
    {
        if (GetStatus() != SshAgentStatus.Running)
        {
            return Lang.StartAgentFirst;
        }

        var sshAdd = SshKeyScanner.FindTool("ssh-add.exe");
        if (sshAdd is null)
        {
            return Lang.SshAddMissing;
        }

        var (exit, _, stderr) = RunSshAdd(sshAdd, "-d " + Quote(privatePath), passphrase: null);
        if (exit == 0)
        {
            return null;
        }

        return FriendlySshAddError(stderr, adding: false);
    }

    private static string Quote(string path) => "\"" + path + "\"";

    private static string FriendlySshAddError(string stderr, bool adding)
    {
        if (stderr.Contains("Could not open a connection", StringComparison.OrdinalIgnoreCase)
            || stderr.Contains("agent", StringComparison.OrdinalIgnoreCase)
               && stderr.Contains("connect", StringComparison.OrdinalIgnoreCase))
        {
            return Lang.AgentNotConnected;
        }

        if (stderr.Contains("passphrase", StringComparison.OrdinalIgnoreCase)
            || stderr.Contains("incorrect", StringComparison.OrdinalIgnoreCase)
            || stderr.Contains("Bad", StringComparison.OrdinalIgnoreCase))
        {
            return Lang.WrongKeyPassword;
        }

        if (string.IsNullOrWhiteSpace(stderr))
        {
            return adding ? Lang.AddToAgentFailed : Lang.RemoveFromAgentFailed;
        }

        return stderr;
    }

    private static (int Exit, string StdOut, string StdErr) RunSshAdd(string sshAdd, string arguments, string? passphrase)
    {
        var start = new System.Diagnostics.ProcessStartInfo
        {
            FileName = sshAdd,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.EnvironmentVariables["DISPLAY"] = "1";
        if (passphrase is not null)
        {
            start.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
            start.EnvironmentVariables["SSH_ASKPASS"] = EnsureAskPass();
            start.EnvironmentVariables["SSH_ASKPASS_PASSWORD"] = passphrase;
        }

        using var process = System.Diagnostics.Process.Start(start);
        if (process is null)
        {
            return (-1, "", Lang.SshAddStartFailed);
        }

        process.StandardInput.Close();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(20000))
        {
            try { process.Kill(true); } catch { /* ignore */ }
            return (-1, stdout, Lang.SshAddTimeout);
        }

        return (process.ExitCode, stdout.Trim(), stderr.Trim());
    }

    private static string EnsureAskPass()
    {
        var path = Path.Combine(Path.GetTempPath(), "sshkeymanager-askpass.cmd");
        File.WriteAllText(path,
            "@echo off\r\n" +
            "powershell -NoProfile -WindowStyle Hidden -Command \"[Console]::Out.Write($env:SSH_ASKPASS_PASSWORD)\"\r\n");
        return path;
    }
}
