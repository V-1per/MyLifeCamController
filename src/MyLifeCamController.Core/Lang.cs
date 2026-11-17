using System.Globalization;

namespace MyLifeCamController.Core;

/// <summary>
/// Picks the UI language: Portuguese when Windows is set to Portuguese, English otherwise.
/// Every user-facing string is written as <c>Lang.T("pt text", "en text")</c>.
/// </summary>
public static class Lang
{
    public static bool IsPortuguese { get; set; } =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt";

    public static string T(string portuguese, string english) => IsPortuguese ? portuguese : english;
}
