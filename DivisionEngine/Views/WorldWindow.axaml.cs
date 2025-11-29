using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DivisionEngine.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DivisionEngine.Editor;

public partial class WorldWindow : EditorWindow
{
    private readonly ListBox entitiesList;
    private readonly ScrollViewer scrollViewer;
    private readonly TextBlock entitiesHeader;
    private readonly StackPanel header;

    private readonly DispatcherTimer worldWinUpdater;

    private HashSet<uint> curEntities;

    private class EntityListItem
    {
        public uint Id { get; set; }
        public string Display { get; set; }

        public EntityListItem(uint entityId, World? world)
        {
            Id = entityId;

            if (world != null && world.HasComponent<Name>(entityId))
            {
                Name nameComp = world.GetComponent<Name>(entityId)!;
                Display = string.IsNullOrWhiteSpace(nameComp.name)
                    ? $"Entity_{entityId}"
                    : nameComp.name;
            }
            else Display = $"Entity_{entityId}";
        }

        public override string ToString() => $"[{Id}] {Display}";
        public override bool Equals(object? obj) =>
            obj is EntityListItem other && other.Id == Id;
        public override int GetHashCode() => Id.GetHashCode();
    }

    public WorldWindow()
    {
        InitializeComponent();

        curEntities = [];

        header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Margin = new Thickness(5, 5),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        entitiesHeader = new TextBlock
        {
            Text = "Entities: 0",
            FontSize = 10,
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center
        };

        header.Children.Add(entitiesHeader);

        entitiesList = new ListBox
        {
            BorderThickness = new Thickness(0),
            ItemTemplate = new FuncDataTemplate<EntityListItem>((item, _) =>
            {
                StackPanel panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Margin = new Thickness(8, 2),
                };
                TextBlock nameText = new TextBlock
                {
                    Text = item.Display,
                    FontSize = 12,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 0, 0)
                };
                TextBlock idText = new TextBlock
                {
                    Text = $"{item.Id}",
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                panel.Children.Add(idText);
                panel.Children.Add(nameText);
                return panel;
            })
        };

        entitiesList.SelectionChanged += EntitiesList_SelectionChanged;

        scrollViewer = new ScrollViewer
        {
            Content = entitiesList,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0
        };

        mainPanel.Children.Add(header);
        mainPanel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
            Height = 1,
        });
        mainPanel.Children.Add(scrollViewer);
        this.FindControl<Border>("MainBorder")!.Child = mainPanel;

        worldWinUpdater = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        worldWinUpdater.Tick += WorldWinUpdater_Tick;
        worldWinUpdater.Start();
    }

    private void EntitiesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (entitiesList.SelectedItem is EntityListItem selectedItem)
        {
            PropertiesWindow.LoadEntityComponents(selectedItem.Id);
        }
    }

    private void WorldWinUpdater_Tick(object? sender, EventArgs e)
    {
        if (WorldManager.CurrentWorld == null) return;
        foreach (EntityListItem? listItem in entitiesList.Items.Cast<EntityListItem?>())
        {
            if (listItem != null && W.HasComponent<Name>(listItem.Id))
            {
                string? name = W.GetComponent<Name>(listItem.Id)!.name;
                listItem.Display = string.IsNullOrWhiteSpace(name)
                    ? $"Entity_{listItem.Id}"
                    : name;
            }
        }

        UpdateListEntries();
    }

    private void UpdateListEntries()
    {
        HashSet<uint> newEntities = WorldManager.CurrentWorld!.entities;
        if (newEntities.Count == curEntities.Count && newEntities.SetEquals(curEntities)) return;

        World w = WorldManager.CurrentWorld;
        foreach (uint entity in curEntities)
        {
            if (!newEntities.Contains(entity))
            {
                EntityListItem checkListItem = new EntityListItem(entity, w);
                entitiesList.Items.Remove(checkListItem);
            }
        }

        foreach (uint entity in newEntities)
        {
            if (!curEntities.Contains(entity))
            {
                EntityListItem newListItem = new EntityListItem(entity, w);
                entitiesList.Items.Add(newListItem);
            }
        }

        curEntities.Clear();
        curEntities.UnionWith(newEntities);
        entitiesHeader.Text = $"Entities: {newEntities.Count}";
    }
}