//
// Copyright (C) 2026 Rex Woodfield
//
// This file is part of Division Engine.
//
// Division Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Division Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Division Engine.  If not, see <https://www.gnu.org/licenses/>.
//
using Avalonia.Controls;
using DivisionEngine.Editor.ViewModels;
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
        /// Functionally the same as MainWindowViewModel.vm!.ProgressValue!.Value.
        /// </summary>
        public static double Progress
        {
            get { return MainWindowViewModel.vm!.ProgressValue!.Value; }
            set { MainWindowViewModel.vm!.ProgressValue = value; }
        }

        /// <summary>
        /// Functionally the same as MainWindowViewModel.vm!.ShowProgress!.Value.
        /// </summary>
        public static bool ShowProgress
        {
            get { return MainWindowViewModel.vm!.ShowProgress!.Value; }
            set { MainWindowViewModel.vm!.ShowProgress = value; }
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
                Debug.Warning($"Could not open file", ex);
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
