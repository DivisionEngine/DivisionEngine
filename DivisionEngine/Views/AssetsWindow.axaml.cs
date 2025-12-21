using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace DivisionEngine.Editor;

public partial class AssetsWindow : EditorWindow
{
    private readonly ScrollViewer scrollViewer;
    private readonly WrapPanel assetsPanel;

    private readonly StackPanel header;
    private readonly TextBlock headerText;

    public AssetsWindow()
    {
        InitializeComponent();

        scrollViewer = new ScrollViewer
        {
            //Content = propertiesPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        headerText = new TextBlock
        {
            Text = "No Selection",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        StackPanel mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0
        };

        mainPanel.Children.Add(headerText);
        mainPanel.Children.Add(new Border
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            Height = 1,
            Margin = new Thickness(0, 0, 0, 5)
        });
        mainPanel.Children.Add(scrollViewer);
        this.FindControl<Border>("MainBorder")!.Child = mainPanel;
    }
}