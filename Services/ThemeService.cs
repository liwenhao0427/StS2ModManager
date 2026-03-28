using System.Windows;
using System.Windows.Media;

namespace StS2ModManager.Services;

public static class ThemeService
{
    // ── 亮色 ──────────────────────────────────────────────────────────────
    private static readonly Dictionary<string, Color> Light = new()
    {
        ["WindowBg"]          = C(0xEC, 0xF1, 0xF7),
        ["CardBrush"]         = C(0xFF, 0xFF, 0xFF),
        ["CardHeaderBg"]      = C(0xF7, 0xFB, 0xFF),
        ["LineBrush"]         = C(0xD9, 0xE3, 0xEF),
        ["TitleBrush"]        = C(0x12, 0x30, 0x47),
        ["TextFg"]            = C(0x30, 0x42, 0x56),
        ["SubtextFg"]         = C(0x58, 0x70, 0x86),
        ["MutedFg"]           = C(0x5D, 0x73, 0x88),
        ["HintFg"]            = C(0x60, 0x79, 0x8E),
        ["VersionFg"]         = C(0x7A, 0x9A, 0xB5),
        ["StatusOkFg"]        = C(0x15, 0x80, 0x3D),
        ["SyncProgressFg"]    = C(0x1D, 0x5E, 0x8A),
        // 普通按钮
        ["BtnBg"]             = C(0xF4, 0xF7, 0xFB),
        ["BtnFg"]             = C(0x29, 0x40, 0x55),
        ["BtnBorder"]         = C(0xC7, 0xD6, 0xE7),
        ["BtnHover"]          = C(0xE7, 0xEF, 0xF8),
        ["BtnPress"]          = C(0xD7, 0xE5, 0xF4),
        // 输入框
        ["InputBg"]           = C(0xFF, 0xFF, 0xFF),
        ["InputFg"]           = C(0x30, 0x42, 0x56),
        ["InputBorder"]       = C(0xC7, 0xD6, 0xE7),
        // 列表
        ["ListBg"]            = C(0xF9, 0xFB, 0xFD),
        ["ListBorder"]        = C(0xD7, 0xE2, 0xEE),
        // 筛选栏小卡片
        ["FilterCardBg"]      = C(0xF7, 0xFB, 0xFF),
        ["FilterCardBorder"]  = C(0xD7, 0xE5, 0xF2),
        // Mod 列表项 - 路径标签
        ["ModPathBg"]         = C(0xFF, 0xF2, 0xE5),
        ["ModPathBorder"]     = C(0xE1, 0xB4, 0x8A),
        ["ModPathFg"]         = C(0x8A, 0x4F, 0x1D),
        // Mod 列表项 - tag 标签
        ["ModTagBg"]          = C(0xE8, 0xF4, 0xFF),
        ["ModTagBorder"]      = C(0x9A, 0xC5, 0xE8),
        ["ModTagFg"]          = C(0x1D, 0x5E, 0x8A),
        // 标签编辑区
        ["TagAreaBg"]         = C(0xF6, 0xFA, 0xFF),
        ["TagAreaBorder"]     = C(0xD7, 0xE5, 0xF2),
        ["TagChipBg"]         = C(0xE8, 0xF3, 0xFF),
        ["TagChipBorder"]     = C(0xA8, 0xC9, 0xEA),
        ["TagChipFg"]         = C(0x1D, 0x5E, 0x8A),
        // 小删除按钮
        ["TagRemoveBg"]       = C(0xE7, 0xEE, 0xF6),
        ["TagRemoveBorder"]   = C(0xBF, 0xD2, 0xE6),
        ["TagRemoveFg"]       = C(0x36, 0x53, 0x6D),
        // 状态栏
        ["StatusBarFg"]       = C(0x4B, 0x60, 0x73),
        // 捐赠按钮（亮色保持暖黄）
        ["DonateBg"]          = C(0xFF, 0xF8, 0xEC),
        ["DonateFg"]          = C(0x8A, 0x6A, 0x2A),
        ["DonateBorder"]      = C(0xE8, 0xD5, 0xA0),
    };

