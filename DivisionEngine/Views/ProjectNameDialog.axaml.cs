using Avalonia.Controls;

namespace DivisionEngine.Editor;

/// <summary>
/// Used for creating a new project name.
/// </summary>
public partial class ProjectNameDialog : Window
{
    public string ProjectName { get; set; } = "NewProject";

    public ProjectNameDialog()
    {
        InitializeComponent();
        DataContext = this;
        Width = 350;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    private void Ok_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(ProjectName);
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }
}