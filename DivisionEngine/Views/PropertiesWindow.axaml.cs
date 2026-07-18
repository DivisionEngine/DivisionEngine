//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DivisionEngine.Components;
using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.Components.Lights;
using DivisionEngine.Editor.Systems;
using DivisionEngine.MathLib;
using DivisionEngine.Projects.Assets;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Button = Avalonia.Controls.Button;
using Environment = DivisionEngine.Components.Environment;
using Math = DivisionEngine.MathLib.Math;
using Transform = DivisionEngine.Components.Transform;

namespace DivisionEngine.Editor;

/// <summary>
/// Represents the properties window in the Division editor.
/// </summary>
public partial class PropertiesWindow : EditorWindow
{
    private static readonly List<PropertiesWindow?> currentWindows = [];

    private readonly StackPanel propertiesPanel;
    private readonly ScrollViewer scrollViewer;
    private readonly StackPanel header;
    private readonly TextBlock headerText;
    private readonly Button addComponentButton;

    private uint curEntityId;

    /// <summary>
    /// Loads this entity when the properties window is opened.
    /// </summary>
    private static uint LastSelected { get; set; } = uint.MaxValue;

    public PropertiesWindow()
    {
        InitializeComponent();
        curEntityId = uint.MaxValue;

        // Panel
        propertiesPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(5),
        };
        scrollViewer = new ScrollViewer
        {
            Content = propertiesPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalAlignment = VerticalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Top,
        };

