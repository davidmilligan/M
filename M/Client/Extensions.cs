using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace M.Client
{
    public static class Extensions
    {
        public static string TextColor(this string htmlColor)
        {
            if (!string.IsNullOrEmpty(htmlColor) && htmlColor.StartsWith("#"))
            {
                static int RepeatHexDigit(int value) => (value & 0xF) | ((value & 0xF) << 4);
                int argb = int.Parse(htmlColor.Substring(1), NumberStyles.HexNumber);
                var color = htmlColor.Length switch
                {
                    3 => Color.FromArgb(RepeatHexDigit(argb >> 8), RepeatHexDigit(argb >> 4), RepeatHexDigit(argb)),
                    6 => Color.FromArgb((argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF),
                    _ => Color.FromArgb(argb)
                };
                double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
                if (luminance > 0.5)
                {
                    return "black";
                }
                else
                {
                    return "white";
                }
            }
            return "black";
        }

        public static string FormatDateRelativeToNow(this DateTimeOffset date) => date switch
        {
            var d when (DateTimeOffset.Now - d).TotalMinutes <= 1.0 => "Just Now",
            var d when (DateTimeOffset.Now - d).TotalHours < 1.0 => $"{Math.Round((DateTimeOffset.Now - d).TotalMinutes)} minutes ago",
            var d when d.Date == DateTimeOffset.Now.Date => $"{Math.Round((DateTimeOffset.Now - d).TotalHours)} hours ago",
            var d when d > DateTimeOffset.Now.Date.AddDays(-1) => $"yesterday at {d.LocalDateTime.ToShortTimeString()}",
            var d when d > DateTimeOffset.Now.Date.AddDays(-7) => $"{d.DayOfWeek} at {d.LocalDateTime.ToShortTimeString()}",
            _ => date.ToString(),
        };
    }
}
