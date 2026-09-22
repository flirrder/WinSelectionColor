using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WinSelectionColor
{
    public static class WallpaperHelper
    {
        private const uint SPI_GETDESKWALLPAPER = 0x0073;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(uint uAction, uint uParam, StringBuilder lpvParam, uint fuWinIni);

        public static string GetCurrentWallpaperPath()
        {
            try
            {
                // 1. Try registry key
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false))
                {
                    if (key != null)
                    {
                        string path = key.GetValue("Wallpaper") as string;
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            return path;
                        }
                    }
                }

                // 2. Try SystemParametersInfo
                StringBuilder sb = new StringBuilder(260);
                if (SystemParametersInfo(SPI_GETDESKWALLPAPER, (uint)sb.Capacity, sb, 0) != 0)
                {
                    string path = sb.ToString();
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        return path;
                    }
                }

                // 3. Try TranscodedWallpaper (Windows cache)
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string transcoded = Path.Combine(appData, @"Microsoft\Windows\Themes\TranscodedWallpaper");
                if (File.Exists(transcoded))
                {
                    return transcoded;
                }
            }
            catch { }

            return null;
        }

        public static Image GetWallpaperThumbnail(string imagePath, int targetWidth, int targetHeight)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return null;

            try
            {
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Image original = Image.FromStream(stream))
                {
                    Bitmap thumb = new Bitmap(targetWidth, targetHeight);
                    using (Graphics g = Graphics.FromImage(thumb))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        // Calculate aspect ratio crop/fill
                        float srcAspect = (float)original.Width / original.Height;
                        float dstAspect = (float)targetWidth / targetHeight;

                        Rectangle srcRect;
                        if (srcAspect > dstAspect)
                        {
                            int w = (int)(original.Height * dstAspect);
                            srcRect = new Rectangle((original.Width - w) / 2, 0, w, original.Height);
                        }
                        else
                        {
                            int h = (int)(original.Width / dstAspect);
                            srcRect = new Rectangle(0, (original.Height - h) / 2, original.Width, h);
                        }

                        g.DrawImage(original, new Rectangle(0, 0, targetWidth, targetHeight), srcRect, GraphicsUnit.Pixel);
                    }
                    return thumb;
                }
            }
            catch
            {
                return null;
            }
        }

        public static List<Color> ExtractDominantColors(string imagePath, int maxCount)
        {
            var results = new List<Color>();
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return results;

            try
            {
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Image original = Image.FromStream(stream))
                {
                    // Scale down to 70x70 for fast processing
                    int sampleSize = 70;
                    using (Bitmap thumb = new Bitmap(sampleSize, sampleSize))
                    {
                        using (Graphics g = Graphics.FromImage(thumb))
                        {
                            g.InterpolationMode = InterpolationMode.Bilinear;
                            g.DrawImage(original, 0, 0, sampleSize, sampleSize);
                        }

                        // Quantize into color buckets (step 32 gives 8x8x8=512 buckets)
                        var bucketCounts = new Dictionary<int, int>();
                        var bucketRgbSums = new Dictionary<int, long[]>();

                        for (int y = 0; y < sampleSize; y++)
                        {
                            for (int x = 0; x < sampleSize; x++)
                            {
                                Color px = thumb.GetPixel(x, y);

                                // Filter out extremes: pure black / pure white
                                int brightness = (px.R + px.G + px.B) / 3;
                                if (brightness < 18 || brightness > 242) continue;

                                int qR = px.R / 28;
                                int qG = px.G / 28;
                                int qB = px.B / 28;
                                int bucketKey = (qR << 16) | (qG << 8) | qB;

                                if (!bucketCounts.ContainsKey(bucketKey))
                                {
                                    bucketCounts[bucketKey] = 0;
                                    bucketRgbSums[bucketKey] = new long[3];
                                }

                                bucketCounts[bucketKey]++;
                                bucketRgbSums[bucketKey][0] += px.R;
                                bucketRgbSums[bucketKey][1] += px.G;
                                bucketRgbSums[bucketKey][2] += px.B;
                            }
                        }

                        // Sort buckets by popularity
                        var sortedBuckets = new List<KeyValuePair<int, int>>(bucketCounts);
                        sortedBuckets.Sort((a, b) => b.Value.CompareTo(a.Value));

                        // Select diverse colors (ensure min euclidean color distance)
                        foreach (var kvp in sortedBuckets)
                        {
                            int count = kvp.Value;
                            if (count < 3) continue;

                            long[] sums = bucketRgbSums[kvp.Key];
                            Color candidate = Color.FromArgb(
                                (int)(sums[0] / count),
                                (int)(sums[1] / count),
                                (int)(sums[2] / count)
                            );

                            // Check distance against already picked colors
                            bool isDistinct = true;
                            foreach (Color chosen in results)
                            {
                                double dist = ColorDistance(candidate, chosen);
                                if (dist < 22.0)
                                {
                                    isDistinct = false;
                                    break;
                                }
                            }

                            if (isDistinct)
                            {
                                results.Add(candidate);
                                if (results.Count >= maxCount) break;
                            }
                        }
                    }
                }
            }
            catch { }

            // If wallpaper has few colors, generate harmonious accent variations (lighter, richer)
            if (results.Count > 0 && results.Count < 6)
            {
                int baseCount = results.Count;
                for (int i = 0; i < baseCount && results.Count < maxCount; i++)
                {
                    Color baseCol = results[i];
                    // Create a slightly more vibrant / lighter accent
                    int r = Math.Min(255, (int)(baseCol.R * 1.35) + 15);
                    int g = Math.Min(255, (int)(baseCol.G * 1.35) + 15);
                    int b = Math.Min(255, (int)(baseCol.B * 1.35) + 15);
                    results.Add(Color.FromArgb(r, g, b));
                }
            }

            // If empty or few, add nice fallbacks
            if (results.Count == 0)
            {
                results.Add(Color.FromArgb(0, 120, 215));  // Windows Default
                results.Add(Color.FromArgb(142, 68, 173)); // Purple
                results.Add(Color.FromArgb(39, 174, 96));  // Emerald
                results.Add(Color.FromArgb(230, 126, 34)); // Amber
                results.Add(Color.FromArgb(231, 76, 60));  // Crimson
                results.Add(Color.FromArgb(52, 73, 94));   // Slate
            }

            return results;
        }

        private static double ColorDistance(Color c1, Color c2)
        {
            long rDiff = c1.R - c2.R;
            long gDiff = c1.G - c2.G;
            long bDiff = c1.B - c2.B;
            // Weighted euclidean distance roughly matching human perception
            return Math.Sqrt(0.3 * rDiff * rDiff + 0.59 * gDiff * gDiff + 0.11 * bDiff * bDiff);
        }
    }
}