        // Header
        Border separator = new Border
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            Height = 1,
        };
        header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Background = EditorColor.FromRGB(28, 28, 28),
            VerticalAlignment = VerticalAlignment.Top,
        };
        headerText = new TextBlock
        {
            Text = "No Selection",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        header.Children.Add(headerText);

        // Footer (add component button)
        Border separator2 = new Border
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            Height = 1,
        };
        StackPanel footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Background = EditorColor.FromRGB(28, 28, 28),
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        DockPanel buttonContent = new DockPanel
        {
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        MaterialIcon buttonIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.BoxAdd,
            Margin = new Thickness(4),
            Foreground = EditorColor.FromRGB(200, 255, 200),
            VerticalAlignment = VerticalAlignment.Center,
        };
        TextBlock buttonText = new TextBlock
        {
            Text = "Add Component",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        DockPanel.SetDock(buttonIcon, Dock.Left);
        DockPanel.SetDock(buttonText, Dock.Top);
        buttonContent.Children.Add(buttonIcon);
        buttonContent.Children.Add(buttonText);
        addComponentButton = new Button
        {
            Content = buttonContent,
            FontSize = 14,
            FontWeight = FontWeight.Medium,
            Foreground = EditorColor.FromRGB(200, 200, 200),
            Background = EditorColor.FromRGB(20, 20, 20),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(3),

            Height = 26,
            Margin = new Thickness(12, 5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        Flyout addComponentFlyout = new Flyout
        {
            Placement = PlacementMode.Top,
            ShowMode = FlyoutShowMode.Standard,
            Content = CreateAddComponentMenu(), // Add padding adjustment
        };
        addComponentButton.Click += (_, _) => addComponentFlyout.ShowAt(addComponentButton);

        // Assemble panel
        Grid mainGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            }
        };
        header.SetValue(Grid.RowProperty, 0);
        separator.SetValue(Grid.RowProperty, 1);
        scrollViewer.SetValue(Grid.RowProperty, 2);
        separator2.SetValue(Grid.RowProperty, 3);
        addComponentButton.SetValue(Grid.RowProperty, 4);
        mainGrid.Children.Add(header);
        mainGrid.Children.Add(separator);
        mainGrid.Children.Add(scrollViewer);
        mainGrid.Children.Add(separator2);
        mainGrid.Children.Add(addComponentButton);
        this.FindControl<Border>("MainBorder")!.Child = mainGrid;
        currentWindows.Add(this);

        if (W.EntityExists(LastSelected)) DisplayEntityComponents(LastSelected);
        else CreateWorldEditor(WorldManager.CurrentWorld);

        Selection.OnSelectionChanged += OnSelectedObject; // Add window to selection system
    }
    private void OnSelectedObject(object? selection)
    {
        if (Selection.SelectedType == SelectionType.Entity) LoadEntityComponents((uint)selection!);
    }

    /// <summary>
    /// Gets all active properties windows.
    /// </summary>
    public static List<PropertiesWindow?> GetCurrentWindows()
    {
        ValidatePropertiesWindows();
        return currentWindows;
    }

    /// <summary>
    /// Creates the add component flyout menu.
    /// </summary>
    /// <returns>Stack panel add component flyout menu</returns>
    private StackPanel CreateAddComponentMenu()
    {
        StackPanel addComponentMenu = new StackPanel
        {
            Spacing = 1,
        };
        TextBox searchBox = new TextBox
        {
            InnerLeftContent = new MaterialIcon
            {
                Kind = MaterialIconKind.Search,
                Foreground = EditorColor.FromRGB(128, 128, 128),
                Margin = new Thickness(6, 0, 0, 0),
                Width = 12,
                Height = 12,
            },
            Text = "",
            PlaceholderText = "Search Components...",
            FontSize = 12,
            Foreground = EditorColor.FromRGB(220, 220, 220),
            Background = EditorColor.FromRGB(17, 17, 17),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinWidth = 240,
            Margin = new Thickness(0),
        };
        addComponentMenu.Children.Add(searchBox);

        StackPanel compListPanel = new StackPanel();
        ScrollViewer addComponentScrollView = new ScrollViewer
        {
            Content = compListPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalAlignment = VerticalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Top,
            MaxHeight = 200,
        };
        addComponentMenu.Children.Add(addComponentScrollView);

        List<Type> componentTypes = GetComponentTypes();
        PopulateComponentList(compListPanel, componentTypes, "");

        searchBox.TextChanged += (s, e) => PopulateComponentList(compListPanel, componentTypes, searchBox.Text ?? "");
        return addComponentMenu;
    }

    /// <summary>
    /// Populates the component list with filtered types.
    /// </summary>
    private void PopulateComponentList(StackPanel compListPanel, List<Type> componentTypes, string searchText)
    {
        compListPanel.Children.Clear();
        string searchLower = searchText.ToLowerInvariant().Replace(" ", "");
        bool hasFilter = !string.IsNullOrWhiteSpace(searchText);

        foreach (Type compType in componentTypes)
        {
            string displayName = FormatComponentName(compType.Name);
            string searchableText = (compType.Name + " " + displayName).ToLowerInvariant();

            // Filter by search text
            if (hasFilter && !searchableText.Contains(searchLower)) continue;

            Button compTypeButton = new Button
            {
                Content = displayName,
                FontSize = 11,
                Background = EditorColor.FromRGB(20, 20, 20),
                Foreground = EditorColor.FromRGB(200, 200, 200),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                MinWidth = 240,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 2),
                Tag = compType, // Component type stored in tag
            };

            compTypeButton.Click += (sender, _) =>
            {
                if (sender is Button btn && btn.Tag is Type type)
                {
                    if (curEntityId != uint.MaxValue && !W.HasComponent(curEntityId, type))
                    {
                        IComponent? compInstance = (IComponent?)Activator.CreateInstance(type);
                        if (compInstance != null && W.AddComponent(curEntityId, compInstance))
                            LoadEntityComponents(curEntityId);
                        else Debug.Warning($"Failed to add component | Null: {compInstance != null}");
                    }
                }
            };

            compListPanel.Children.Add(compTypeButton);
        }

        // No results if no matches
        if (compListPanel.Children.Count == 0)
        {
            TextBlock noResultsText = new TextBlock
            {
                Text = "No components found",
                FontSize = 11,
                Foreground = EditorColor.FromRGB(148, 148, 148),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20),
            };
            compListPanel.Children.Add(noResultsText);
        }
    }

    /// <summary>
    /// Formats a component type name.
    /// </summary>
    /// <param name="name">Name of component type to format</param>
    /// <returns>Formatted component type name</returns>
    private static string FormatComponentName(string name)
    {
        string formatted = "";
        for (int i = 0; i < name.Length - 1; i++)
        {
            formatted += name[i];
            if (char.IsLower(name[i]) && char.IsUpper(name[i + 1])) formatted += ' ';
        }
        formatted += name[^1];
        return formatted.Replace("SDF", "SDF ");
    }

    private static List<Type> GetComponentTypes()
    {
        List<Type> componentTypes = [];
        foreach (Assembly? assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                IEnumerable<Type> types = assembly.GetTypes()
                    .Where(t => typeof(IComponent).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface && t != typeof(IComponent));
                componentTypes.AddRange(types);
            }
            catch (ReflectionTypeLoadException ex)
            {
                IEnumerable<Type> types = ex.Types // Get the types that were successfully loaded
                    .Where(t => t != null && typeof(IComponent).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface && t != typeof(IComponent))
                    .Cast<Type>();
                componentTypes.AddRange(types);
                Debug.Warning($"Could not load some component types from {assembly.FullName}");
            }
            catch (Exception ex)
            {
                Debug.Warning($"Error loading component types from {assembly.FullName}", ex);
            }
        }
        return componentTypes;
    }

    /// <summary>
    /// Has the properties window load the components for an entity.
    /// </summary>
    /// <param name="entityId">Entity to load components for</param>
    public static void LoadEntityComponents(uint entityId)
    {
        LastSelected = entityId;
        ValidatePropertiesWindows();
        foreach (PropertiesWindow? window in currentWindows) Dispatcher.UIThread.Post(() => window!.DisplayEntityComponents(entityId));
    }

    /// <summary>
    /// Displays data for a world into the properties editor.
    /// </summary>
    /// <param name="world">World data to pull from</param>
    public static void LoadWorldData(World? world)
    {
        LastSelected = uint.MaxValue;
        ValidatePropertiesWindows();
        foreach (PropertiesWindow? window in currentWindows) Dispatcher.UIThread.Post(() => window!.CreateWorldEditor(world));
    }

    /// <summary>
    /// Makes sure all properties windows in current list are active.
    /// </summary>
    private static void ValidatePropertiesWindows()
    {
        foreach (PropertiesWindow? window in currentWindows.ToArray()) // Dont forget to create iterator copy
        {
            if (window == null || !window.IsLoaded) currentWindows.Remove(window);
        }
    }

    /// <summary>
    /// Displays all components for an entity.
    /// </summary>
    /// <param name="entityId">Entity to display values for</param>
    private bool DisplayEntityComponents(uint entityId)
    {
        if (WorldManager.CurrentWorld == null || !W.EntityExists(entityId))
        {
            Debug.Warning("Could not load entity, world is null or entity does not exist");
            return false;
        }

        // Notify the refresh system of the selected entity
        PropertiesRefreshSystem.OnEntitySelected(entityId);
        propertiesPanel.Children.Clear();

        string entityName = W.TryGetEntityName(entityId);
        if (string.IsNullOrEmpty(entityName)) headerText.Text = $"Entity_{entityId}";
        else headerText.Text = entityName;
        curEntityId = entityId;

        List<IComponent> entityComps = W.GetAllComponents(entityId);
        foreach (IComponent component in entityComps)
            CreateComponentEditor(component.GetType(), component, entityId);
        return true;
    }

    public void CreateWorldEditor(World? curWorld)
    {
        LastSelected = uint.MaxValue;
        if (curWorld != null)
        {
            headerText.Text = curWorld.Name;
            propertiesPanel.Children.Clear();

            Border headerBorder = new Border
            {
                BorderThickness = new Thickness(0, 0, 1, 1),
                BorderBrush = EditorColor.FromRGB(17, 17, 17),
                Background = EditorColor.FromRGB(44, 44, 44),
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Margin = new Thickness(4, 8, 12, 0),
                Padding = new Thickness(4, 4),
            };
            DockPanel headerPanel = new DockPanel();
            MaterialIcon headerCompIcon = new MaterialIcon
            {
                Kind = MaterialIconKind.World,
                Width = 16,
                Height = 16,
                Margin = new Thickness(6, 2, 6, 2),
                Foreground = EditorColor.FromRGB(148, 148, 148),
                VerticalAlignment = VerticalAlignment.Center,
            };
            TextBlock componentName = new TextBlock
            {
                Text = curWorld.Name,
                FontSize = 14,
                Foreground = EditorColor.FromRGB(200, 200, 200),
                VerticalAlignment = VerticalAlignment.Center,
            };

            DockPanel.SetDock(headerCompIcon, Dock.Left);
            DockPanel.SetDock(componentName, Dock.Left);
            headerPanel.Children.Add(headerCompIcon);
            headerPanel.Children.Add(componentName);
            headerBorder.Child = headerPanel;
            propertiesPanel.Children.Add(headerBorder);

            // Create fields editor
            StackPanel fieldsPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(4, 0, 4, 0),
            };
            Border fieldsBorder = new Border
            {
                BorderThickness = new Thickness(0, 0, 1, 1),
                BorderBrush = EditorColor.FromRGB(10, 10, 10),
                Background = EditorColor.FromRGB(20, 20, 20),
                CornerRadius = new CornerRadius(0, 0, 4, 4),
                Margin = new Thickness(4, 0, 12, 0),
                Padding = new Thickness(8, 4, 4, 4),
            };
            fieldsBorder.PointerEntered += (_, _) =>
            {
                fieldsBorder.BorderThickness = new Thickness(0, 0, 2, 2);
                fieldsBorder.BorderBrush = EditorColor.FromRGB(12, 12, 12);
                fieldsBorder.Background = Background = EditorColor.FromRGB(24, 24, 24);
            };
            fieldsBorder.PointerExited += (_, _) =>
            {
                fieldsBorder.BorderThickness = new Thickness(0, 0, 1, 1);
                fieldsBorder.BorderBrush = EditorColor.FromRGB(10, 10, 10);
                fieldsBorder.Background = Background = EditorColor.FromRGB(20, 20, 20);
            };

            TextBlock entitiesText = new TextBlock
            {
                Text = $"Entities: {curWorld.entities.Count}",
                FontSize = 12,
                Foreground = EditorColor.FromRGB(200, 200, 200),
                VerticalAlignment = VerticalAlignment.Center,
            };
            TextBlock nextEntityText = new TextBlock
            {
                Text = $"Next Entity ID: {curWorld.NextEntityId}",
                FontSize = 12,
                Foreground = EditorColor.FromRGB(200, 200, 200),
                VerticalAlignment = VerticalAlignment.Center,
            };
            fieldsPanel.Children.Add(entitiesText);
            fieldsPanel.Children.Add(nextEntityText);

            fieldsBorder.Child = fieldsPanel;
            propertiesPanel.Children.Add(fieldsBorder);

            foreach (var (entityId, env) in W.QueryData<Environment>())
            {
                CreateComponentEditor(typeof(Environment), env, entityId);
                break; // Use first entity
            }
            foreach (var (entityId, sun) in W.QueryData<DirectionalLight>())
            {
                CreateComponentEditor(typeof(DirectionalLight), sun, entityId);
                break; // Use first entity
            }
            foreach (var (entityId, _, cam) in W.QueryData<Transform, Camera>())
            {
                if (entityId != EditorCamera.EditorCameraId)
                {
                    CreateComponentEditor(typeof(Camera), cam, entityId);
                    break; // Use first entity
                }
            }
        }
    }

    private void CreateComponentEditor(Type compType, IComponent instance, uint entityId)
    {
        Border headerBorder = new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 1),
            BorderBrush = EditorColor.FromRGB(17, 17, 17),
            Background = EditorColor.FromRGB(44, 44, 44),
            CornerRadius = new CornerRadius(4, 4, 0, 0),
            Margin = new Thickness(4, 8, 12, 0),
            Padding = new Thickness(4, 4),
        };
        DockPanel headerPanel = new DockPanel();
        MaterialIcon headerCompIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.DataMatrixScan,
            Width = 16,
            Height = 16,
            Margin = new Thickness(6, 2, 6, 2),
            Foreground = EditorColor.FromRGB(148, 148, 148),
            VerticalAlignment = VerticalAlignment.Center,
        };
        TextBlock componentName = new TextBlock
        {
            Text = compType.Name,
            FontSize = 14,
            Foreground = EditorColor.FromRGB(200, 200, 200),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Button removeButton = new Button
        {
            Content = new MaterialIcon
            {
                Kind = MaterialIconKind.Remove,
            },
            Padding = new Thickness(2, 1),
            HorizontalAlignment = HorizontalAlignment.Right,
            BorderThickness = new Thickness(0),
            Background = EditorColor.FromRGB(17, 17, 17),
            Foreground = EditorColor.FromRGB(200, 200, 200),
            FontSize = 11,
            CornerRadius = new CornerRadius(3),
        };
        removeButton.Click += (_, _) =>
        {
            W.RemoveComponent(entityId, compType);
            LoadEntityComponents(entityId);
        };

        DockPanel.SetDock(headerCompIcon, Dock.Left);
        DockPanel.SetDock(componentName, Dock.Left);
        DockPanel.SetDock(removeButton, Dock.Right);
        headerPanel.Children.Add(headerCompIcon);
        headerPanel.Children.Add(componentName);
        headerPanel.Children.Add(removeButton);
        headerBorder.Child = headerPanel;
        propertiesPanel.Children.Add(headerBorder);

        // Create fields editor
        StackPanel fieldsPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(4, 0, 4, 0),
            Tag = compType // Store component type for reference
        };

        Border fieldsBorder = new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 1),
            BorderBrush = EditorColor.FromRGB(10, 10, 10),
            Background = EditorColor.FromRGB(20, 20, 20),
            CornerRadius = new CornerRadius(0, 0, 4, 4),
            Margin = new Thickness(4, 0, 12, 0),
            Padding = new Thickness(8, 4, 4, 4),
            Tag = compType,
            Child = fieldsPanel
        };
        fieldsBorder.PointerEntered += (_, _) =>
        {
            fieldsBorder.BorderThickness = new Thickness(0, 0, 2, 2);
            fieldsBorder.BorderBrush = EditorColor.FromRGB(12, 12, 12);
            fieldsBorder.Background = Background = EditorColor.FromRGB(24, 24, 24);
        };
        fieldsBorder.PointerExited += (_, _) =>
        {
            fieldsBorder.BorderThickness = new Thickness(0, 0, 1, 1);
            fieldsBorder.BorderBrush = EditorColor.FromRGB(10, 10, 10);
            fieldsBorder.Background = Background = EditorColor.FromRGB(20, 20, 20);
        };

        FieldInfo[] fields = compType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            if (field.GetCustomAttribute<HideInEditorAttribute>() != null) continue;

            StackPanel? fieldEditor = CreateFieldEditor(field, instance, entityId);
            if (fieldEditor != null) fieldsPanel.Children.Add(fieldEditor);
        }

        fieldsBorder.Child = fieldsPanel;
        if (fieldsPanel.Children.Count > 0) propertiesPanel.Children.Add(fieldsBorder);
    }

    /// <summary>
    /// Refreshes only a specific component on the currently selected entity.
    /// </summary>
    /// <summary>
    /// Refreshes only a specific component on the currently selected entity.
    /// </summary>
    public void RefreshComponent(Type compType)
    {
        if (curEntityId == uint.MaxValue) return;
        if (WorldManager.CurrentWorld == null) return;
        if (!W.EntityExists(curEntityId)) return;

        // Get the FRESH component instance from the world
        IComponent? freshComponent = null;
        foreach (var comp in W.GetAllComponents(curEntityId))
        {
            if (comp.GetType() == compType)
            {
                freshComponent = comp;
                break;
            }
        }

        if (freshComponent == null) return;

        // Find the existing component UI in the properties panel
        foreach (var child in propertiesPanel.Children)
        {
            if (child is Border border && border.Tag is Type type && type == compType)
            {
                // Find the fields panel inside this component
                if (border.Child is StackPanel fieldsPanel)
                {
                    // Clear existing field editors
                    fieldsPanel.Children.Clear();

                    // Recreate field editors with updated values using the FRESH component
                    FieldInfo[] fields = compType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (FieldInfo field in fields)
                    {
                        if (field.GetCustomAttribute<HideInEditorAttribute>() != null) continue;

                        StackPanel? fieldEditor = CreateFieldEditor(field, freshComponent, curEntityId);
                        if (fieldEditor != null) fieldsPanel.Children.Add(fieldEditor);
                    }
                }
                break;
            }
        }
    }

    /// <summary>
    /// Creates an editor for a FieldInfo field type.
    /// </summary>
    /// <param name="field">Field to pull data from</param>
    /// <param name="component">Component this field resides on</param>
    /// <param name="entityId">Entity the field resides on</param>
    /// <returns>A StackPanel field editor object</returns>
    private static StackPanel? CreateFieldEditor(FieldInfo field, IComponent component, uint entityId)
    {
        Type fieldType = field.FieldType;
        object? fieldValue = field.GetValue(component);

        float topMargin = 0f;
        SpaceAttribute? spaceAttr = field.GetCustomAttribute<SpaceAttribute>();
        if (spaceAttr != null) topMargin = spaceAttr.Space;

        // Setup field panel
        StackPanel fieldPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            MinHeight = 20,
            Margin = new Thickness(0, topMargin, 0, 0),
        };

        // If header exists
        HeaderAttribute? headerAttr = field.GetCustomAttribute<HeaderAttribute>();
        StackPanel superFieldPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            MinHeight = 20,
            Margin = new Thickness(0, topMargin, 0, 0f),
        };
        if (headerAttr != null)
        {
            TextBlock headerText = new TextBlock
            {
                Text = headerAttr.Header,
                FontSize = 14,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0f, 12f, 0f, 6f),
            };
            fieldPanel.Margin = new Thickness(0);
            superFieldPanel.Children.Add(headerText);
            superFieldPanel.Children.Add(fieldPanel);
        }

        CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
        TextInfo textInfo = cultureInfo.TextInfo;
        string formattedFieldName = textInfo.ToTitleCase(FormattedFieldRegex().Replace(field.Name, "$1 $2"));

        // Create name label container
        StackPanel nameContainer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };
        TextBlock nameLabel = new TextBlock
        {
            Text = formattedFieldName,
            FontSize = 12,
            Foreground = Brushes.LightGray,
            VerticalAlignment = VerticalAlignment.Center,
        };
        nameContainer.Children.Add(nameLabel);
        fieldPanel.Children.Add(nameContainer);

        Control? editorControl = new Control();
        fieldPanel.Children.Add(editorControl);

        // Create context menu for this field
        ContextMenu fieldContextMenu = new ContextMenu
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            BorderBrush = EditorColor.FromRGB(128, 128, 128),
        };
        MenuItem resetMenuItem = new MenuItem
        {
            Header = "Reset to Default",
            Icon = new MaterialIcon
            {
                Kind = MaterialIconKind.Restore,
                Width = 16,
                Height = 16,
                Foreground = EditorColor.FromRGB(200, 140, 120),
            },
            Foreground = Brushes.White,
        };

        // Get default value from a fresh component instance
        object? defaultValue = GetDefaultFieldValue(component.GetType(), field.Name);

        resetMenuItem.Click += (s, e) =>
        {
            if (defaultValue != null)
            {
                field.SetValue(component, defaultValue);
                PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
            }
        };
        fieldContextMenu.Items.Add(resetMenuItem);
        fieldPanel.ContextMenu = fieldContextMenu;

        MinAttribute? minAttr = field.GetCustomAttribute<MinAttribute>();
        MaxAttribute? maxAttr = field.GetCustomAttribute<MaxAttribute>();
        RangeAttribute? rangeAttr = field.GetCustomAttribute<RangeAttribute>();
        if (fieldValue != null && fieldType == typeof(float))
        {
            float value = (float)fieldValue;
            if (rangeAttr != null)
            {
                StackPanel floatControl = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                };
                NumericUpDown floatBox = CreateFloatNumericBox(value, (f) =>
                {
                    field.SetValue(component, f);
                    PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
                }, false, minAttr != null ? minAttr.Min : -2000000000f, maxAttr != null ? maxAttr.Max : 2000000000f);
                StackPanel floatSlider = CreateFloatSlider(value, rangeAttr.Min, rangeAttr.Max, (f) =>
                {
                    field.SetValue(component, f);
                    floatBox.Value = (decimal)f;
                    PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
                });
                floatControl.Children.Add(floatSlider);
                floatControl.Children.Add(floatBox);
                editorControl = floatControl;
            }
            else editorControl = CreateFloatNumericBox(value, (f) =>
            {
                field.SetValue(component, f);
                PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
            }, true, minAttr != null ? minAttr.Min : -2000000000f, maxAttr != null ? maxAttr.Max : 2000000000f);
        }
        else if (fieldValue != null && fieldType == typeof(int))
        {
            int value = (int)fieldValue;
            if (rangeAttr != null)
            {
                StackPanel intControl = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                };
                NumericUpDown intBox = CreateIntegerNumericBox(value, (f) =>
                {
                    field.SetValue(component, f);
                    PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
                }, false, minAttr != null ? (int)minAttr.Min : int.MinValue, maxAttr != null ? (int)maxAttr.Max : int.MaxValue);
                StackPanel intSlider = CreateIntegerSlider(value, (int)rangeAttr.Min, (int)rangeAttr.Max, (i) =>
                {
                    field.SetValue(component, i);
                    intBox.Value = i;
                    PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
                });
                intControl.Children.Add(intSlider);
                intControl.Children.Add(intBox);
                editorControl = intControl;
            }
            else editorControl = CreateIntegerNumericBox(value, (f) =>
            {
                field.SetValue(component, f);
                PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
            }, true, minAttr != null ? (int)minAttr.Min : int.MinValue, maxAttr != null ? (int)maxAttr.Max : int.MaxValue);
        }
        else if (fieldValue != null && fieldType == typeof(string))
        {
            bool multiline = field.GetCustomAttribute<MultilineAttribute>() != null; // checks to see if multiline text field
            string value = (string)fieldValue;
            TextBox textBox = new TextBox
            {
                Text = value,
                FontSize = 12,
                AcceptsReturn = multiline,
                Background = EditorColor.FromRGB(32, 32, 32),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4, 2),
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            textBox.PropertyChanged += (s, e) =>
            {
                if (e.Property == TextBox.TextProperty)
                {
                    field.SetValue(component, textBox.Text);
                    PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
                }
            };
            editorControl = textBox;
        }
        else if (fieldValue != null && fieldType == typeof(bool))
        {
            bool value = (bool)fieldValue;
            CheckBox checkBox = new CheckBox
            {
                IsChecked = value,
                IsDefault = false,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            checkBox.IsCheckedChanged += (s, e) =>
            {
                field.SetValue(component, checkBox.IsChecked);
                PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
            };
            editorControl = checkBox;
        }
        else if (fieldValue != null && fieldType == typeof(float2))
        {
            float2 value = (float2)fieldValue;
            StackPanel vectorPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
            };

            NumericUpDown xBox = CreateFloatNumericBox(value.X, (val) =>
            {
                value.X = val;
                field.SetValue(component, value);
                PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
            });
            NumericUpDown yBox = CreateFloatNumericBox(value.Y, (val) =>
            {
                value.Y = val;
                field.SetValue(component, value);
                PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
            });

            vectorPanel.Children.Add(new TextBlock
            {
                Text = "X",
                Foreground = Brushes.LightGray,
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0),
            });
            vectorPanel.Children.Add(xBox);
            vectorPanel.Children.Add(new TextBlock
            {
                Text = "Y",
                Foreground = Brushes.LightGray,
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0),
            });
            vectorPanel.Children.Add(yBox);
            editorControl = vectorPanel;
        }
        else if (fieldValue != null && fieldType == typeof(float3))
        {
            float3 value = (float3)fieldValue;
            StackPanel vectorPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
            };

            NumericUpDown xBox = CreateFloatNumericBox(value.X, (val) =>
            {
                value.X = val;
                field.SetValue(component, value);
                PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
            });
            NumericUpDown yBox = CreateFloatNumericBox(value.Y, (val) =>
            {
                value.Y = val;
                field.SetValue(component, value);
                PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
            });
            NumericUpDown zBox = CreateFloatNumericBox(value.Z, (val) =>
            {
                value.Z = val;
                field.SetValue(component, value);
                PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
            });

            vectorPanel.Children.Add(new TextBlock {
                Text = "X",
                Foreground = Brushes.LightGray,
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0),
            });
            vectorPanel.Children.Add(xBox);
            vectorPanel.Children.Add(new TextBlock {
                Text = "Y",
                Foreground = Brushes.LightGray,
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0),
            });
            vectorPanel.Children.Add(yBox);
            vectorPanel.Children.Add(new TextBlock {
                Text = "Z",
                Foreground = Brushes.LightGray,
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0),
            });
            vectorPanel.Children.Add(zBox);
            editorControl = vectorPanel;
        }
        else if (fieldValue != null && fieldType == typeof(float4))
        {
            ColorAttribute? colorAttr = field.GetCustomAttribute<ColorAttribute>(); // This section tests for color or rotation editors
            RotationAttribute? rotAttr = field.GetCustomAttribute<RotationAttribute>();
            if (colorAttr != null) editorControl = CreateColorFieldEditor(field, component, colorAttr, entityId);
            else if (rotAttr != null) editorControl = CreateRotationFieldEditor(field, component, rotAttr, entityId);
            else
            {
                float4 value = (float4)fieldValue;
                StackPanel vectorPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                NumericUpDown xBox = CreateFloatNumericBox(value.X, (val) =>
                {
                    value.X = val;
                    field.SetValue(component, value);
                    PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
                });
                NumericUpDown yBox = CreateFloatNumericBox(value.Y, (val) =>
                {
                    value.Y = val;
                    field.SetValue(component, value);
                    PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
                });
                NumericUpDown zBox = CreateFloatNumericBox(value.Z, (val) =>
                {
                    value.Z = val;
                    field.SetValue(component, value);
                    PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
                });
                NumericUpDown wBox = CreateFloatNumericBox(value.W, (val) =>
                {
                    value.W = val;
                    field.SetValue(component, value);
                    PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
                });

                vectorPanel.Children.Add(new TextBlock
                {
                    Text = "X",
                    Foreground = Brushes.LightGray,
                    FontSize = 9,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 2, 0),
                });
                vectorPanel.Children.Add(xBox);
                vectorPanel.Children.Add(new TextBlock
                {
                    Text = "Y",
                    Foreground = Brushes.LightGray,
                    FontSize = 9,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 2, 0),
                });
                vectorPanel.Children.Add(yBox);
                vectorPanel.Children.Add(new TextBlock
                {
                    Text = "Z",
                    Foreground = Brushes.LightGray,
                    FontSize = 9,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 2, 0),
                });
                vectorPanel.Children.Add(zBox);
                vectorPanel.Children.Add(new TextBlock
                {
                    Text = "W",
                    Foreground = Brushes.LightGray,
                    FontSize = 9,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 2, 0),
                });
                vectorPanel.Children.Add(wBox);
                editorControl = vectorPanel;
            }
        }
        else if (fieldValue != null && fieldType == typeof(DateTime))
        {
            DateTime value = (DateTime)fieldValue;
            CalendarDatePicker dateTimePicker = new CalendarDatePicker
            {
                SelectedDate = value,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(4),
                FontSize = 11,
                Background = EditorColor.FromRGB(32, 32, 32),
                Foreground = Brushes.White,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            dateTimePicker.SelectedDateChanged += (s, e) =>
            {
                field.SetValue(component, dateTimePicker.SelectedDate);
                PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
            };
            editorControl = dateTimePicker;
        }
        else if (fieldValue != null && fieldType == typeof(float4x4))
        {
            float4x4 value = (float4x4)fieldValue;
            editorControl = CreateMatrixEditor(value, field, component, entityId);
        }
        else if (fieldType.IsEnum)
            editorControl = CreateEnumEditor(field, component, fieldType, fieldValue, entityId);
        else if (fieldType == typeof(AssetRef) || (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(AssetRef<>)))
            editorControl = CreateAssetRefEditor(field, component);

        ApplyTooltip(editorControl!, field);
        fieldPanel.Children.Add(editorControl!);

        // Add tooltip indicator if field has tooltip attribute
        TooltipAttribute? tooltipAttr = field.GetCustomAttribute<TooltipAttribute>();
        if (tooltipAttr != null)
        {
            MaterialIcon tooltipIcon = new MaterialIcon
            {
                Kind = MaterialIconKind.InformationOutline,
                Width = 12,
                Height = 12,
                Margin = new Thickness(4, 0, 0, 0),
                Foreground = EditorColor.FromRGB(148, 148, 148),
                VerticalAlignment = VerticalAlignment.Center,
            };
            ApplyTooltip(tooltipIcon, field);
            ApplyTooltip(nameLabel, field);
            fieldPanel.Children.Add(tooltipIcon);
        }

        if (headerAttr != null) return superFieldPanel;
        else return fieldPanel;
    }

    /// <summary>
    /// Creates a ComboBox editor for enum types.
    /// </summary>
    private static StackPanel CreateEnumEditor(FieldInfo field, IComponent component, Type enumType, object? currentValue, uint entityId)
    {
        StackPanel enumPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 4,
        };
        Array enumValues = Enum.GetValues(enumType);
        ComboBox enumComboBox = new ComboBox
        {
            MinWidth = 100,
            MaxWidth = 200,
            Height = 20,
            FontSize = 11,
            Background = EditorColor.FromRGB(32, 32, 32),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(4, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            PlaceholderText = "Select value...",
        };

        // Create enum items
        List<EnumItem> items = [];
        int selectedIndex = 0;
        int index = 0;
        foreach (var enumValue in enumValues)
        {
            string displayName = FormatEnumName(enumValue.ToString()!);
            items.Add(new EnumItem
            {
                Value = enumValue,
                DisplayName = displayName,
            });
            if (currentValue != null && enumValue.Equals(currentValue)) selectedIndex = index;
            index++;
        }
        enumComboBox.ItemsSource = items;
        enumComboBox.SelectedIndex = selectedIndex;

        // Build item template
        enumComboBox.ItemTemplate = new FuncDataTemplate<EnumItem>((item, _) =>
        {
            DockPanel itemPanel = new DockPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0),
            };

            // Optional: Add colored square for special enums (like flags)
            /*if (IsFlagsEnum(enumType))
            {
                Border flagIndicator = new Border
                {
                    Width = 12,
                    Height = 12,
                    CornerRadius = new CornerRadius(2),
                    Background = GetEnumColor(item.Value),
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(flagIndicator, Dock.Left);
                itemPanel.Children.Add(flagIndicator);
            }*/

            // Main text
            TextBlock nameText = new TextBlock
            {
                Text = item.DisplayName,
                FontSize = 11,
                FontWeight = FontWeight.Medium,
                Foreground = Brushes.White,
            };
            itemPanel.Children.Add(nameText);
            return itemPanel;
        });

        enumComboBox.SelectionChanged += (s, e) =>
        {
            if (enumComboBox.SelectedItem is EnumItem selectedItem)
            {
                try
                {
                    field.SetValue(component, selectedItem.Value);
                    PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
                }
                catch (Exception ex) { Debug.Error($"Failed to set enum value for {field.Name}", ex); }
            }
        };
        enumPanel.Children.Add(enumComboBox);
        return enumPanel;
    }

    /// <summary>
    /// Scaffolding class for enum editor.
    /// </summary>
    private class EnumItem
    {
        public object Value { get; set; } = null!;
        public string DisplayName { get; set; } = string.Empty;
        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Formats enum name with spaces between words.
    /// </summary>
    private static string FormatEnumName(string enumName)
    {
        if (string.IsNullOrEmpty(enumName)) return enumName;
        StringBuilder? result = new StringBuilder();
        result.Append(char.ToUpperInvariant(enumName[0]));

        for (int i = 1; i < enumName.Length; i++)
        {
            if ((char.IsUpper(enumName[i]) || char.IsDigit(enumName[i])) && !char.IsUpper(enumName[i - 1])) result.Append(' ');
            result.Append(enumName[i]);
        }
        return result.ToString();
    }

    private static Button CreateMatrixEditor(float4x4 initialValue, FieldInfo field, object component, uint entityId)
    {
        Button matrixButton = new Button
        {
            Content = CreateMatrixButtonContent(),
            Padding = new Thickness(8, 4),
            BorderBrush = EditorColor.FromRGB(45, 45, 45),
            BorderThickness = new Thickness(0),
            Background = EditorColor.FromRGB(32, 32, 32),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4),
            HorizontalContentAlignment = HorizontalAlignment.Left,
        };
        StackPanel mainPanel = new StackPanel
        {
            Spacing = 8,
        };
        Flyout flyout = new Flyout
        {
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            ShowMode = FlyoutShowMode.Standard,
            Content = mainPanel,
        };

        // Header
        DockPanel headerPanel = new DockPanel();
        TextBlock headerText = new TextBlock
        {
            Text = "Edit Matrix",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 4),
            VerticalAlignment = VerticalAlignment.Center,
        };
        DockPanel.SetDock(headerText, Dock.Left);
        headerPanel.Children.Add(headerText);
        Button closeButton = new Button
        {
            Content = new MaterialIcon
            {
                Kind = MaterialIconKind.Close,
            },
            Padding = new Thickness(2, 1),
            HorizontalAlignment = HorizontalAlignment.Right,
            BorderThickness = new Thickness(0),
            Background = EditorColor.FromRGB(17, 17, 17),
            Foreground = EditorColor.FromRGB(200, 200, 200),
            FontSize = 11,
            CornerRadius = new CornerRadius(3),
        };
        closeButton.Click += (s, e) => flyout.Hide();
        DockPanel.SetDock(closeButton, Dock.Right);
        headerPanel.Children.Add(closeButton);
        mainPanel.Children.Add(headerPanel);

        // Create 4x4 grid of float box controls
        Border gridBorder = new Border
        {
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(3),
            Background = EditorColor.FromRGB(52, 52, 52),
            Padding = new Thickness(2),
        };
        StackPanel gridContainer = new StackPanel
        {
            Spacing = 1,
        };
        StackPanel columnHeaders = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(20, 0, 0, 2),
        };
        for (int col = 0; col < 4; col++)
        {
            columnHeaders.Children.Add(new Border
            {
                Child = new TextBlock
                {
                    Text = $"C{col + 1}",
                    Foreground = EditorColor.FromRGB(200, 200, 200),
                    FontSize = 10,
                    FontWeight = FontWeight.Medium,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                Width = 32,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(2, 0, 2, 0),
            });
        }

        gridContainer.Children.Add(columnHeaders);
        NumericUpDown[,] matrixBoxes = new NumericUpDown[4, 4];

        // Create rows and fields
        for (int row = 0; row < 4; row++)
        {
            DockPanel rowPanel = new DockPanel();
            Border rowHeader = new Border
            {
                Child = new TextBlock
                {
                    Text = $"R{row + 1}",
                    Foreground = EditorColor.FromRGB(200, 200, 200),
                    FontSize = 10,
                    FontWeight = FontWeight.Medium,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                },
                Width = 20,
                VerticalAlignment = VerticalAlignment.Center,
            };
            DockPanel.SetDock(rowHeader, Dock.Left);
            rowPanel.Children.Add(rowHeader);
            StackPanel rowCells = new StackPanel
            {
                Orientation = Orientation.Horizontal,
            };

            for (int col = 0; col < 4; col++)
            {
                int r = row, c = col;
                float initialCellValue = initialValue.GetVal(r, c);

                NumericUpDown numBox = CreateFloatNumericBox(initialCellValue, (val) =>
                {
                    float4x4 currentMatrix = (float4x4)field.GetValue(component)!;
                    currentMatrix.SetVal(r, c, val);
                    field.SetValue(component, currentMatrix);
                    PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
                });
                numBox.Width = 24;
                numBox.Height = 20;
                numBox.Margin = new Thickness(2);

                matrixBoxes[row, col] = numBox;
                rowCells.Children.Add(numBox);
            }

            rowPanel.Children.Add(rowCells);
            gridContainer.Children.Add(rowPanel);
        }

        gridBorder.Child = gridContainer;
        mainPanel.Children.Add(gridBorder);

        // Attach flyout to button
        matrixButton.Click += (_, _) => flyout.ShowAt(matrixButton);
        return matrixButton;
    }

    private static StackPanel CreateMatrixButtonContent()
    {
        StackPanel previewPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center
        };
        previewPanel.Children.Add(new MaterialIcon
        {
            Kind = MaterialIconKind.Matrix,
            Width = 16,
            Height = 16,
            Foreground = EditorColor.FromRGB(100, 200, 255),
            VerticalAlignment = VerticalAlignment.Center,
        });

        // Matrix preview text
        StackPanel textPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2
        };
        textPanel.Children.Add(new TextBlock
        {
            Text = "4x4 Matrix",
            FontSize = 11,
            FontWeight = FontWeight.Medium,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = EditorColor.FromRGB(220, 220, 220),
        });

        previewPanel.Children.Add(textPanel);
        previewPanel.Children.Add(new MaterialIcon
        {
            Kind = MaterialIconKind.ChevronRight,
            Width = 12,
            Height = 12,
            Foreground = Brushes.Gray,
            Margin = new Thickness(2, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        });
        return previewPanel;
    }

    private static StackPanel? CreateColorFieldEditor(FieldInfo field, IComponent component, ColorAttribute colorAttr, uint entityId)
    {
        object? fieldValue = field.GetValue(component);
        if (fieldValue == null) return null;
        float4 colorValue = (float4)fieldValue;

        StackPanel fieldPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            MinHeight = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ColorPicker colorPicker = new ColorPicker
        {
            Width = 150,
            Height = 20,
            Color = EditorColor.FromColor(colorValue).Color,
            Background = EditorColor.FromRGB(32, 32, 32),
            IsAlphaVisible = colorAttr.ShowAlpha,
            IsColorSpectrumVisible = true, // Shows as a simple color button
            IsColorPreviewVisible = true,
            IsColorComponentsVisible = true,
            IsComponentTextInputVisible = false,
            IsComponentSliderVisible = true,
            IsAlphaEnabled = colorAttr.ShowAlpha,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            IsHexInputVisible = true,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 12,
        };
        colorPicker.ColorChanged += (s, e) => // Update on color change
        {
            Color selectedColor = colorPicker.Color;
            float4 newColor = new float4(selectedColor.R / 255f, selectedColor.G / 255f, selectedColor.B / 255f, selectedColor.A / 255f);
            field.SetValue(component, newColor); // Update component
            PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
        };
        fieldPanel.Children.Add(colorPicker);
        return fieldPanel;
    }

    private static StackPanel? CreateRotationFieldEditor(FieldInfo field, IComponent component, RotationAttribute rotAttr, uint entityId)
    {
        object? fieldValue = field.GetValue(component);
        if (fieldValue == null) return null;
        float4 quaternionValue = (float4)fieldValue;

        float3 eulerValue = Math.QuaternionToEuler(quaternionValue);
        if (rotAttr.Degrees)
            eulerValue = new float3(Math.Rad2Deg * eulerValue.X, Math.Rad2Deg * eulerValue.Y, Math.Rad2Deg * eulerValue.Z);

        StackPanel eulerRotationPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        NumericUpDown xBox = CreateFloatNumericBox(eulerValue.X, (val) =>
        {
            if (rotAttr.Degrees) val *= Math.Deg2Rad;
            eulerValue.X = val;
            field.SetValue(component, Math.EulerToQuaternion(eulerValue));
            PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
        });
        NumericUpDown yBox = CreateFloatNumericBox(eulerValue.Y, (val) =>
        {
            if (rotAttr.Degrees) val *= Math.Deg2Rad;
            eulerValue.Y = val;
            field.SetValue(component, Math.EulerToQuaternion(eulerValue));
            PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
        });
        NumericUpDown zBox = CreateFloatNumericBox(eulerValue.Z, (val) =>
        {
            if (rotAttr.Degrees) val *= Math.Deg2Rad;
            eulerValue.Z = val;
            field.SetValue(component, Math.EulerToQuaternion(eulerValue));
            PropertiesRefreshSystem.OnFieldChanged(entityId, component.GetType().Name);
        });
        MaterialIcon rotateTypeIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Pi,
            Foreground = Brushes.LightGray,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0),
        };

        if (rotAttr.Degrees)
        {
            rotateTypeIcon.Kind = MaterialIconKind.Rotate360;
            xBox.Increment = 5;
            yBox.Increment = 5;
            zBox.Increment = 5;
        }

        eulerRotationPanel.Children.Add(new TextBlock
        {
            Text = "X",
            Foreground = Brushes.LightGray,
            FontSize = 9,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0),
        });
        eulerRotationPanel.Children.Add(xBox);
        eulerRotationPanel.Children.Add(new TextBlock
        {
            Text = "Y",
            Foreground = Brushes.LightGray,
            FontSize = 9,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0),
        });
        eulerRotationPanel.Children.Add(yBox);
        eulerRotationPanel.Children.Add(new TextBlock
        {
            Text = "Z",
            Foreground = Brushes.LightGray,
            FontSize = 9,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0),
        });
        eulerRotationPanel.Children.Add(zBox);
        eulerRotationPanel.Children.Add(rotateTypeIcon);
        return eulerRotationPanel;
    }

    private static StackPanel CreateFloatSlider(float initialVal, float min, float max, Action<float> onValueChanged)
    {
        StackPanel sliderPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };
        Slider slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            Value = initialVal,
            Width = 100,
            BackgroundSizing = BackgroundSizing.OuterBorderEdge,
            Height = 20,
            VerticalAlignment = VerticalAlignment.Center,
            Background = EditorColor.FromRGB(32, 32, 32),
            Foreground = EditorColor.FromRGB(100, 100, 100),
        };
        slider.ValueChanged += (s, e) =>
        {
            float newValue = (float)slider.Value;
            onValueChanged(newValue);
        };
        sliderPanel.Children.Add(slider);
        return sliderPanel;
    }

    private static StackPanel CreateIntegerSlider(int initialVal, int min, int max, Action<int> onValueChanged)
    {
        StackPanel sliderPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };
        Slider slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            Value = initialVal,
            Width = 100,
            BackgroundSizing = BackgroundSizing.OuterBorderEdge,
            Height = 20,
            VerticalAlignment = VerticalAlignment.Center,
            Background = EditorColor.FromRGB(32, 32, 32),
            Foreground = EditorColor.FromRGB(100, 100, 100),
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
        };
        slider.ValueChanged += (s, e) =>
        {
            int newValue = (int)slider.Value;
            onValueChanged(newValue);
        };
        sliderPanel.Children.Add(slider);
        return sliderPanel;
    }

    /// <summary>
    /// Creates a numeric float field box with optional addons.
    /// </summary>
    /// <param name="initialVal">Starting value</param>
    /// <param name="onValueChanged">Called when the value of this is changed</param>
    /// <param name="hasSpinner">Has a spinner next to the field</param>
    /// <param name="min">Minimum float value</param>
    /// <param name="max">Maximum float value</param>
    /// <returns>NumericUpDown specialized stylized float box</returns>
    private static NumericUpDown CreateFloatNumericBox(float initialVal, Action<float> onValueChanged,
        bool hasSpinner = false, float min = -2000000000f, float max = 2000000000f)
    {
        NumericUpDown numericBox = new NumericUpDown
        {
            Value = (decimal)initialVal,
            Minimum = (decimal)min,
            Maximum = (decimal)max,
            Increment = (decimal)Math.Max(initialVal / 10f, 0.1f),
            FontSize = 11,
            AllowSpin = true,
            ParsingNumberStyle = NumberStyles.Float,
            Background = EditorColor.FromRGB(32, 32, 32),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            FormatString = "0.##",
            ShowButtonSpinner = hasSpinner,
        };
        numericBox.LostFocus += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(numericBox.Text) || !decimal.TryParse(numericBox.Text, out _))
                numericBox.Value = 0;
        };
        numericBox.ValueChanged += (s, e) =>
        {
            try
            {
                if (numericBox.Value.HasValue)
                    onValueChanged((float)(double)numericBox.Value);
            }
            catch (Exception ex) { Debug.Error("Numeric Box Error", ex); }
        };
        return numericBox;
    }

    /// <summary>
    /// Creates a numeric integer field box with optional addons.
    /// </summary>
    /// <param name="initialVal">Starting value</param>
    /// <param name="onValueChanged">Called when the value of this is changed</param>
    /// <param name="hasSpinner">Has a spinner next to the field</param>
    /// <param name="min">Minimum integer value</param>
    /// <param name="max">Maximum integer value</param>
    /// <returns>NumericUpDown specialized stylized integer box</returns>
    private static NumericUpDown CreateIntegerNumericBox(int initialVal, Action<int> onValueChanged,
        bool hasSpinner = false, int min = int.MinValue, int max = int.MaxValue)
    {
        NumericUpDown numericBox = new NumericUpDown
        {
            Value = initialVal,
            Minimum = min,
            Maximum = max,
            Increment = 1,
            FontSize = 11,
            AllowSpin = true,
            ParsingNumberStyle = NumberStyles.Integer,
            Background = EditorColor.FromRGB(32, 32, 32),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            ShowButtonSpinner = hasSpinner,
        };
        numericBox.LostFocus += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(numericBox.Text) || !decimal.TryParse(numericBox.Text, out _))
                numericBox.Value = 0;
        };
        numericBox.ValueChanged += (s, e) =>
        {
            try
            {
                if (numericBox.Value.HasValue)
                    onValueChanged((int)numericBox.Value);
            }
            catch (Exception ex) { Debug.Error("Numeric Box Error", ex); }
        };
        return numericBox;
    }

    /// <summary>
    /// Gets the default value of a field from a newly created instance of the component.
    /// </summary>
    /// <param name="compType">Type of the component</param>
    /// <param name="fieldName">Name of the field</param>
    /// <returns>The default value, or null if not found</returns>
    private static object? GetDefaultFieldValue(Type compType, string fieldName)
    {
        try
        {
            // Create a fresh instance of the component to get default values
            IComponent? freshInstance = (IComponent?)Activator.CreateInstance(compType);
            if (freshInstance != null)
            {
                FieldInfo? field = compType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
                return field?.GetValue(freshInstance);
            }
        }
        catch (Exception ex)
        {
            Debug.Warning($"Failed to get default value for {compType.Name}.{fieldName}", ex);
        }
        return null;
    }

    #region AssetReferences

    private static Control CreateAssetRefEditor(FieldInfo field, IComponent component)
    {
        // Get current value
        object? fieldValue = field.GetValue(component);
        if (fieldValue == null) return new TextBlock { Text = "Error" };

        // Determine asset type
        AssetType expectedType = GetExpectedTypeFromField(field, fieldValue);
        string currentId = GetAssetId(fieldValue);

        // Get current asset name
        string currentName = "None";
        if (!string.IsNullOrEmpty(currentId))
        {
            AssetMetadata? metadata = AssetDatabase.GetAssetMetadataByID(currentId);
            currentName = metadata != null ? Path.GetFileNameWithoutExtension(metadata.FileName) : "Missing";
        }

        StackPanel buttonContentPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
        };
        TextBlock assetRefButtonText = new TextBlock
        {
            Text = currentName,
            FontSize = 12,
            Foreground = EditorColor.FromRGB(200, 200, 200),
        };
        buttonContentPanel.Children.Add(EditorUI.CreateAssetTypeIcon(expectedType, 12));
        buttonContentPanel.Children.Add(assetRefButtonText);
        Button assetRefButton = new Button
        {
            Content = buttonContentPanel,
            Background = EditorColor.FromRGB(17, 17, 17),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 4),
            MinWidth = 150,
            HorizontalContentAlignment = HorizontalAlignment.Left,
        };

        // Flyout with list
        Flyout flyout = new Flyout();
        assetRefButton.Click += (_, _) =>
        {
            StackPanel panel = new StackPanel
            {
                MinWidth = 200,
            };

            // None option
            Button noneBtn = new Button
            {
                Content = "None",
                FontSize = 10,
                Background = EditorColor.FromRGB(10, 10, 10),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(1),
            };
            noneBtn.Click += (_, _) =>
            {
                SetAssetValue(field, component, null);
                assetRefButtonText.Text = "None";
                flyout.Hide();
            };
            panel.Children.Add(noneBtn);

            // Asset options
            foreach (AssetMetadata? asset in AssetDatabase.GetAssetsByType(expectedType))
            {
                if (asset == null) continue;
                Button assetBtn = new Button
                {
                    Content = Path.GetFileNameWithoutExtension(asset.FileName),
                    FontSize = 10,
                    Background = EditorColor.FromRGB(10, 10, 10),
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(1),
                };
                assetBtn.Click += (_, _) =>
                {
                    SetAssetValue(field, component, asset.ID);
                    assetRefButtonText.Text = $"{Path.GetFileNameWithoutExtension(asset.FileName)}";
                    flyout.Hide();
                };
                panel.Children.Add(assetBtn);
            }

            flyout.Content = panel;
            flyout.ShowAt(assetRefButton);
        };
        return assetRefButton;
    }

    private static AssetType GetExpectedTypeFromField(FieldInfo field, object fieldValue)
    {
        Type fieldType = field.FieldType;
        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(AssetRef<>))
        {
            Type assetType = fieldType.GetGenericArguments()[0];
            return AssetDatabase.GetAssetType(assetType);
        }
        else if (fieldType == typeof(AssetRef))
        {
            PropertyInfo? typeProp = fieldType.GetProperty("ExpectedType");
            return (AssetType)(typeProp?.GetValue(fieldValue) ?? AssetType.None);
        }
        return AssetType.None;
    }

    private static string GetAssetId(object fieldValue) => fieldValue.GetType().GetProperty("ID")?.GetValue(fieldValue) as string ?? "";

    private static void SetAssetValue(FieldInfo field, IComponent component, string? assetId)
    {
        Type fieldType = field.FieldType;
        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(AssetRef<>))
        {
            object? newValue = Activator.CreateInstance(fieldType, assetId ?? string.Empty);
            field.SetValue(component, newValue);
        }
        else if (fieldType == typeof(AssetRef))
        {
            object? current = field.GetValue(component);
            AssetType expectedType = AssetType.None;
            if (current != null)
            {
                PropertyInfo? typeProp = fieldType.GetProperty("ExpectedType");
                expectedType = (AssetType)(typeProp?.GetValue(current) ?? AssetType.None);
            }
            object? newValue = Activator.CreateInstance(fieldType, assetId ?? string.Empty, expectedType);
            field.SetValue(component, newValue);
        }
    }

    #endregion AssetReferences

    /// <summary>
    /// Applies a tooltip to a control if the field has a TooltipAttribute.
    /// </summary>
    private static void ApplyTooltip(Control control, FieldInfo field)
    {
        TooltipAttribute? tooltipAttr = field.GetCustomAttribute<TooltipAttribute>();
        if (tooltipAttr != null)
        {
            StackPanel tooltipContent = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Margin = new Thickness(2),
            };
            tooltipContent.Children.Add(new MaterialIcon
            {
                Kind = MaterialIconKind.InformationOutline,
                Width = 14,
                Height = 14,
                Foreground = EditorColor.FromRGB(148, 148, 148),
                VerticalAlignment = VerticalAlignment.Center,
            });
            tooltipContent.Children.Add(new TextBlock
            {
                Text = tooltipAttr.Tooltip,
                FontSize = 11,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
            });
            ToolTip.SetTip(control, tooltipContent);
            ToolTip.SetPlacement(control, PlacementMode.Top);
            ToolTip.SetShowDelay(control, 400);
            ToolTip.SetVerticalOffset(control, 4);
        }
    }

    [GeneratedRegex(@"(\p{Ll})(\p{Lu})")]
    private static partial Regex FormattedFieldRegex();
}