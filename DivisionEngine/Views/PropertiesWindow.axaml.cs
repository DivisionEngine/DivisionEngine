using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Math = DivisionEngine.MathLib.Math;

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
            Background = EditorColor.FromRGB(68, 68, 68),
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

    private static StackPanel? CreateFieldEditor(FieldInfo field, IComponent component, uint entityId)
    {
        Type fieldType = field.FieldType;
        var fieldValue = field.GetValue(component);

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
        Control editorControl = new Control();

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

        fieldPanel.Children.Add(editorControl);
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

    public void SetupPropertiesForWorld(World world)
    {

    }
}