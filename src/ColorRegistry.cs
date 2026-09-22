using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinSelectionColor
{
    public class SelectionColorSettings
    {
        public Color Hilight { get; set; }
        public Color HotTrackingColor { get; set; }
        public Color HilightText { get; set; }
        public Color MenuHilight { get; set; }
        public bool SyncDwmAccent { get; set; }

        public SelectionColorSettings()
        {
            // Windows 10 default colors
            Hilight = Color.FromArgb(0, 120, 215);
            HotTrackingColor = Color.FromArgb(0, 102, 204);
            HilightText = Color.FromArgb(255, 255, 255);
            MenuHilight = Color.FromArgb(0, 120, 215);
            SyncDwmAccent = false;
        }
    }

    public static class ColorRegistry
    {
        private const int COLOR_HIGHLIGHT = 13;
        private const int COLOR_HIGHLIGHTTEXT = 14;
        private const int COLOR_HOTLIGHT = 26;
        private const int COLOR_MENUHILIGHT = 29;

        private const uint WM_SYSCOLORCHANGE = 0x0015;
        private const uint WM_SETTINGCHANGE = 0x001A;
        private const uint SMTO_ABORTIFHUNG = 0x0002;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetSysColors(int cElements, int[] lpaElements, uint[] lpaRgbValues);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint msg,
            UIntPtr wParam,
            string lParam,
            uint fuFlags,
            uint uTimeout,
            out UIntPtr lpdwResult);

        [DllImport("dwmapi.dll", EntryPoint = "#127", PreserveSig = false)]
        private static extern void DwmSetColorizationColor(uint color, bool blend);

        private static string GetBackupFilePath()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDir, "selection_color_backup.ini");
        }

        public static SelectionColorSettings LoadCurrentSettings()
        {
            var settings = new SelectionColorSettings();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors", false))
                {
                    if (key != null)
                    {
                        settings.Hilight = ParseColor(key.GetValue("Hilight") as string, settings.Hilight);
                        settings.HotTrackingColor = ParseColor(key.GetValue("HotTrackingColor") as string, settings.HotTrackingColor);
                        settings.HilightText = ParseColor(key.GetValue("HilightText") as string, settings.HilightText);
                        settings.MenuHilight = ParseColor(key.GetValue("MenuHilight") as string, settings.MenuHilight);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading registry: " + ex.Message);
            }
            return settings;
        }

        public static void EnsureInitialBackup()
        {
            string backupPath = GetBackupFilePath();
            if (!File.Exists(backupPath))
            {
                SaveBackup();
            }
        }

        public static void SaveBackup()
        {
            var cur = LoadCurrentSettings();
            string backupPath = GetBackupFilePath();
            try
            {
                using (StreamWriter sw = new StreamWriter(backupPath, false))
                {
                    sw.WriteLine("[WinSelectionColor_Backup]");
                    sw.WriteLine("Timestamp=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sw.WriteLine("Hilight=" + ToRgbString(cur.Hilight));
                    sw.WriteLine("HotTrackingColor=" + ToRgbString(cur.HotTrackingColor));
                    sw.WriteLine("HilightText=" + ToRgbString(cur.HilightText));
                    sw.WriteLine("MenuHilight=" + ToRgbString(cur.MenuHilight));

                    // Backup DWM accent
                    using (RegistryKey dwm = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM", false))
                    {
                        if (dwm != null)
                        {
                            object val = dwm.GetValue("ColorizationColor");
                            if (val != null) sw.WriteLine("ColorizationColor=" + val);
                            object acc = dwm.GetValue("AccentColor");
                            if (acc != null) sw.WriteLine("AccentColor=" + acc);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to save backup: " + ex.Message);
            }
        }

        public static SelectionColorSettings LoadBackup()
        {
            string backupPath = GetBackupFilePath();
            if (!File.Exists(backupPath))
                return null;

            var settings = new SelectionColorSettings();
            try
            {
                foreach (string line in File.ReadAllLines(backupPath))
                {
                    if (string.IsNullOrEmpty(line) || line.StartsWith("[")) continue;
                    int idx = line.IndexOf('=');
                    if (idx > 0)
                    {
                        string k = line.Substring(0, idx).Trim();
                        string v = line.Substring(idx + 1).Trim();
                        if (k.Equals("Hilight", StringComparison.OrdinalIgnoreCase))
                            settings.Hilight = ParseColor(v, settings.Hilight);
                        else if (k.Equals("HotTrackingColor", StringComparison.OrdinalIgnoreCase))
                            settings.HotTrackingColor = ParseColor(v, settings.HotTrackingColor);
                        else if (k.Equals("HilightText", StringComparison.OrdinalIgnoreCase))
                            settings.HilightText = ParseColor(v, settings.HilightText);
                        else if (k.Equals("MenuHilight", StringComparison.OrdinalIgnoreCase))
                            settings.MenuHilight = ParseColor(v, settings.MenuHilight);
                    }
                }
                return settings;
            }
            catch
            {
                return null;
            }
        }

        public static bool ApplySettings(SelectionColorSettings settings)
        {
            bool regSuccess = false;

            // 1. Write to HKCU\Control Panel\Colors
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Hilight", ToRgbString(settings.Hilight), RegistryValueKind.String);
                        key.SetValue("HotTrackingColor", ToRgbString(settings.HotTrackingColor), RegistryValueKind.String);
                        key.SetValue("HilightText", ToRgbString(settings.HilightText), RegistryValueKind.String);
                        key.SetValue("MenuHilight", ToRgbString(settings.MenuHilight), RegistryValueKind.String);
                        regSuccess = true;
                    }
                }

                // Also update HKCU\Control Panel\Desktop\Colors if key exists
                using (RegistryKey dKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\Colors", true))
                {
                    if (dKey != null)
                    {
                        dKey.SetValue("Hilight", ToRgbString(settings.Hilight), RegistryValueKind.String);
                        dKey.SetValue("HotTrackingColor", ToRgbString(settings.HotTrackingColor), RegistryValueKind.String);
                        dKey.SetValue("HilightText", ToRgbString(settings.HilightText), RegistryValueKind.String);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Registry write failed: " + ex.Message);
            }

            // 2. Call SetSysColors for immediate live effect in standard apps
            try
            {
                int[] elements = new int[]
                {
                    COLOR_HIGHLIGHT,
                    COLOR_HIGHLIGHTTEXT,
                    COLOR_HOTLIGHT,
                    COLOR_MENUHILIGHT
                };

                uint[] rgbValues = new uint[]
                {
                    ToColorRef(settings.Hilight),
                    ToColorRef(settings.HilightText),
                    ToColorRef(settings.HotTrackingColor),
                    ToColorRef(settings.MenuHilight)
                };

                SetSysColors(elements.Length, elements, rgbValues);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SetSysColors failed: " + ex.Message);
            }

            // 3. Sync Windows DWM / Taskbar Accent Color if enabled
            if (settings.SyncDwmAccent)
            {
                ApplyDwmAccentColor(settings.Hilight);
            }

            // 4. Broadcast setting change notifications
            try
            {
                UIntPtr result;
                SendMessageTimeout(HWND_BROADCAST, WM_SYSCOLORCHANGE, UIntPtr.Zero, null, SMTO_ABORTIFHUNG, 500, out result);
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero, "Colors", SMTO_ABORTIFHUNG, 500, out result);
            }
            catch { }

            return regSuccess;
        }

        public static void ApplyDwmAccentColor(Color c)
        {
            try
            {
                // DWM AccentColor is ABGR: 0xFFBBGGRR
                uint abgr = 0xFF000000u | ((uint)c.B << 16) | ((uint)c.G << 8) | (uint)c.R;
                // DWM ColorizationColor is ARGB: 0xC4RRGGBB
                uint argb = 0xC4000000u | ((uint)c.R << 16) | ((uint)c.G << 8) | (uint)c.B;

                using (RegistryKey dwm = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM", true))
                {
                    if (dwm != null)
                    {
                        dwm.SetValue("AccentColor", unchecked((int)abgr), RegistryValueKind.DWord);
                        dwm.SetValue("ColorizationColor", unchecked((int)argb), RegistryValueKind.DWord);
                        dwm.SetValue("ColorizationAfterglow", unchecked((int)argb), RegistryValueKind.DWord);
                    }
                }

                using (RegistryKey acc = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent", true))
                {
                    if (acc != null)
                    {
                        Color darker = Color.FromArgb((int)(c.R * 0.82), (int)(c.G * 0.82), (int)(c.B * 0.82));
                        uint abgrDark = 0xFF000000u | ((uint)darker.B << 16) | ((uint)darker.G << 8) | (uint)darker.R;

                        acc.SetValue("AccentColorMenu", unchecked((int)abgr), RegistryValueKind.DWord);
                        acc.SetValue("StartColorMenu", unchecked((int)abgrDark), RegistryValueKind.DWord);
                        acc.SetValue("AccentPalette", GenerateAccentPalette(c), RegistryValueKind.Binary);
                    }
                }

                try
                {
                    DwmSetColorizationColor(argb, false);
                }
                catch { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ApplyDwmAccentColor error: " + ex.Message);
            }
        }

        public static byte[] GenerateAccentPalette(Color c)
        {
            // 8 colors x 4 bytes each = 32 bytes (R, G, B, 0)
            byte[] palette = new byte[32];
            double[] factors = new double[] { 1.45, 1.30, 1.15, 1.00, 0.80, 0.60, 0.40, 1.10 };

            for (int i = 0; i < 8; i++)
            {
                int r = Math.Max(0, Math.Min(255, (int)(c.R * factors[i])));
                int g = Math.Max(0, Math.Min(255, (int)(c.G * factors[i])));
                int b = Math.Max(0, Math.Min(255, (int)(c.B * factors[i])));

                palette[i * 4 + 0] = (byte)r;
                palette[i * 4 + 1] = (byte)g;
                palette[i * 4 + 2] = (byte)b;
                palette[i * 4 + 3] = 0;
            }
            return palette;
        }

        public static bool RestartExplorer()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("explorer");
                foreach (Process p in procs)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(3000);
                    }
                    catch { }
                }

                System.Threading.Thread.Sleep(500);

                // Start explorer again
                ProcessStartInfo psi = new ProcessStartInfo("explorer.exe");
                psi.UseShellExecute = true;
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to restart explorer: " + ex.Message);
                return false;
            }
        }

        public static Color ParseColor(string rgbStr, Color fallback)
        {
            if (string.IsNullOrEmpty(rgbStr)) return fallback;
            string[] parts = rgbStr.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                int r, g, b;
                if (int.TryParse(parts[0], out r) && int.TryParse(parts[1], out g) && int.TryParse(parts[2], out b))
                {
                    return Color.FromArgb(
                        Math.Max(0, Math.Min(255, r)),
                        Math.Max(0, Math.Min(255, g)),
                        Math.Max(0, Math.Min(255, b)));
                }
            }
            return fallback;
        }

        public static string ToRgbString(Color c)
        {
            return string.Format("{0} {1} {2}", c.R, c.G, c.B);
        }

        public static uint ToColorRef(Color c)
        {
            // Win32 COLORREF is 0x00bbggrr
            return (uint)(c.R | (c.G << 8) | (c.B << 16));
        }

        public static Color GetContrastingTextColor(Color background)
        {
            // Calculate relative luminance based on sRGB coefficients
            double luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
            return luminance > 0.55 ? Color.FromArgb(20, 20, 20) : Color.FromArgb(255, 255, 255);
        }

        public static Color GenerateBorderColor(Color fill)
        {
            // Slightly deeper/more saturated shade for clean border
            int r = (int)(fill.R * 0.82);
            int g = (int)(fill.G * 0.82);
            int b = (int)(fill.B * 0.82);
            return Color.FromArgb(Math.Max(0, r), Math.Max(0, g), Math.Max(0, b));
        }
    }
}
