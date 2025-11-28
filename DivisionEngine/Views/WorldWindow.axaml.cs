using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

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

        Border? border = this.FindControl<Border>("MainBorder");
        border!.Child = scrollViewer;

        worldWinUpdater = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        worldWinUpdater.Tick += WorldWinUpdater_Tick;
        worldWinUpdater.Start();
    }

    private void WorldWinUpdater_Tick(object? sender, System.EventArgs e)
    {
        if (WorldManager.CurrentWorld == null) return;
        UpdateListEntries();
    }

    private void UpdateListEntries()
    {
        HashSet<uint> newEntities = WorldManager.CurrentWorld!.entities;
        if (newEntities.Count == entitiesList.Items.Count) return;
        
        foreach (uint entity in newEntities)
        {
            if (!entitiesList.Items.Contains(entity))
                entitiesList.Items.Add(entity);
        }
    }
}