using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DivisionEngine.Editor;

public partial class PropertiesWindow : EditorWindow
{
    private static PropertiesWindow? current;

    private readonly StackPanel propertiesPanel;
    private readonly ScrollViewer scrollViewer;
    private readonly TextBlock headerText;

    private uint curEntityId;

    public PropertiesWindow()
    {
        InitializeComponent();

        propertiesPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 5,
            Margin = new Thickness(5)
        };
        scrollViewer = new ScrollViewer
        {
            Content = propertiesPanel,
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
            Background = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
            Height = 1,
            Margin = new Thickness(0, 0, 0, 5)
        });
        mainPanel.Children.Add(scrollViewer);
        this.FindControl<Border>("MainBorder")!.Child = mainPanel;

        current = this;
    }

    /// <summary>
    /// Make the properties window display all entityIds
    /// </summary>
    /// <param name="entityId"></param>
    public static void LoadEntityComponents(uint entityId) => current!.SetupPropertiesForEntity(entityId);

    public bool SetupPropertiesForEntity(uint entityId)
    {
        if (WorldManager.CurrentWorld == null || !W.EntityExists(entityId)) return false;
        propertiesPanel.Children.Clear();

        string entityName = W.TryGetEntityName(entityId);
        if (string.IsNullOrEmpty(entityName)) headerText.Text = $"Entity_{entityId}";
        else headerText.Text = entityName;

            curEntityId = entityId;
        Dispatcher.UIThread.Post(() => DisplayEntityComponents(entityId));
        return true;
    }

    private void DisplayEntityComponents(uint entityId)
    {
        List<IComponent> entityComps = W.GetAllComponents(entityId);
        foreach (IComponent component in entityComps)
            CreateComponentEditor(component.GetType(), component, entityId);
    }

    private void CreateComponentEditor(Type compType, IComponent instance, uint entityId)
    {
        Border headerBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(34, 34, 34)),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(0, 2),
            Padding = new Thickness(6, 4)
        };
        StackPanel headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5
        };
        TextBlock componentName = new TextBlock
        {
            Text = compType.Name,
            FontSize = 12,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };

        headerPanel.Children.Add(componentName);
        headerBorder.Child = headerPanel;

        propertiesPanel.Children.Add(headerBorder);

        // Create fields editor
        StackPanel fieldsPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 3,
            Margin = new Thickness(10, 5)
        };

        /*var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.IsInitOnly) continue; // Skip readonly fields

            var fieldEditor = CreateFieldEditor(field, instance, entityId);
            if (fieldEditor != null)
            {
                fieldsPanel.Children.Add(fieldEditor);
            }
        }*/

        if (fieldsPanel.Children.Count > 0)
        {
            propertiesPanel.Children.Add(fieldsPanel);
        }
    }

    public void SetupPropertiesForWorld(World world)
    {

    }
}