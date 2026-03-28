using System.Windows;
using System.Windows.Media;

namespace StS2ModManager.Services;

public static class ThemeService
{
    // 亮色
    private static readonly Color LightWindowBg    = Color.FromRgb(0xEC, 0xF1, 0xF7);
    private static readonly Color LightCard        = Colors.White;
    private static readonly Color LightLine        = Color.FromRgb(0xD9, 0xE3, 0xEF);
    private static readonly Color LightTitle       = Color.FromRgb(0x12, 0x30, 0x47);
    private static readonly Color LightText        = Color.FromRgb(0x30, 0x42, 0x56);
    private static readonly Color LightBtnBg       = Color.FromRgb(0xF4, 0xF7, 0xFB);
    private static readonly Color LightBtnFg       = Color.FromRgb(0x29, 0x40, 0x55);
    private static readonly Color LightBtnBorder   = Color.FromRgb(0xC7, 0xD6, 0xE7);
    private static readonly Color LightBtnHover    = Color.FromRgb(0xE7, 0xEF, 0xF8);
    private static readonly Color LightBtnPress    = Color.FromRgb(0xD7, 0xE5, 0xF4);
    private static readonly Color LightInputBg     = Colors.White;
    private static readonly Color LightInputBorder = Color.FromRgb(0xC7, 0xD6, 0xE7);
    private static readonly Color LightListBg      = Color.FromRgb(0xF9, 0xFB, 0xFD);
    private static readonly Color LightListBorder  = Color.FromRgb(0xD7, 0xE2, 0xEE);

    // 深色
    private static readonly Color DarkWindowBg    = Color.FromRgb(0x18, 0x1E, 0x26);
    private static readonly Color DarkCard        = Color.FromRgb(0x22, 0x2B, 0x38);
    private static readonly Color DarkLine        = Color.FromRgb(0x35, 0x44, 0x58);
    private static readonly Color DarkTitle       = Color.FromRgb(0xC8, 0xDF, 0xF5);
    private static readonly Color DarkText        = Color.FromRgb(0xB8, 0xCC, 0xE0);
    private static readonly Color DarkBtnBg       = Color.FromRgb(0x2C, 0x38, 0x4A);
    private static readonly Color DarkBtnFg       = Color.FromRgb(0xC8, 0xDF, 0xF5);
    private static readonly Color DarkBtnBorder   = Color.FromRgb(0x45, 0x5A, 0x72);
    private static readonly Color DarkBtnHover    = Color.FromRgb(0x38, 0x48, 0x60);
    private static readonly Color DarkBtnPress    = Color.FromRgb(0x42, 0x55, 0x6E);
    private static readonly Color DarkInputBg     = Color.FromRgb(0x1E, 0x28, 0x36);
    private static readonly Color DarkInputBorder = Color.FromRgb(0x45, 0x5A, 0x72);
    private static readonly Color DarkListBg      = Color.FromRgb(0x1E, 0x28, 0x36);
    private static readonly Color DarkListBorder  = Color.FromRgb(0x35, 0x44, 0x58);

    public static void Apply(bool dark)
    {
        var res = Application.Current.Resources;

        Set(res, "CardBrush",      dark ? DarkCard        : LightCard);
        Set(res, "LineBrush",      dark ? DarkLine        : LightLine);
        Set(res, "TitleBrush",     dark ? DarkTitle       : LightTitle);
        Set(res, "TextFg",         dark ? DarkText        : LightText);
        Set(res, "BtnBg",          dark ? DarkBtnBg       : LightBtnBg);
        Set(res, "BtnFg",          dark ? DarkBtnFg       : LightBtnFg);
        Set(res, "BtnBorder",      dark ? DarkBtnBorder   : LightBtnBorder);
        Set(res, "BtnHover",       dark ? DarkBtnHover    : LightBtnHover);
        Set(res, "BtnPress",       dark ? DarkBtnPress    : LightBtnPress);
        Set(res, "InputBg",        dark ? DarkInputBg     : LightInputBg);
        Set(res, "InputBorder",    dark ? DarkInputBorder : LightInputBorder);
        Set(res, "ListBg",         dark ? DarkListBg      : LightListBg);
        Set(res, "ListBorder",     dark ? DarkListBorder  : LightListBorder);

        // 窗口背景
        if (Application.Current.MainWindow is { } win)
        {
            win.Background = new SolidColorBrush(dark ? DarkWindowBg : LightWindowBg);
        }
    }

    private static void Set(ResourceDictionary res, string key, Color color)
    {
        if (res[key] is SolidColorBrush brush)
            brush.Color = color;
        else
            res[key] = new SolidColorBrush(color);
    }
}
