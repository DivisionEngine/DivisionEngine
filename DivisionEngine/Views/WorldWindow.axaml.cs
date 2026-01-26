using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DivisionEngine.Components;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DivisionEngine.Editor;

/// <summary>
/// Window for displaying an entity hierarchy view of the ECS world.
/// </summary>
public partial class WorldWindow : EditorWindow
{
    private readonly StackPanel entitiesPanel;
    private readonly ScrollViewer scrollViewer;
    private readonly TextBlock entitiesHeader;
    private readonly StackPanel header;
    private readonly Grid mainGrid;

    private readonly DispatcherTimer worldWinUpdater;
    private readonly Dictionary<uint, EntityItemControl> entityControls;

    private readonly HashSet<uint> curEntities;

    /// <summary>
    /// Represents an item in the entity display list used by this window's stack panel, internal only.
    /// </summary>
    private class EntityItemControl : Border
    {
        private readonly uint entityId;
        private readonly TextBlock idText;
        private readonly TextBlock nameText;
        private readonly StackPanel panel;
        private readonly ContextMenu contextMenu;

        public uint EntityId => entityId;

        public EntityItemControl(uint entityId, World? world)
        {
            this.entityId = entityId;

            // Set up visual appearance
            Background = EditorColor.FromRGB(30, 30, 30);
            BorderBrush = EditorColor.FromRGB(30, 30, 30);
            BorderThickness = new Thickness(0);
            Margin = new Thickness(0, 0);
            Padding = new Thickness(10, 2);
            CornerRadius = new CornerRadius(0);

            // Create content panel
            panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
            };
            idText = new TextBlock
            {
                Text = $"{entityId}",
                FontSize = 10,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 20,
            };
            nameText = new TextBlock
            {
                FontSize = 12,
                Foreground = EditorColor.FromRGB(220, 220, 220),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 0, 0),
            };

            panel.Children.Add(idText);
            panel.Children.Add(nameText);
            Child = panel;

            // Build context menu
            contextMenu = CreateContextMenu(entityId);

            // Initial update
            UpdateDisplay(world);

            // Add handlers
            PointerPressed += (s, e) => PropertiesWindow.LoadEntityComponents(entityId);
            ContextRequested += (s, e) => { contextMenu?.Open(this); e.Handled = true; };
            PointerEntered += (s, e) => { Background = EditorColor.FromRGB(17, 17, 17); };
            PointerExited += (s, e) => { Background = EditorColor.FromRGB(30, 30, 30); };
        }

        private static ContextMenu CreateContextMenu(uint entityId)
        {
            ContextMenu menu = new ContextMenu
            {
                Background = EditorColor.FromRGB(24, 24, 24),
                BorderBrush = EditorColor.FromRGB(68, 68, 68),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(0),
            };

            List<MenuItem> menuItems = [];

            // Rename entity
            MenuItem renameItem = new MenuItem
            {
                Header = "Rename",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Rename, FontSize = 12, Margin = new Thickness(0) },
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Margin = new Thickness(0),
            };
            //renameItem.Click += (s, e) => W.DuplicateEntity(entityId);
            menuItems.Add(renameItem);

            // Duplicate entity
            MenuItem duplicateItem = new MenuItem
            {
                Header = "Duplicate",
                Icon = new MaterialIcon { Kind = MaterialIconKind.ContentDuplicate, FontSize = 12, Margin = new Thickness(0) },
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Margin = new Thickness(0),
            };
            duplicateItem.Click += (s, e) => W.DuplicateEntity(entityId);
            menuItems.Add(duplicateItem);

            // Delete entity
            MenuItem deleteItem = new MenuItem
            {
                Header = "Delete",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Delete, FontSize = 12, Margin = new Thickness(0) },
                Background = Brushes.Transparent,
                Foreground = EditorColor.FromRGB(220, 68, 68),
                Margin = new Thickness(0),
            };
            deleteItem.Click += (s, e) => W.DestroyEntity(entityId);
            menuItems.Add(deleteItem);

            menu.ItemsSource = menuItems;
            return menu;
        }

        public void UpdateDisplay(World? world)
        {
            string displayName;
            if (world != null && world.HasComponent<Name>(entityId))
            {
                Name nameComp = world.GetComponent<Name>(entityId)!;
                displayName = string.IsNullOrWhiteSpace(nameComp.name)
                    ? $"Entity_{entityId}"
                    : nameComp.name;
            }
            else displayName = $"Entity_{entityId}";
            nameText.Text = displayName;
        }
    }

    /// <summary>
    /// Creates a new instance of the world window.
    /// </summary>
    public WorldWindow()
    {
        InitializeComponent();
        curEntities = [];
        entityControls = [];

        header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Margin = new Thickness(5, 5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        entitiesHeader = new TextBlock
        {
            Text = "Entities: 0",
            FontSize = 10,
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center,
        };
        header.Children.Add(entitiesHeader);

        // Now using StackPanel
        entitiesPanel = new StackPanel
        {
            Margin = new Thickness(0),
        };
        Border separator = new Border
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            Height = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        scrollViewer = new ScrollViewer
        {
            Content = entitiesPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        // Create main grid with proper row definitions
        mainGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
            },
        };
        Grid.SetRow(header, 0);
        Grid.SetRow(separator, 1);
        Grid.SetRow(scrollViewer, 2);
        mainGrid.Children.Add(header);
        mainGrid.Children.Add(separator);
        mainGrid.Children.Add(scrollViewer);
        this.FindControl<Border>("MainBorder")!.Child = mainGrid;

        worldWinUpdater = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        worldWinUpdater.Tick += WorldWinUpdater_Tick;
        worldWinUpdater.Start();
    }

    /// <summary>
    /// Called when the world window updates (4fps).
    /// </summary>
    private void WorldWinUpdater_Tick(object? sender, EventArgs e)
    {
        if (WorldManager.CurrentWorld == null) return;
        World w = WorldManager.CurrentWorld;

        // Update existing entity displays
        foreach (var entityId in curEntities.ToList())
        {
            if (entityControls.TryGetValue(entityId, out var control))
            {
                control.UpdateDisplay(w);
            }
        }

        // Update the list of entities
        UpdateListEntries();
    }

    /// <summary>
    /// Called when the entity list must be updated in-place.
    /// </summary>
    private void UpdateListEntries()
    {
        if (WorldManager.CurrentWorld == null) return;
        HashSet<uint> newEntities = WorldManager.CurrentWorld.entities;

        // Remove entities that no longer exist
        foreach (uint entityId in curEntities.ToList())
        {
            if (!newEntities.Contains(entityId))
            {
                if (entityControls.TryGetValue(entityId, out var control))
                {
                    entitiesPanel.Children.Remove(control);
                    entityControls.Remove(entityId);
                }
            }
        }

        // Add new entities
        foreach (uint entityId in newEntities)
        {
            if (!curEntities.Contains(entityId))
            {
                var control = new EntityItemControl(entityId, WorldManager.CurrentWorld);
                entityControls[entityId] = control;
                entitiesPanel.Children.Add(control);
            }
        }

        // Update current entities set
        curEntities.Clear();
        curEntities.UnionWith(newEntities);
        entitiesHeader.Text = $"Entities: {newEntities.Count}";
    }
}