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
using Avalonia;
using Avalonia.Controls;

namespace DivisionEngine.Editor
{
    /// <summary>
    /// Displays up to two decimals for consistent formatting.
    /// </summary>
    internal class SmartNumericUpDown : NumericUpDown
    {
        private bool _isUpdatingText = false;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ValueProperty && !_isUpdatingText)
            {
                _isUpdatingText = true;
                UpdateTextFormat();
                _isUpdatingText = false;
            }
        }

        private void UpdateTextFormat()
        {
            if (Value.HasValue)
            {
                decimal val = Value.Value;
                if (val % 1 == 0) FormatString = "0";
                else FormatString = "0.##";
            }
        }
    }
}
