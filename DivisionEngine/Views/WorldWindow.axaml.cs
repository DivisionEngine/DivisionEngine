using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace DivisionEngine.Editor;

public partial class WorldWindow : UserControl
{
    private World refWorld;
    private bool isFake;

    private readonly ListBox entitiesList;
    private readonly ScrollViewer scrollViewer;

    private DispatcherTimer worldWinUpdater;

    public WorldWindow()
    {
        InitializeComponent();

        entitiesList = new ListBox
        {
            
        };
        scrollViewer = new ScrollViewer
        {
            Content = entitiesList
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

        worldWinUpdater = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        worldWinUpdater.Tick += WorldWinUpdater_Tick;
        worldWinUpdater.Start();
    }

    private void WorldWinUpdater_Tick(object? sender, System.EventArgs e)
    {
        Debug.Warning("Update world window");
    }

    private void UpdateListEntries()
    {
        
    }
}