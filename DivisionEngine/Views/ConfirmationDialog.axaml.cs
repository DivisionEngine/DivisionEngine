using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DivisionEngine.Editor;

public partial class ConfirmationDialog : Window
{
    public new string Title { get; set; } = "Confirm";
    public string Message { get; set; } = "Are you sure?";

    public ConfirmationDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void OnYesClicked(object? obj, RoutedEventArgs args) => Close(true);
    private void OnNoClicked(object? obj, RoutedEventArgs args) => Close(false);
}