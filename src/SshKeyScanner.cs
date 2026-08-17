namespace SshKeyManager;

internal static class SshKeyScanner
{
    // not keys: ssh config and host files
    private static readonly HashSet<string> SkipNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "config", "known_hosts", "known_hosts.old", "authorized_keys",
        "environment", "rc",
    };

    public static string SshDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");

    // --- scan ~/.ssh ---

    public static IReadOnlyList<SshKeyInfo> Scan()
    {
        var dir = SshDirectory;
        if (!Directory.Exists(dir))
        {
            return [];
        }

        var agentFingerprints = LoadAgentFingerprints();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<SshKeyInfo>();

        foreach (var file in Directory.EnumerateFiles(dir))
        {
            var name = Path.GetFileName(file);
            if (SkipNames.Contains(name) || name.EndsWith(".pub", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!LooksLikePrivateKey(file))
            {
                continue;
            }

            names.Add(name);
            result.Add(BuildInfo(file, agentFingerprints));
        }

        foreach (var pub in Directory.EnumerateFiles(dir, "*.pub"))
        {
            var stem = Path.GetFileNameWithoutExtension(pub);
            if (names.Contains(stem))
            {
                continue;
            }

            var privatePath = Path.Combine(dir, stem);
            result.Add(BuildInfo(File.Exists(privatePath) ? privatePath : pub, agentFingerprints, pubOnly: !File.Exists(privatePath)));
        }

        return result
            .OrderBy(k => k.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // --- rename / delete ---

    public static string? Rename(SshKeyInfo key, string newName)
    {
        newName = newName.Trim();
        if (newName.EndsWith(".pub", StringComparison.OrdinalIgnoreCase))
        {
            newName = newName[..^4];
        }

        if (string.IsNullOrWhiteSpace(newName) || newName is "." or "..")
        {
            return Lang.EmptyName;
        }

        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || newName.Contains('\\') || newName.Contains('/'))
        {
            return Lang.InvalidNameChars;
        }

        if (SkipNames.Contains(newName) || SkipNames.Contains(newName + ".pub"))
        {
            return Lang.ReservedName;
        }

        if (newName.Equals(key.Name, StringComparison.Ordinal))
        {
            return null;
        }

        var dir = SshDirectory;
        var newPrivate = Path.Combine(dir, newName);
        var newPublic = newPrivate + ".pub";
        var caseOnly = newName.Equals(key.Name, StringComparison.OrdinalIgnoreCase);

        var moves = new List<(string From, string To)>();
        if (File.Exists(key.PrivatePath))
        {
            moves.Add((key.PrivatePath, newPrivate));
        }

        if (File.Exists(key.PublicPath))
        {
            moves.Add((key.PublicPath, newPublic));
        }

        if (moves.Count == 0)
        {
            return Lang.KeyFilesNotFound;
        }

        if (!caseOnly)
        {
            foreach (var (_, to) in moves)
            {
                if (File.Exists(to))
                {
                    return Lang.FileExists(Path.GetFileName(to));
                }
            }
        }

        var done = new List<(string From, string To)>();
        try
        {
            foreach (var (from, to) in moves)
            {
                MoveFile(from, to);
                done.Add((from, to));
            }
        }
        catch (Exception ex)
        {
            for (var i = done.Count - 1; i >= 0; i--)
            {
                try { MoveFile(done[i].To, done[i].From); } catch { /* rollback best-effort */ }
            }

            return Lang.RenameFailed(ex.Message);
        }

        return null;
    }

    public static string? Delete(SshKeyInfo key)
    {
        var files = new List<string>();
        if (File.Exists(key.PrivatePath))
        {
            files.Add(key.PrivatePath);
        }

        if (File.Exists(key.PublicPath))
        {
            files.Add(key.PublicPath);
        }

        if (files.Count == 0)
        {
            return Lang.KeyFilesNotFound;
        }

        if (File.Exists(key.PrivatePath) && key.InAgent)
        {
            SshAgentService.RemoveIdentity(key.PrivatePath);
        }

        try
        {
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            return Lang.DeleteFailed(ex.Message);
        }

        return null;
    }

    private static void MoveFile(string from, string to)
    {
        if (from.Equals(to, StringComparison.Ordinal))
        {
            return;
        }

        if (from.Equals(to, StringComparison.OrdinalIgnoreCase))
        {
            var temp = from + ".rename-tmp";
            File.Move(from, temp);
            File.Move(temp, to);
            return;
        }

        File.Move(from, to);
    }

    // --- one key: type, fingerprint, passphrase, agent ---

    private static SshKeyInfo BuildInfo(string path, HashSet<string> agentFingerprints, bool pubOnly = false)
    {
        var name = pubOnly ? Path.GetFileNameWithoutExtension(path) : Path.GetFileName(path);
        var privatePath = pubOnly ? "" : path;
        var publicPath = pubOnly ? path : path + ".pub";
        var hasPub = File.Exists(publicPath);
        var probePath = hasPub ? publicPath : (pubOnly ? path : privatePath);

        var (bits, fingerprint, comment, type) = ReadFingerprint(probePath);
        if (type == "—" && hasPub)
        {
            type = GuessTypeFromPub(publicPath);
        }

        var hasPassphrase = !pubOnly && !string.IsNullOrEmpty(privatePath) && IsEncrypted(privatePath);
        var protection = pubOnly || string.IsNullOrEmpty(privatePath)
            ? "—"
            : hasPassphrase ? Lang.ProtectionPassword : Lang.ProtectionNone;

        var inAgent = fingerprint.StartsWith("SHA256:", StringComparison.Ordinal)
                      && agentFingerprints.Contains(fingerprint);
        var state = pubOnly ? Lang.StateNoPrivate
            : !hasPub ? Lang.StateNoPub
            : type.Equals("RSA", StringComparison.OrdinalIgnoreCase) && int.TryParse(bits, out var n) && n <= 2048 ? Lang.StateWeakRsa
            : Lang.StateOk;

        var changedSource = File.Exists(privatePath) ? privatePath : publicPath;
        var changed = File.Exists(changedSource)
            ? File.GetLastWriteTime(changedSource).ToString(Lang.DateFormat)
            : "—";

        return new SshKeyInfo(
            Name: name,
            Type: type,
            Bits: type.Equals("ED25519", StringComparison.OrdinalIgnoreCase) ? "—" : bits,
            Fingerprint: fingerprint,
            Comment: string.IsNullOrWhiteSpace(comment) ? "—" : comment,
            PrivatePath: string.IsNullOrEmpty(privatePath) ? "—" : privatePath,
            PublicPath: hasPub ? publicPath : "—",
            Protection: protection,
            Agent: inAgent ? Lang.AgentLoaded : Lang.AgentNotLoaded,
            AgentMark: inAgent ? Lang.AgentYes : Lang.AgentNo,
            State: state,
            Changed: changed,
            HasPassphrase: hasPassphrase,
            InAgent: inAgent);
    }

    private static bool LooksLikePrivateKey(string path)
    {
        try
        {
            using var reader = new StreamReader(path);
            var line = reader.ReadLine() ?? "";
            return line.StartsWith("-----BEGIN ", StringComparison.Ordinal)
                   && line.Contains("PRIVATE KEY", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsEncrypted(string privatePath)
    {
        var keygen = FindTool("ssh-keygen.exe");
        if (keygen is null)
        {
            return false;
        }

        var (exit, _, _) = Run(keygen, $"-y -P \"\" -f \"{privatePath}\"");
        return exit != 0;
    }

    private static (string Bits, string Fingerprint, string Comment, string Type) ReadFingerprint(string path)
    {
        var keygen = FindTool("ssh-keygen.exe");
        if (keygen is null || !File.Exists(path))
        {
            return ("—", "—", "—", "—");
        }

        var (_, stdout, _) = Run(keygen, $"-lf \"{path}\"");
        var line = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        // 256 SHA256:xxxx comment (ED25519)
        var open = line.LastIndexOf(" (", StringComparison.Ordinal);
        var close = line.LastIndexOf(')');
        var type = open >= 0 && close > open ? line[(open + 2)..close] : "—";
        var body = open >= 0 ? line[..open] : line;
        var parts = body.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        var bits = parts.Length > 0 ? parts[0] : "—";
        var fp = parts.Length > 1 ? parts[1] : "—";
        var comment = parts.Length > 2 ? parts[2].Trim() : "";
        return (bits, fp, comment, type);
    }

    private static string GuessTypeFromPub(string publicPath)
    {
        try
        {
            var first = File.ReadLines(publicPath).FirstOrDefault() ?? "";
            if (first.StartsWith("ssh-ed25519", StringComparison.Ordinal)) return "ED25519";
            if (first.StartsWith("ssh-rsa", StringComparison.Ordinal)) return "RSA";
            if (first.StartsWith("ecdsa-sha2", StringComparison.Ordinal)) return "ECDSA";
            if (first.StartsWith("ssh-dss", StringComparison.Ordinal)) return "DSA";
        }
        catch
        {
            // ignore
        }

        return "—";
    }

    private static HashSet<string> LoadAgentFingerprints()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var sshAdd = FindTool("ssh-add.exe");
        if (sshAdd is null)
        {
            return result;
        }

        var (exit, stdout, _) = Run(sshAdd, "-l -E sha256");
        if (exit != 0)
        {
            return result;
        }

        foreach (var line in stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var fp = parts.FirstOrDefault(p => p.StartsWith("SHA256:", StringComparison.Ordinal));
            if (fp is not null)
            {
                result.Add(fp);
            }
        }

        return result;
    }

    // --- find ssh-keygen / ssh-add on PATH or in Windows OpenSSH ---

    internal static string? FindTool(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\OpenSSH", fileName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Git\usr\bin", fileName),
        };
        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        var envPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in envPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var full = Path.Combine(dir.Trim(), fileName);
            if (File.Exists(full))
            {
                return full;
            }
        }

        return null;
    }

    private static (int Exit, string StdOut, string StdErr) Run(string fileName, string arguments)
    {
        var start = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var process = System.Diagnostics.Process.Start(start);
        if (process is null)
        {
            return (-1, "", "failed to start");
        }

        process.StandardInput.Close();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(8000))
        {
            try { process.Kill(true); } catch { /* ignore */ }
            return (-1, stdout, stderr);
        }
        return (process.ExitCode, stdout.Trim(), stderr.Trim());
    }

    // --- ssh-keygen: change passphrase / create ---

    internal static string? ChangePassphrase(string privatePath, string current, string next)
    {
        var keygen = FindTool("ssh-keygen.exe");
        if (keygen is null)
        {
            return Lang.SshKeygenMissing;
        }

        if (!File.Exists(privatePath))
        {
            return Lang.PrivateKeyMissing;
        }

        var start = new System.Diagnostics.ProcessStartInfo
        {
            FileName = keygen,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add("-p");
        start.ArgumentList.Add("-f");
        start.ArgumentList.Add(privatePath);
        start.ArgumentList.Add("-P");
        start.ArgumentList.Add(current);
        start.ArgumentList.Add("-N");
        start.ArgumentList.Add(next);

        using var process = System.Diagnostics.Process.Start(start);
        if (process is null)
        {
            return Lang.SshKeygenStartFailed;
        }

        process.StandardInput.Close();
        var stderr = process.StandardError.ReadToEnd();
        var stdout = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(15000))
        {
            try { process.Kill(true); } catch { /* ignore */ }
            return Lang.SshKeygenTimeout;
        }

        if (process.ExitCode == 0)
        {
            return null;
        }

        var text = (stderr + " " + stdout).Trim();
        if (text.Contains("passphrase", StringComparison.OrdinalIgnoreCase)
            || text.Contains("incorrect", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Bad", StringComparison.OrdinalIgnoreCase)
            || text.Contains("wrong", StringComparison.OrdinalIgnoreCase)
            || text.Contains("load key", StringComparison.OrdinalIgnoreCase))
        {
            return Lang.WrongCurrentPassword;
        }

        return string.IsNullOrWhiteSpace(text) ? Lang.ChangePasswordFailed : text;
    }

    internal static string? CreateKey(string type, string privatePath, string comment, string passphrase)
    {
        var keygen = FindTool("ssh-keygen.exe");
        if (keygen is null)
        {
            return Lang.SshKeygenMissing;
        }

        if (File.Exists(privatePath) || File.Exists(privatePath + ".pub"))
        {
            return Lang.FileExists(Path.GetFileName(privatePath));
        }

        var dir = Path.GetDirectoryName(privatePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var start = new System.Diagnostics.ProcessStartInfo
        {
            FileName = keygen,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add("-q");
        if (type.StartsWith("RSA", StringComparison.OrdinalIgnoreCase))
        {
            start.ArgumentList.Add("-t");
            start.ArgumentList.Add("rsa");
            start.ArgumentList.Add("-b");
            start.ArgumentList.Add("4096");
        }
        else
        {
            start.ArgumentList.Add("-t");
            start.ArgumentList.Add("ed25519");
        }

        start.ArgumentList.Add("-f");
        start.ArgumentList.Add(privatePath);
        start.ArgumentList.Add("-C");
        start.ArgumentList.Add(comment);
        start.ArgumentList.Add("-N");
        start.ArgumentList.Add(passphrase);

        using var process = System.Diagnostics.Process.Start(start);
        if (process is null)
        {
            return Lang.SshKeygenStartFailed;
        }

        process.StandardInput.Close();
        var stderr = process.StandardError.ReadToEnd();
        var stdout = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(20000))
        {
            try { process.Kill(true); } catch { /* ignore */ }
            return Lang.SshKeygenTimeout;
        }

        if (process.ExitCode == 0 && File.Exists(privatePath))
        {
            return null;
        }

        var text = (stderr + " " + stdout).Trim();
        return string.IsNullOrWhiteSpace(text) ? Lang.CreateKeyFailed : text;
    }
}

// display strings + flags used for comparisons (not localized text)
internal sealed record SshKeyInfo(
    string Name,
    string Type,
    string Bits,
    string Fingerprint,
    string Comment,
    string PrivatePath,
    string PublicPath,
    string Protection,
    string Agent,
    string AgentMark,
    string State,
    string Changed,
    bool HasPassphrase,
    bool InAgent);
