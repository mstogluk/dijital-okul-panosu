using System.Windows;

namespace OkulPanosu.App.Services;

public sealed record AppTheme(string Key, string DisplayName, string SwatchHex, Uri ResourceUri);

/// <summary>
/// Koyu taban her zaman sabit kalır; kullanıcı sadece vurgu (accent) rengini değiştirebilir.
/// Şartnamedeki renk paletiyle (Bölüm "TEMA VE GÖRSEL TASARIM") birebir eşleşir.
/// Seçim LocalSettings üzerinden kalıcı tutulur.
/// </summary>
public static class ThemeManager
{
    public static readonly IReadOnlyList<AppTheme> Themes =
    [
        new("Blue", "Mavi", "#2563EB", new Uri("Themes/Accent.Blue.xaml", UriKind.Relative)),
        new("Emerald", "Zümrüt", "#059669", new Uri("Themes/Accent.Emerald.xaml", UriKind.Relative)),
        new("Amber", "Kehribar", "#D97706", new Uri("Themes/Accent.Amber.xaml", UriKind.Relative)),
        new("Rose", "Gül", "#E11D48", new Uri("Themes/Accent.Rose.xaml", UriKind.Relative)),
        new("Indigo", "İndigo", "#4F46E5", new Uri("Themes/Accent.Indigo.xaml", UriKind.Relative)),
        new("Violet", "Menekşe", "#7C3AED", new Uri("Themes/Accent.Violet.xaml", UriKind.Relative)),
        new("Purple", "Mor", "#9333EA", new Uri("Themes/Accent.Purple.xaml", UriKind.Relative)),
        new("Sky", "Gökyüzü", "#0284C7", new Uri("Themes/Accent.Sky.xaml", UriKind.Relative)),
        new("Teal", "Turkuaz", "#0D9488", new Uri("Themes/Accent.Teal.xaml", UriKind.Relative)),
        new("Orange", "Turuncu", "#EA580C", new Uri("Themes/Accent.Orange.xaml", UriKind.Relative)),
        new("Slate", "Kayrak", "#475569", new Uri("Themes/Accent.Slate.xaml", UriKind.Relative)),
    ];

    public const string DefaultThemeKey = "Blue";

    public static string CurrentThemeKey { get; private set; } = DefaultThemeKey;

    public static event Action<string>? ThemeChanged;

    public static string GetSwatchHex(string? themeColorKey) =>
        Themes.FirstOrDefault(t => t.Key == themeColorKey)?.SwatchHex ?? Themes.First(t => t.Key == DefaultThemeKey).SwatchHex;

    public static void Apply(string? themeKey)
    {
        var theme = Themes.FirstOrDefault(t => t.Key == themeKey) ?? Themes.First(t => t.Key == DefaultThemeKey);
        var dictionaries = Application.Current.Resources.MergedDictionaries;

        var existing = dictionaries.FirstOrDefault(d => d.Source is { } src && src.OriginalString.Contains("Themes/Accent."));
        var newDict = new ResourceDictionary { Source = theme.ResourceUri };

        if (existing is not null)
        {
            var index = dictionaries.IndexOf(existing);
            dictionaries[index] = newDict;
        }
        else
        {
            dictionaries.Add(newDict);
        }

        CurrentThemeKey = theme.Key;
        ThemeChanged?.Invoke(theme.Key);
    }
}
