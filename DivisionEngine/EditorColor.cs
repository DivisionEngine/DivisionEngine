using Avalonia.Media;
using System;

namespace DivisionEngine.Editor
{
    /// <summary>
    /// A class to help with creating color brushes.
    /// </summary>
    public static class EditorColor
    {
        public static SolidColorBrush FromRGB(byte r, byte g, byte b) => new SolidColorBrush(Color.FromRgb(r, g, b));
        public static SolidColorBrush FromRGB(float r, float g, float b) => new SolidColorBrush(Color.FromRgb(
            Convert.ToByte(r * 255f),
            Convert.ToByte(g * 255f),
            Convert.ToByte(b * 255f)));
        public static SolidColorBrush FromRGBA(byte r, byte g, byte b, byte a) => new SolidColorBrush(Color.FromArgb(a, r, g, b));
        public static SolidColorBrush FromRGBA(float r, float g, float b, float a) => new SolidColorBrush(Color.FromArgb(
            Convert.ToByte(a * 255f),
            Convert.ToByte(r * 255f),
            Convert.ToByte(g * 255f),
            Convert.ToByte(b * 255f)));
        public static SolidColorBrush FromColor(float4 color) => new SolidColorBrush(Color.FromArgb(
            Convert.ToByte(color.W * 255f),
            Convert.ToByte(color.X * 255f),
            Convert.ToByte(color.Y * 255f),
            Convert.ToByte(color.Z * 255f)));
    }
}
