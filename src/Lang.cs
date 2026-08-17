namespace SshKeyManager;

internal enum AppLanguage
{
    En,
    Ru,
}

internal static class Lang
{
    public static bool Ru => PasswordOptions.Language == AppLanguage.Ru;

    private static string S(string en, string ru) => Ru ? ru : en;

    public static string DateFormat => Ru ? "dd.MM.yyyy HH:mm" : "yyyy-MM-dd HH:mm";

    // --- main window ---
    public static string Subtitle => S("Manage your SSH keys.", "Управление SSH-ключами.");
    public static string CreateKey => S("+ Create key", "+ Создать ключ");
    public static string Refresh => S("Refresh", "Обновить");
    public static string Settings => S("Settings", "Настройки");
    public static string SshAgent => S("SSH Agent", "Агент SSH");

    public static string KeysOnThisPc => S("Keys on this computer", "Ключи на этом компьютере");
    public static string SelectedKey => S("Selected key", "Выбранный ключ");

    // --- list columns ---
    public static string ColName => S("Name", "Имя");
    public static string ColType => S("Type", "Тип");
    public static string ColFingerprint => S("Fingerprint", "Отпечаток");
    public static string ColAgent => S("Agent", "Агент");
    public static string ColProtection => S("Protection", "Защита");
    public static string ColState => S("State", "Состояние");

    // --- selected key labels ---
    public static string Name => S("Name:", "Имя:");
    public static string Type => S("Type:", "Тип:");
    public static string Fingerprint => S("Fingerprint:", "Отпечаток:");
    public static string Comment => S("Comment:", "Комментарий:");
    public static string PrivateKey => S("Private key:", "Приватный ключ:");
    public static string PublicKey => S("Public key:", "Публичный ключ:");
    public static string Protection => S("Protection:", "Защита:");
    public static string Password => S("Password:", "Пароль:");
    public static string SshAgentLabel => S("SSH Agent:", "Агент SSH:");
    public static string Changed => S("Modified:", "Изменён:");

    public static string KeysFound(int count) =>
        Ru ? $"Найдено ключей: {count}" : $"Keys found: {count}";

    public static string Folder => S("Folder: ", "Каталог: ");

    // --- context menu ---
    public static string CopyPublicKey => S("Copy public key", "Копировать публичный ключ");
    public static string CopyFingerprint => S("Copy fingerprint", "Копировать отпечаток");
    public static string OpenFolder => S("Open folder", "Открыть папку");
    public static string Rename => S("Rename", "Переименовать");
    public static string AddToAgent => S("Add to agent", "Добавить в агент");
    public static string RemoveFromAgent => S("Remove from agent", "Убрать из агента");
    public static string ChangePassword => S("Change password", "Изменить пароль");
    public static string DeleteKey => S("Delete key", "Удалить ключ");

    public static string AgentStartTip => S("Start the ssh-agent service", "Запустить службу ssh-agent");
    public static string AgentStopTip => S("Stop the ssh-agent service", "Остановить службу ssh-agent");
    public static string AgentMissingTip => S("ssh-agent service not found", "Служба ssh-agent не найдена");

    public static string NoPubFile => S("This key has no .pub file.", "У этого ключа нет файла .pub.");
    public static string NoFingerprint => S("This key has no fingerprint.", "У этого ключа нет отпечатка.");
    public static string NoPrivateKey => S("No private key.", "Нет приватного ключа.");
    public static string CreateSshKey => S("Create SSH key", "Создать SSH-ключ");
    public static string DeleteKeyConfirm(string name) =>
        Ru
            ? $"Удалить ключ «{name}»?\nБудут удалены private и .pub. Это нельзя отменить."
            : $"Delete key “{name}”?\nThe private key and .pub will be removed. This cannot be undone.";

    // --- list values ---
    public static string PasswordNotSaved => S("not saved", "не сохранён");
    public static string ProtectionPassword => S("Password", "Пароль");
    public static string ProtectionNone => S("No password", "Без пароля");
    public static string AgentLoaded => S("Loaded", "Загружен");
    public static string AgentNotLoaded => S("Not loaded", "Не загружен");
    public static string AgentYes => S("● Yes", "● Да");
    public static string AgentNo => S("○ No", "○ Нет");
    public static string StateOk => "✓ Ok";
    public static string StateNoPrivate => S("⚠ No private", "⚠ Нет private");
    public static string StateNoPub => S("⚠ No .pub", "⚠ Нет .pub");
    public static string StateWeakRsa => "⚠ RSA2048";

    // --- dialogs ---
    public static string Cancel => S("Cancel", "Отмена");
    public static string Save => S("Save", "Сохранить");
    public static string Create => S("Create", "Создать");
    public static string Delete => S("Delete", "Удалить");
    public static string Ok => "OK";

    public static string Language => S("Language:", "Язык:");
    public static string LanguageEnglish => "English";
    public static string LanguageRussian => "Русский";
    public static string PasswordGeneration => S("Password generation", "Генерация пароля");
    public static string Length => S("Length:", "Длина:");
    public static string Lower => S("Lowercase a–z", "Строчные a–z");
    public static string Upper => S("Uppercase A–Z", "Заглавные A–Z");
    public static string Digits => S("Digits 0–9", "Цифры 0–9");
    public static string Special => S("Special characters", "Спецсимволы");
    public static string NeedCharset => S("Select at least one character set.", "Выберите хотя бы один набор символов.");
    public static string SaveSettingsFailed(string message) =>
        Ru ? "Не удалось сохранить настройки: " + message : "Could not save settings: " + message;

