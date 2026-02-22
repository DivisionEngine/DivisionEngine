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
