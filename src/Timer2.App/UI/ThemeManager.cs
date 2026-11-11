using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Timer2.App.UI
{
    public class ThemePalette
    {
        public string Name { get; set; } = "CyberGreen";
        public Color Background { get; set; }
        public Color Foreground { get; set; }
        public Color AccentLabelColor { get; set; }
        public Color ButtonBackground { get; set; }
        public Color ButtonForeground { get; set; }
        public Color ButtonBorderColor { get; set; }
        public Color TextBoxBackground { get; set; }
        public Color TextBoxForeground { get; set; }
        public Color LogBoxBackground { get; set; }
        public Color LogBoxForeground { get; set; }
        public Color MenuBackground { get; set; }
        public Color MenuForeground { get; set; }
    }

    public static class ThemeManager
    {
        public static Dictionary<string, ThemePalette> Themes { get; } = new Dictionary<string, ThemePalette>(StringComparer.OrdinalIgnoreCase)
        {
            ["CyberGreen"] = new ThemePalette
            {
                Name = "CyberGreen (Default)",
                Background = Color.Black,
                Foreground = Color.White,
                AccentLabelColor = Color.FromArgb(0, 255, 0), // Lime green as in screenshots
                ButtonBackground = Color.White,
                ButtonForeground = Color.Black,
                ButtonBorderColor = Color.FromArgb(0, 120, 215), // Crisp blue border
                TextBoxBackground = Color.FromArgb(200, 200, 200),
                TextBoxForeground = Color.Black,
                LogBoxBackground = Color.FromArgb(224, 224, 224), // Light gray console in screenshot
                LogBoxForeground = Color.Black,
                MenuBackground = Color.FromArgb(240, 240, 240),
                MenuForeground = Color.Black
            },
            ["MidnightBlue"] = new ThemePalette
            {
                Name = "Midnight Blue",
                Background = Color.FromArgb(15, 23, 42),
                Foreground = Color.FromArgb(248, 250, 252),
                AccentLabelColor = Color.FromArgb(56, 189, 248),
                ButtonBackground = Color.FromArgb(30, 41, 59),
                ButtonForeground = Color.FromArgb(241, 245, 249),
                ButtonBorderColor = Color.FromArgb(56, 189, 248),
                TextBoxBackground = Color.FromArgb(30, 41, 59),
                TextBoxForeground = Color.White,
                LogBoxBackground = Color.FromArgb(2, 6, 23),
                LogBoxForeground = Color.FromArgb(226, 232, 240),
                MenuBackground = Color.FromArgb(30, 41, 59),
                MenuForeground = Color.White
            },
            ["ObsidianDark"] = new ThemePalette
            {
                Name = "Obsidian Dark",
                Background = Color.FromArgb(24, 24, 27),
                Foreground = Color.FromArgb(244, 244, 245),
                AccentLabelColor = Color.FromArgb(129, 140, 248),
                ButtonBackground = Color.FromArgb(39, 39, 42),
                ButtonForeground = Color.White,
                ButtonBorderColor = Color.FromArgb(129, 140, 248),
                TextBoxBackground = Color.FromArgb(39, 39, 42),
                TextBoxForeground = Color.White,
                LogBoxBackground = Color.FromArgb(9, 9, 11),
                LogBoxForeground = Color.FromArgb(228, 228, 231),
                MenuBackground = Color.FromArgb(39, 39, 42),
                MenuForeground = Color.White
            },
            ["PaperLight"] = new ThemePalette
            {
                Name = "Paper Light",
                Background = Color.FromArgb(248, 250, 252),
                Foreground = Color.FromArgb(15, 23, 42),
                AccentLabelColor = Color.FromArgb(22, 163, 74),
                ButtonBackground = Color.White,
                ButtonForeground = Color.FromArgb(15, 23, 42),
                ButtonBorderColor = Color.FromArgb(203, 213, 225),
                TextBoxBackground = Color.White,
                TextBoxForeground = Color.FromArgb(15, 23, 42),
                LogBoxBackground = Color.White,
                LogBoxForeground = Color.FromArgb(15, 23, 42),
                MenuBackground = Color.FromArgb(241, 245, 249),
                MenuForeground = Color.Black
            },
            ["MatrixHacker"] = new ThemePalette
            {
                Name = "Matrix Hacker",
                Background = Color.FromArgb(5, 5, 5),
                Foreground = Color.FromArgb(34, 197, 94),
                AccentLabelColor = Color.FromArgb(74, 222, 128),
                ButtonBackground = Color.FromArgb(20, 83, 45),
                ButtonForeground = Color.FromArgb(187, 247, 208),
                ButtonBorderColor = Color.FromArgb(34, 197, 94),
                TextBoxBackground = Color.FromArgb(10, 30, 15),
                TextBoxForeground = Color.FromArgb(74, 222, 128),
                LogBoxBackground = Color.FromArgb(2, 44, 34),
                LogBoxForeground = Color.FromArgb(74, 222, 128),
                MenuBackground = Color.FromArgb(10, 30, 15),
                MenuForeground = Color.FromArgb(74, 222, 128)
            }
        };

        public static ThemePalette GetPalette(string themeName)
        {
            if (string.IsNullOrEmpty(themeName) || !Themes.TryGetValue(themeName, out var palette))
            {
                return Themes["CyberGreen"];
            }
            return palette;
        }

        public static void ApplyTheme(Form form, string themeName)
        {
            var palette = GetPalette(themeName);
            form.BackColor = palette.Background;
            form.ForeColor = palette.Foreground;

            ApplyThemeRecursive(form, palette);
        }

        private static void ApplyThemeRecursive(Control parent, ThemePalette palette)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is MenuStrip menu)
                {
                    menu.BackColor = palette.MenuBackground;
                    menu.ForeColor = palette.MenuForeground;
                    foreach (ToolStripMenuItem item in menu.Items)
                    {
                        item.BackColor = palette.MenuBackground;
                        item.ForeColor = palette.MenuForeground;
                        ApplyMenuItemTheme(item, palette);
                    }
                }
                else if (c is Button btn)
                {
                    btn.BackColor = palette.ButtonBackground;
                    btn.ForeColor = palette.ButtonForeground;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = palette.ButtonBorderColor;
                    btn.FlatAppearance.BorderSize = 1;
                    btn.Cursor = Cursors.Hand;
                }
                else if (c.Tag != null && c.Tag.ToString() == "LogBox")
                {
                    c.BackColor = palette.LogBoxBackground;
                    c.ForeColor = palette.LogBoxForeground;
                }
                else if (c is TextBox txt)
                {
                    txt.BackColor = palette.TextBoxBackground;
                    txt.ForeColor = palette.TextBoxForeground;
                }
                else if (c is ComboBox cb)
                {
                    cb.BackColor = palette.ButtonBackground;
                    cb.ForeColor = palette.ButtonForeground;
                    cb.FlatStyle = FlatStyle.Flat;
                }
                else if (c is Label lbl)
                {
                    lbl.ForeColor = palette.AccentLabelColor;
                }
                else if (c is CheckBox chk)
                {
                    chk.ForeColor = palette.AccentLabelColor;
                }

                if (c.HasChildren)
                {
                    ApplyThemeRecursive(c, palette);
                }
            }
        }

        private static void ApplyMenuItemTheme(ToolStripMenuItem parentItem, ThemePalette palette)
        {
            foreach (ToolStripItem sub in parentItem.DropDownItems)
            {
                if (sub is ToolStripMenuItem menuItem)
                {
                    menuItem.BackColor = palette.MenuBackground;
                    menuItem.ForeColor = palette.MenuForeground;
                    ApplyMenuItemTheme(menuItem, palette);
                }
            }
        }
    }
}