    public static string KeyLabel(string name) => S("Key: ", "Ключ: ") + name;
    public static string CurrentPassword => S("Current password:", "Текущий пароль:");
    public static string NewPassword => S("New password:", "Новый пароль:");
    public static string RepeatPassword => S("Repeat new password:", "Повтор нового пароля:");
    public static string GenerateTip(int length) =>
        Ru
            ? $"Случайный пароль ({length} символов). Набор — в Настройках."
            : $"Random password ({length} characters). Character set is in Settings.";
    public static string ShowHidePassword => S("Show or hide password", "Показать или скрыть пароль");
    public static string CopyPassword => S("Copy password", "Копировать пароль");
    public static string PastePassword => S("Paste password", "Вставить пароль");
    public static string EmptyCharset =>
        S("No character set is selected in Settings.", "В Настройках не выбран набор символов для генерации.");
    public static string PasswordMismatch =>
        S("New password and confirmation do not match.", "Новый пароль и повтор не совпадают.");
    public static string EnterNewOrRemove =>
        S("Enter a new password, or click Delete to remove protection.",
            "Укажите новый пароль или нажмите «Удалить», чтобы снять защиту.");
    public static string EnterCurrentPassword => S("Enter the current password.", "Укажите текущий пароль.");
    public static string EnterCurrentToRemove =>
        S("To remove the password, enter the current one.", "Чтобы удалить пароль, укажите текущий.");
    public static string KeyHasNoPassword => S("This key has no password.", "У этого ключа нет пароля.");

    public static string SaveTo => S("Save to:", "Сохранить в:");
    public static string KeyFolder => S("Folder for the key", "Каталог для ключа");
    public static string InvalidFileName => S("Enter a valid file name.", "Укажите корректное имя файла.");
    public static string EnterFolder => S("Enter a folder.", "Укажите каталог.");

    public static string KeyPassword => S("Key password", "Пароль ключа");
    public static string EnterPasswordFor(string name) =>
        Ru
            ? "Введите пароль для " + name + ".\nПустое поле — без пароля."
            : "Enter the password for " + name + ".\nLeave empty for no password.";

    public static string RenameKey => S("Rename key", "Переименовать ключ");
    public static string RenameCaption =>
        S("New file name (without .pub). The private/.pub pair is renamed together.",
            "Новое имя файла (без .pub). Пара private/.pub переименуется вместе.");

    // --- scanner / ssh-keygen / ssh-add errors ---
    public static string EmptyName => S("Name cannot be empty.", "Имя не должно быть пустым.");
    public static string InvalidNameChars => S("The name cannot contain path characters.", "В имени нельзя использовать символы пути.");
    public static string ReservedName => S("This is a reserved name and cannot be used for a key.", "Это служебное имя, его нельзя использовать для ключа.");
    public static string KeyFilesNotFound => S("Key files were not found.", "Файлы ключа не найдены.");
    public static string FileExists(string name) => S("File already exists: ", "Файл уже есть: ") + name;
    public static string RenameFailed(string message) =>
        Ru ? "Не удалось переименовать: " + message : "Could not rename: " + message;
    public static string DeleteFailed(string message) =>
        Ru ? "Не удалось удалить: " + message : "Could not delete: " + message;
    public static string SshKeygenMissing => S("ssh-keygen.exe was not found.", "ssh-keygen.exe не найден.");
    public static string PrivateKeyMissing => S("Private key was not found.", "Приватный ключ не найден.");
    public static string SshKeygenStartFailed => S("Could not start ssh-keygen.", "Не удалось запустить ssh-keygen.");
    public static string SshKeygenTimeout => S("ssh-keygen timed out.", "Таймаут ssh-keygen.");
    public static string WrongCurrentPassword => S("Current password is incorrect.", "Неверный текущий пароль.");
    public static string ChangePasswordFailed => S("Could not change the key password.", "Не удалось изменить пароль ключа.");
    public static string CreateKeyFailed => S("Could not create the key.", "Не удалось создать ключ.");

    // --- ssh-agent service ---
    public static string AgentServiceMissing =>
        S("ssh-agent service was not found. OpenSSH is required.", "Служба ssh-agent не найдена. Нужен OpenSSH.");
    public static string AgentDidNotStart => S("The service did not start.", "Служба не запустилась.");
    public static string AgentDidNotStop => S("The service did not stop.", "Служба не остановилась.");
    public static string UacCancelled => S("Service start was cancelled in UAC.", "Запуск службы отменён в UAC.");
    public static string ScFailed(int exit) =>
        Ru ? $"sc.exe завершился с кодом {exit}." : $"sc.exe exited with code {exit}.";
    public static string ScStartFailed => S("Could not start sc.exe.", "Не удалось запустить sc.exe");
    public static string ScTimeout => S("sc.exe timed out.", "Таймаут sc.exe");
    public static string StartAgentFirst =>
        S("Start SSH Agent with the toolbar button first.", "Сначала запустите Агент SSH кнопкой вверху.");
    public static string SshAddMissing => S("ssh-add.exe was not found.", "ssh-add.exe не найден.");
    public static string AgentNotConnected =>
        S("No connection to the agent. Start SSH Agent with the toolbar button.",
            "Нет связи с агентом. Запустите Агент SSH кнопкой вверху.");
    public static string WrongKeyPassword => S("Incorrect key password.", "Неверный пароль ключа.");
    public static string AddToAgentFailed => S("Could not add the key to the agent.", "Не удалось добавить ключ в агент.");
    public static string RemoveFromAgentFailed => S("Could not remove the key from the agent.", "Не удалось убрать ключ из агента.");
    public static string SshAddStartFailed => S("Could not start ssh-add.", "Не удалось запустить ssh-add.");
    public static string SshAddTimeout => S("ssh-add timed out.", "Таймаут ssh-add.");
}
