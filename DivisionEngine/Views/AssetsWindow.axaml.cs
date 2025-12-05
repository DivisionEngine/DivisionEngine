using Avalonia.Controls;

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
    }
}