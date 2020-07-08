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
    }
}
