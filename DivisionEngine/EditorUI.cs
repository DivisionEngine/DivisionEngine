using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.IO;
using Math = DivisionEngine.MathLib.Math;

namespace DivisionEngine.Editor
{
    /// <summary>
    /// API for common Division-Avalonia UI utilities.
    /// </summary>
    internal static class EditorUI
    {
        /// <summary>
        /// Literally the same as App.MainWin!.ProgressValue.
        /// </summary>
        public static double Progress
        {
            get { return App.MainWin!.ProgressValue; }
            set { App.MainWin!.ProgressValue = value; }
        }

        /// <summary>
        /// Literally the same as App.MainWin!.ShowProgress.
        /// </summary>
        public static bool ShowProgress
        {
            get { return App.MainWin!.ShowProgress; }
            set { App.MainWin!.ShowProgress = value; }
        }

        /// <summary>
        /// Checks and sees if there is a parent TabItem to this control.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static TabItem? FindParentTabItem(Control? control)
        {
            Control? current = control;
            while (current != null)
            {
                if (current is TabItem tabItem) return tabItem;
                current = current.Parent as Control;
            }
            return null;
        }

        /// <summary>
        /// Opens a file.
        /// </summary>
        /// <param name="file">File to open</param>
        public static void OpenFile(FileInfo file)
        {
            try
            {
                if (!File.Exists(file.FullName))
                {
                    Debug.Warning($"File does not exist: {file.FullName}");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = file.FullName,
                    UseShellExecute = true  // This is often needed for file associations
                });
            }
            catch (Exception ex)
            {
                Debug.Warning($"Could not open file: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts a number of bytes to a string representation.
        /// </summary>
        /// <param name="bytes">Byte amount</param>
        /// <param name="decimalPlaces">Number of decimal places to include</param>
        /// <returns>Formatted byte size string</returns>
        public static string FormatFileSize(long bytes, int decimalPlaces = 1)
        {
            if (bytes <= 0) return "0 B";
            string[] sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            // Use log to find the right unit index
            int unitIndex = (int)Math.Min(Math.Floor(Math.Log(bytes, 1024)), sizeSuffixes.Length - 1);
            double size = bytes / Math.Pow(1024, unitIndex);
            string format = $"{{0:F{decimalPlaces}}} {{1}}";
            return string.Format(format, size, sizeSuffixes[unitIndex]);
        }
    }
}
