using Avalonia.Media;
using System;

namespace DivisionEngine.Editor
{
    /// <summary>
    /// A class to help with creating color brushes.
    /// </summary>
    internal static class EditorColor
    {
        /// <summary>
        /// Creates a new color from 3 RGB bytes.
        /// </summary>
        /// <param name="r">Red value from 0-255</param>
        /// <param name="g">Green value from 0-255</param>
        /// <param name="b">Blue value from 0-255</param>
        /// <returns>New SolidColorBrush with selected RGB values</returns>
        public static SolidColorBrush FromRGB(byte r, byte g, byte b) => new SolidColorBrush(Color.FromRgb(r, g, b));

        /// <summary>
        /// Creates a new color from 3 RGB floats.
        /// </summary>
        /// <param name="r">Red value from 0-1</param>
        /// <param name="g">Green value from 0-1</param>
        /// <param name="b">Blue value from 0-1</param>
        /// <returns>New SolidColorBrush with selected RGB values</returns>
        public static SolidColorBrush FromRGB(float r, float g, float b) => new SolidColorBrush(Color.FromRgb(
            Convert.ToByte(r * 255f),
            Convert.ToByte(g * 255f),
            Convert.ToByte(b * 255f)));

        /// <summary>
        /// Creates a new color from 4 RGBA bytes.
        /// </summary>
        /// <param name="r">Red value from 0-255</param>
        /// <param name="g">Green value from 0-255</param>
        /// <param name="b">Blue value from 0-255</param>
        /// <param name="a">Blue value from 0-255</param>
        /// <returns>New SolidColorBrush with selected RGBA values</returns>
        public static SolidColorBrush FromRGBA(byte r, byte g, byte b, byte a) => new SolidColorBrush(Color.FromArgb(a, r, g, b));

        /// <summary>
        /// Creates a new color from 4 RGBA floats.
        /// </summary>
        /// <param name="r">Red value from 0-1</param>
        /// <param name="g">Green value from 0-1</param>
        /// <param name="b">Blue value from 0-1</param>
        /// <param name="a">Blue value from 0-1</param>
        /// <returns>New SolidColorBrush with selected RGBA values</returns>
        public static SolidColorBrush FromRGBA(float r, float g, float b, float a) => new SolidColorBrush(Color.FromArgb(
            Convert.ToByte(a * 255f),
            Convert.ToByte(r * 255f),
            Convert.ToByte(g * 255f),
            Convert.ToByte(b * 255f)));

        /// <summary>
        /// Creates a new color from a float4 color vector.
        /// </summary>
        /// <param name="color">Float4 color vector to apply to brush</param>
        /// <returns>New SolidColorBrush with selected RGBA values</returns>
        public static SolidColorBrush FromColor(float4 color) => new SolidColorBrush(Color.FromArgb(
            Convert.ToByte(color.W * 255f),
            Convert.ToByte(color.X * 255f),
            Convert.ToByte(color.Y * 255f),
            Convert.ToByte(color.Z * 255f)));
    }
}
