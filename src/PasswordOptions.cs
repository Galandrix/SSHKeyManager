using System.Security.Cryptography;
using System.Text.Json;

namespace SshKeyManager;

internal static class PasswordOptions
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    // --- generator + UI language (settings.json) ---
    public static int Length { get; set; } = 16;
    public static bool Lower { get; set; } = true;
    public static bool Upper { get; set; } = true;
    public static bool Digits { get; set; } = true;
    public static bool Special { get; set; } = true;
    public static string SpecialChars { get; set; } = "!@#$%_-";
    public static AppLanguage Language { get; set; } = AppLanguage.En;

    public static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SshKeyManager",
            "settings.json");

    public static string Alphabet
    {
        get
        {
            var chars = new System.Text.StringBuilder();
            if (Lower) chars.Append("abcdefghijklmnopqrstuvwxyz");
            if (Upper) chars.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            if (Digits) chars.Append("0123456789");
            if (Special) chars.Append(SpecialChars);
            return chars.ToString();
        }
    }

    // --- load / save / generate ---

    public static void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return;
            }

            var dto = JsonSerializer.Deserialize<Dto>(File.ReadAllText(SettingsPath));
            if (dto is null)
            {
                return;
            }

            Length = Math.Clamp(dto.Length, 4, 128);
            Lower = dto.Lower;
            Upper = dto.Upper;
            Digits = dto.Digits;
            Special = dto.Special;
            Language = ParseLanguage(dto.Language);
            if (!string.IsNullOrWhiteSpace(dto.SpecialChars))
            {
                SpecialChars = dto.SpecialChars;
            }
        }
        catch
        {
            // keep defaults
        }
    }

    public static string? Save()
    {
        if (Alphabet.Length == 0)
        {
            return Lang.NeedCharset;
        }

        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(new Dto
            {
                Length = Length,
                Lower = Lower,
                Upper = Upper,
                Digits = Digits,
                Special = Special,
                SpecialChars = SpecialChars,
                Language = Language == AppLanguage.Ru ? "ru" : "en",
            }, Json);
            File.WriteAllText(SettingsPath, json);
            return null;
        }
        catch (Exception ex)
        {
            return Lang.SaveSettingsFailed(ex.Message);
        }
    }

    public static string Generate()
    {
        var alphabet = Alphabet;
        var length = Math.Clamp(Length, 4, 128);
        if (alphabet.Length == 0)
        {
            return "";
        }

        return string.Create(length, alphabet, static (span, chars) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            }
        });
    }

    private sealed class Dto
    {
        public int Length { get; set; } = 16;
        public bool Lower { get; set; } = true;
        public bool Upper { get; set; } = true;
        public bool Digits { get; set; } = true;
        public bool Special { get; set; } = true;
        public string SpecialChars { get; set; } = "!@#$%_-";
        public string Language { get; set; } = "en";
    }

    public static AppLanguage ParseLanguage(string? value) =>
        value is "ru" or "Ru" or "RU" or "russian" or "Russian" or "ru-RU"
            ? AppLanguage.Ru
            : AppLanguage.En;
}
