using System.Security.Cryptography;
using System.Text;
using System.Windows;
using OkulPanosu.App.Views;

namespace OkulPanosu.App.Services;

/// <summary>
/// Yönetici şifresi: hash olarak paylaşılan veri klasöründeki board-data.json'da saklanır
/// (hangi PC'den girilirse girilsin aynı şifre geçerli olsun diye — Taşıt Tanıma'nın
/// SystemConfig'te sakladığı desenin aksine, burada makineye özel değil paylaşımlı olmalı).
/// 20 dakikalık oturum "unlock" durumu bellek içi tutulur (uygulama kapanınca sıfırlanır).
/// Şifre hiç kurulmadıysa korumalı işlemler serbest bırakılır.
/// </summary>
public static class AdminAuthService
{
    private static readonly TimeSpan SessionDuration = TimeSpan.FromMinutes(20);

    private static DateTime? _unlockedUntilUtc;

    public static bool IsConfigured => !string.IsNullOrEmpty(AppServices.Data?.Load().AdminPasswordHash);

    public static bool IsUnlocked => _unlockedUntilUtc is { } until && DateTime.UtcNow < until;

    public static void SetPassword(string newPassword)
    {
        if (AppServices.Data is not { } repo) return;
        var data = repo.Load();
        data.AdminPasswordHash = Hash(newPassword);
        repo.Save(data);
    }

    public static bool ChangePassword(string currentPassword, string newPassword)
    {
        if (AppServices.Data is not { } repo) return false;
        var data = repo.Load();
        if (data.AdminPasswordHash is null || Hash(currentPassword) != data.AdminPasswordHash) return false;

        data.AdminPasswordHash = Hash(newPassword);
        repo.Save(data);
        return true;
    }

    /// <summary>XXXX-XXXX-XXXX formatında yeni bir kurtarma kodu üretir, hash'ini kaydeder ve düz metni tek seferlik döner.</summary>
    public static string GenerateRecoveryCode()
    {
        var code = string.Join('-', Enumerable.Range(0, 3).Select(_ => RandomPart()));
        if (AppServices.Data is { } repo)
        {
            var data = repo.Load();
            data.AdminRecoveryHash = Hash(code);
            repo.Save(data);
        }
        return code;

        static string RandomPart()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            Span<char> chars = stackalloc char[4];
            for (var i = 0; i < chars.Length; i++) chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            return new string(chars);
        }
    }

    public static bool ResetWithRecoveryCode(string recoveryCode, string newPassword)
    {
        if (AppServices.Data is not { } repo) return false;
        var data = repo.Load();
        if (data.AdminRecoveryHash is null || Hash(recoveryCode.Trim().ToUpperInvariant()) != data.AdminRecoveryHash) return false;

        data.AdminPasswordHash = Hash(newPassword);
        repo.Save(data);
        return true;
    }

    public static bool TryUnlock(string password)
    {
        if (AppServices.Data is not { } repo) return false;
        var data = repo.Load();
        if (data.AdminPasswordHash is null || Hash(password) != data.AdminPasswordHash) return false;

        _unlockedUntilUtc = DateTime.UtcNow.Add(SessionDuration);
        return true;
    }

    public static void Lock() => _unlockedUntilUtc = null;

    /// <summary>
    /// Korumalı bir işlemden önce çağrılır. Şifre kurulmamışsa veya oturum zaten açıksa true döner;
    /// aksi halde bir parola diyaloğu gösterir. Kullanıcı iptal ederse veya yanlış girerse false döner.
    /// </summary>
    public static bool EnsureUnlocked(Window? owner)
    {
        if (!IsConfigured || IsUnlocked) return true;

        var dialog = new AdminPasswordDialog { Owner = owner };
        if (dialog.ShowDialog() != true) return false;

        if (TryUnlock(dialog.EnteredPassword)) return true;

        AppMessageBox.Show(owner, "Şifre hatalı.", "Yönetici Şifresi");
        return false;
    }

    private static string Hash(string input) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
}
