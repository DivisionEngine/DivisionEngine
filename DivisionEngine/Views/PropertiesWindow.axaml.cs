using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DivisionEngine.Components.FieldAttributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Math = DivisionEngine.MathLib.Math;

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
        header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Background = EditorColor.FromRGB(28, 28, 28),
            VerticalAlignment = VerticalAlignment.Top
        };
        headerText = new TextBlock
        {
            Text = "No Selection",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        header.Children.Add(headerText);

        StackPanel mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0
        };

        mainPanel.Children.Add(header);
        mainPanel.Children.Add(new Border
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            Height = 1
        });
        mainPanel.Children.Add(scrollViewer);
        this.FindControl<Border>("MainBorder")!.Child = mainPanel;

        currentWindows.Add(this);
    }

    /// <summary>
    /// Has the properties window load the components for an entity.
    /// </summary>
    /// <param name="entityId">Entity to load components for</param>
    public static void LoadEntityComponents(uint entityId)
    {
        ValidatePropertiesWindows();
        foreach (PropertiesWindow? window in currentWindows)
            window!.SetupPropertiesForEntity(entityId);
    }

    /// <summary>
    /// Makes sure all properties windows in current list are active.
    /// </summary>
    private static void ValidatePropertiesWindows()
    {
        foreach (PropertiesWindow? window in currentWindows.ToArray()) // Dont forget to create iterator copy
        {
            if (window == null || !window.IsLoaded)
                currentWindows.Remove(window);
        }
    }

    private bool SetupPropertiesForEntity(uint entityId)
    {
        if (WorldManager.CurrentWorld == null || !W.EntityExists(entityId))
        {
            Debug.Warning("Could not load entity, world is null or entity does not exist");
            return false;
        }
        propertiesPanel.Children.Clear();

        string entityName = W.TryGetEntityName(entityId);
        if (string.IsNullOrEmpty(entityName)) headerText.Text = $"Entity_{entityId}";
        else headerText.Text = entityName;

        curEntityId = entityId;
        Dispatcher.UIThread.Post(() => DisplayEntityComponents(entityId));
        return true;
    }

    /// <summary>
    /// Displays all components for an entity.
    /// </summary>
    /// <param name="entityId">Entity to display values for</param>
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
            BorderThickness = new Thickness(2),
            BorderBrush = EditorColor.FromRGB(68, 68, 68),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(4, 4)
        };
        StackPanel headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5
        };
        TextBlock componentName = new TextBlock
        {
            Text = compType.Name,
            FontSize = 14,
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
            Margin = new Thickness(4, 0, 4, 0)
        };
        Border fieldsBorder = new Border
        {
            BorderThickness = new Thickness(2),
            BorderBrush = EditorColor.FromRGB(17, 17, 17),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 0, 2),
        };

        FieldInfo[] fields = compType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.IsInitOnly) continue; // readonly field, implement these in the future
            Control? fieldEditor = CreateFieldEditor(field, instance, entityId);
            if (fieldEditor != null) fieldsPanel.Children.Add(fieldEditor);
        }

        fieldsBorder.Child = fieldsPanel;
        if (fieldsPanel.Children.Count > 0) propertiesPanel.Children.Add(fieldsBorder);
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
        var fieldValue = field.GetValue(component);

        // Setup field panel

        StackPanel fieldPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            MinHeight = 20,
            Margin = new Thickness(0, 0)
        };

        CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
        TextInfo textInfo = cultureInfo.TextInfo;
        string formattedFieldName = textInfo.ToTitleCase(Regex.Replace(field.Name, @"(\p{Ll})(\p{Lu})", "$1 $2"));

        TextBlock nameLabel = new TextBlock
        {
            Text = formattedFieldName,
            FontSize = 12,
            Foreground = Brushes.LightGray,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        };

        fieldPanel.Children.Add(nameLabel);
        Control? editorControl = new Control();

        // Check each type of field editor value possible

        if (fieldValue != null && fieldType == typeof(float))
        {
            float value = (float)fieldValue;
            editorControl = CreateFloatNumericBox(value, (f) => {
                field.SetValue(component, f);
            },
            true);
        }
        else if (fieldValue != null && fieldType == typeof(int))
        {
            int value = (int)fieldValue;
            editorControl = CreateIntegerNumericBox(value, (f) => {
                field.SetValue(component, f);
            },
            true);
        }
        else if (fieldValue != null && fieldType == typeof(string))
        {
            string value = (string)fieldValue;
            TextBox textBox = new TextBox
            {
                Text = value,
                FontSize = 12,
                Background = EditorColor.FromRGB(28, 28, 28),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4, 2),
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            textBox.PropertyChanged += (s, e) =>
            {
                if (e.Property == TextBox.TextProperty) field.SetValue(component, textBox.Text);
            };
            editorControl = textBox;
        }
        else if (fieldValue != null && fieldType == typeof(bool))
        {
            bool value = (bool)fieldValue;
            CheckBox textBox = new CheckBox
            {
                IsChecked = value,
                IsDefault = false,
                //Background = new SolidColorBrush(Color.FromRgb(17, 17, 17)),
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            textBox.IsCheckedChanged += (s, e) =>
            {
                field.SetValue(component, textBox.IsChecked);
            };
            editorControl = textBox;
        }
        else if (fieldValue != null && fieldType == typeof(float2))
        {
            float2 value = (float2)fieldValue;
            StackPanel vectorPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            NumericUpDown xBox = CreateFloatNumericBox(value.X, (val) => { value.X = val; field.SetValue(component, value); });
            NumericUpDown yBox = CreateFloatNumericBox(value.Y, (val) => { value.Y = val; field.SetValue(component, value); });

            vectorPanel.Children.Add(new TextBlock
            {
                Text = "X",
                Foreground = Brushes.LightGray,
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0)
            });
            vectorPanel.Children.Add(xBox);
            vectorPanel.Children.Add(new TextBlock
            {
                Text = "Y",
                Foreground = Brushes.LightGray,
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0)
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
                VerticalAlignment = VerticalAlignment.Center
            };

            NumericUpDown xBox = CreateFloatNumericBox(value.X, (val) => { value.X = val; field.SetValue(component, value); });
            NumericUpDown yBox = CreateFloatNumericBox(value.Y, (val) => { value.Y = val; field.SetValue(component, value); });
            NumericUpDown zBox = CreateFloatNumericBox(value.Z, (val) => { value.Z = val; field.SetValue(component, value); });

            vectorPanel.Children.Add(new TextBlock {
                Text = "X",
                Foreground = Brushes.LightGray,
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0)
            });
            vectorPanel.Children.Add(xBox);
            vectorPanel.Children.Add(new TextBlock {
                Text = "Y",
                Foreground = Brushes.LightGray,
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0)
            });
            vectorPanel.Children.Add(yBox);
            vectorPanel.Children.Add(new TextBlock {
                Text = "Z",
                Foreground = Brushes.LightGray,
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0)
            });
            vectorPanel.Children.Add(zBox);

            editorControl = vectorPanel;
        }
        else if (fieldValue != null && fieldType == typeof(float4))
        {
            ColorAttribute? colorAttr = field.GetCustomAttribute<ColorAttribute>();
            RotationAttribute? rotAttr = field.GetCustomAttribute<RotationAttribute>();
            if (colorAttr != null) // Check if this float4 is a color
                editorControl = CreateColorFieldEditor(field, component, colorAttr);
            else if (rotAttr != null) // Check if this float4 is a quaternion rotation
                editorControl = CreateRotationFieldEditor(field, component, rotAttr);
            else
            {
                float4 value = (float4)fieldValue;
                StackPanel vectorPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                NumericUpDown xBox = CreateFloatNumericBox(value.X, (val) => { value.X = val; field.SetValue(component, value); });
                NumericUpDown yBox = CreateFloatNumericBox(value.Y, (val) => { value.Y = val; field.SetValue(component, value); });
                NumericUpDown zBox = CreateFloatNumericBox(value.Z, (val) => { value.Z = val; field.SetValue(component, value); });
                NumericUpDown wBox = CreateFloatNumericBox(value.W, (val) => { value.W = val; field.SetValue(component, value); });

                vectorPanel.Children.Add(new TextBlock
                {
                    Text = "X",
                    Foreground = Brushes.LightGray,
                    FontSize = 9,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 2, 0)
                });
                vectorPanel.Children.Add(xBox);
                vectorPanel.Children.Add(new TextBlock
                {
                    Text = "Y",
                    Foreground = Brushes.LightGray,
                    FontSize = 9,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 2, 0)
                });
                vectorPanel.Children.Add(yBox);
                vectorPanel.Children.Add(new TextBlock
                {
                    Text = "Z",
                    Foreground = Brushes.LightGray,
                    FontSize = 9,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 2, 0)
                });
                vectorPanel.Children.Add(zBox);
                vectorPanel.Children.Add(new TextBlock
                {
                    Text = "W",
                    Foreground = Brushes.LightGray,
                    FontSize = 9,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 2, 0)
                });
                vectorPanel.Children.Add(wBox);

                editorControl = vectorPanel;
            }
        }

        fieldPanel.Children.Add(editorControl!);
        return fieldPanel;
    }

    private static NumericUpDown CreateFloatNumericBox(float initialVal, Action<float> onValueChanged, bool hasSpinner = false)
    {
        NumericUpDown numericBox = new NumericUpDown
        {
            Value = (decimal)initialVal,
            Increment = (decimal)Math.Max(initialVal / 10f, 0.1f),
            FontSize = 11,
            AllowSpin = true,
            ParsingNumberStyle = NumberStyles.Float,
            Background = EditorColor.FromRGB(28, 28, 28),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Center,
            FormatString = "F2",
            ShowButtonSpinner = hasSpinner
        };
        numericBox.ValueChanged += (s, e) =>
        {
            try
            {
                onValueChanged((float)(double)numericBox.Value);
            }
            catch (Exception ex) { Debug.Error(ex.Message); }
        };
        return numericBox;
    }

    private static NumericUpDown CreateIntegerNumericBox(int initialVal, Action<int> onValueChanged, bool hasSpinner = false)
    {
        NumericUpDown numericBox = new NumericUpDown
        {
            Value = initialVal,
            Increment = 1,
            FontSize = 11,
            AllowSpin = true,
            ParsingNumberStyle = NumberStyles.Integer,
            Background = EditorColor.FromRGB(28, 28, 28),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Center,
            ShowButtonSpinner = hasSpinner
        };
        numericBox.ValueChanged += (s, e) =>
        {
            try
            {
                onValueChanged((int)numericBox.Value);
            }
            catch (Exception ex) { Debug.Error(ex.Message); }
        };
        return numericBox;
    }

    private static Button? CreateColorFieldEditor(FieldInfo field, IComponent component, ColorAttribute colorAttr)
    {
        var fieldValue = field.GetValue(component);
        if (fieldValue == null) return null;
        float4 colorValue = (float4)fieldValue;

        // Color preview button with ColorPicker flyout
        Button colorButton = new Button
        {
            Height = 25,
            MinWidth = 100,
            Background = EditorColor.FromColor(colorValue),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            CornerRadius = new CornerRadius(3),
            Margin = new Thickness(8, 0, 8, 0),
            HorizontalContentAlignment = HorizontalAlignment.Left
        };

        // Create ColorPicker
        ColorPicker colorPicker = new ColorPicker
        {
            Width = 280,
            Height = 320,
            Color = EditorColor.FromColor(colorValue).Color,
            IsAlphaVisible = colorAttr.ShowAlpha,
            IsAccentColorsVisible = true,
            IsColorSpectrumVisible = true,
            IsColorPreviewVisible = true,
            IsColorComponentsVisible = true,
            IsHexInputVisible = false,
        };

        // Configure for HDR if needed
        if (colorAttr.HDR)
        {
            // For HDR colors, you might want to create a custom color picker
            // or adjust the ColorPicker to handle values > 1
            Debug.Warning("HDR color picker not fully implemented - using standard picker");
        }

        // Create flyout
        Flyout flyout = new Flyout
        {
            Content = new Border
            {
                Background = EditorColor.FromRGB(40, 40, 40),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(1),
                BorderBrush = EditorColor.FromRGB(80, 80, 80),
                Child = colorPicker
            },
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            ShowMode = FlyoutShowMode.Standard
        };

        colorButton.Flyout = flyout;

        // Update on color change
        colorPicker.ColorChanged += (s, e) =>
        {
            Color selectedColor = colorPicker.Color;

            // Convert to float4
            float4 newColor = new float4(
                selectedColor.R / 255f,
                selectedColor.G / 255f,
                selectedColor.B / 255f,
                selectedColor.A / 255f);

            // Update component
            field.SetValue(component, newColor);
            colorButton.Background = new SolidColorBrush(selectedColor); // Update button appearance
        };

        return colorButton;
    }

    private static StackPanel? CreateRotationFieldEditor(FieldInfo field, IComponent component, RotationAttribute colorAttr)
    {
        var fieldValue = field.GetValue(component);
        if (fieldValue == null) return null;
        float4 quaternionValue = (float4)fieldValue;

        float3 eulerValue = Math.QuaternionToEuler(quaternionValue);
        StackPanel eulerRotationPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        NumericUpDown xBox = CreateFloatNumericBox(eulerValue.X, (val) =>
        { eulerValue.X = val; field.SetValue(component, Math.EulerToQuaternion(eulerValue)); });

        NumericUpDown yBox = CreateFloatNumericBox(eulerValue.Y, (val) =>
        { eulerValue.Y = val; field.SetValue(component, Math.EulerToQuaternion(eulerValue)); });

        NumericUpDown zBox = CreateFloatNumericBox(eulerValue.Z, (val) =>
        { eulerValue.Z = val; field.SetValue(component, Math.EulerToQuaternion(eulerValue)); });

        eulerRotationPanel.Children.Add(new TextBlock
        {
            Text = "X",
            Foreground = Brushes.LightGray,
            FontSize = 9,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0)
        });
        eulerRotationPanel.Children.Add(xBox);
        eulerRotationPanel.Children.Add(new TextBlock
        {
            Text = "Y",
            Foreground = Brushes.LightGray,
            FontSize = 9,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0)
        });
        eulerRotationPanel.Children.Add(yBox);
        eulerRotationPanel.Children.Add(new TextBlock
        {
            Text = "Z",
            Foreground = Brushes.LightGray,
            FontSize = 9,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0)
        });
        eulerRotationPanel.Children.Add(zBox);

        return eulerRotationPanel;
    }
}