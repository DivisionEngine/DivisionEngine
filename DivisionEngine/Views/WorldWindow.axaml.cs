using Avalonia.Controls;

namespace DivisionEngine.Editor;

public partial class WorldWindow : UserControl
{
    private World refWorld;
    private bool isFake;

    private readonly ListBox entitiesList;
    private readonly ScrollViewer scrollViewer;

    public WorldWindow()
    {
        InitializeComponent();

        entitiesList = new ListBox
        {
            
        };
        scrollViewer = new ScrollViewer
        {

        };

        isFake = true;

        if (WorldManager.CurrentWorld != null)
        {
            refWorld = WorldManager.CurrentWorld;
            isFake = false;
        }
        else
        {
            refWorld = WorldManager.CreateDefaultWorld(false);
            isFake = true;
        }
    }
}