    // ── 深色 ──────────────────────────────────────────────────────────────
    private static readonly Dictionary<string, Color> Dark = new()
    {
        ["WindowBg"]          = C(0x14, 0x1A, 0x22),
        ["CardBrush"]         = C(0x1E, 0x27, 0x35),
        ["CardHeaderBg"]      = C(0x1A, 0x23, 0x30),
        ["LineBrush"]         = C(0x2E, 0x3D, 0x52),
        ["TitleBrush"]        = C(0xC8, 0xDF, 0xF5),
        ["TextFg"]            = C(0xD0, 0xE2, 0xF4),
        ["SubtextFg"]         = C(0x8A, 0xA8, 0xC8),
        ["MutedFg"]           = C(0x7A, 0x98, 0xB8),
        ["HintFg"]            = C(0x6A, 0x88, 0xA8),
        ["VersionFg"]         = C(0x6A, 0x8A, 0xAA),
        ["StatusOkFg"]        = C(0x4A, 0xD4, 0x7A),
        ["SyncProgressFg"]    = C(0x5A, 0xA8, 0xE8),
        // 普通按钮
        ["BtnBg"]             = C(0x28, 0x35, 0x46),
        ["BtnFg"]             = C(0xC8, 0xDF, 0xF5),
        ["BtnBorder"]         = C(0x3E, 0x52, 0x6A),
        ["BtnHover"]          = C(0x34, 0x44, 0x5C),
        ["BtnPress"]          = C(0x3E, 0x52, 0x6A),
        // 输入框
        ["InputBg"]           = C(0x18, 0x22, 0x2E),
        ["InputFg"]           = C(0xD0, 0xE2, 0xF4),
        ["InputBorder"]       = C(0x3E, 0x52, 0x6A),
        // 列表
        ["ListBg"]            = C(0x18, 0x22, 0x2E),
        ["ListBorder"]        = C(0x2E, 0x3D, 0x52),
        // 筛选栏小卡片
        ["FilterCardBg"]      = C(0x1A, 0x23, 0x30),
        ["FilterCardBorder"]  = C(0x2E, 0x3D, 0x52),
        // Mod 列表项 - 路径标签
        ["ModPathBg"]         = C(0x2E, 0x22, 0x14),
        ["ModPathBorder"]     = C(0x6A, 0x44, 0x22),
        ["ModPathFg"]         = C(0xE8, 0xB8, 0x80),
        // Mod 列表项 - tag 标签
        ["ModTagBg"]          = C(0x14, 0x28, 0x3E),
        ["ModTagBorder"]      = C(0x2E, 0x52, 0x78),
        ["ModTagFg"]          = C(0x7A, 0xC4, 0xF0),
        // 标签编辑区
        ["TagAreaBg"]         = C(0x18, 0x22, 0x2E),
        ["TagAreaBorder"]     = C(0x2E, 0x3D, 0x52),
        ["TagChipBg"]         = C(0x14, 0x28, 0x3E),
        ["TagChipBorder"]     = C(0x2E, 0x52, 0x78),
        ["TagChipFg"]         = C(0x7A, 0xC4, 0xF0),
        // 小删除按钮
        ["TagRemoveBg"]       = C(0x28, 0x35, 0x46),
        ["TagRemoveBorder"]   = C(0x3E, 0x52, 0x6A),
        ["TagRemoveFg"]       = C(0xA0, 0xC0, 0xE0),
        // 状态栏
        ["StatusBarFg"]       = C(0x8A, 0xA8, 0xC8),
        // 捐赠按钮（深色模式用稍暗的暖色）
        ["DonateBg"]          = C(0x2E, 0x26, 0x14),
        ["DonateFg"]          = C(0xE8, 0xC8, 0x78),
        ["DonateBorder"]      = C(0x5A, 0x48, 0x22),
    };

    public static void Apply(bool dark)
    {
        var palette = dark ? Dark : Light;

        var win = Application.Current.MainWindow;
        if (win == null) return;

        // 自定义画刷写入 Window.Resources（DynamicResource 在可视树内查找）
        var res = win.Resources;
        foreach (var (key, color) in palette)
            res[key] = new SolidColorBrush(color);

        win.Background = new SolidColorBrush(palette["WindowBg"]);

        // SystemColors 必须写入 Application.Resources
        // 因为 ComboBox 的 Popup 是独立顶层窗口，不在 Window 可视树内
        var appRes = Application.Current.Resources;

        var inputBg     = new SolidColorBrush(palette["InputBg"]);
        var inputFg     = new SolidColorBrush(palette["InputFg"]);
        var textBrush   = new SolidColorBrush(palette["TextFg"]);
        var highlightBg = new SolidColorBrush(dark ? C(0x2A, 0x50, 0x80) : C(0x33, 0x99, 0xFF));
        var highlightFg = new SolidColorBrush(Colors.White);

        // ComboBox 下拉弹出层背景和文字
        appRes[SystemColors.WindowBrushKey]          = inputBg;
        appRes[SystemColors.WindowTextBrushKey]       = inputFg;
        // 通用控件文字（CheckBox、RadioButton 等）
        appRes[SystemColors.ControlTextBrushKey]      = textBrush;
        // 选中项高亮
        appRes[SystemColors.HighlightBrushKey]        = highlightBg;
        appRes[SystemColors.HighlightTextBrushKey]    = highlightFg;
        appRes[SystemColors.InactiveSelectionHighlightBrushKey]     = highlightBg;
        appRes[SystemColors.InactiveSelectionHighlightTextBrushKey] = highlightFg;
        // 控件背景
        appRes[SystemColors.ControlBrushKey]          = inputBg;
    }

    private static Color C(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
}
