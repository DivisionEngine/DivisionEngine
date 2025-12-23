using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DivisionEngine.Projects;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Collections.Generic;
using System.IO;

namespace DivisionEngine.Editor;

public partial class AssetsWindow : EditorWindow
{
    private static readonly List<AssetsWindow?> currentWindows = [];

    private readonly ScrollViewer scrollViewer;
    private readonly WrapPanel assetsPanel;

    private readonly StackPanel header;
    private readonly TextBlock headerText;
    private readonly TextBlock assetCountText;

    public AssetsWindow()
    {
        InitializeComponent();

        assetsPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,            
        };
        scrollViewer = new ScrollViewer
        {
            Content = assetsPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        header = new StackPanel
        {
            Background = EditorColor.FromRGB(28, 28, 28),
            Orientation = Orientation.Horizontal,
            Spacing = 0,
            Height = 30,
            VerticalAlignment = VerticalAlignment.Top
        };
        headerText = new TextBlock
        {
            Text = "No Project Loaded",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        assetCountText = new TextBlock
        {
            Text = "0 assets",
            FontSize = 10,
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };
        header.Children.Add(headerText);
        header.Children.Add(assetCountText);

        StackPanel mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0
        };

        mainPanel.Children.Add(header);
        mainPanel.Children.Add(new Border
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            Height = 1,
            Margin = new Thickness(0, 0, 0, 5)
        });
        mainPanel.Children.Add(scrollViewer);
        this.FindControl<Border>("MainBorder")!.Child = mainPanel;

        currentWindows.Add(this);
        LoadAssetsForCurrentProject();
    }

    /// <summary>
    /// Has the assets window load the current project assets.
    /// </summary>
    public static void LoadAssetsForCurrentProject()
    {
        ValidatePropertiesWindows();
        foreach (AssetsWindow? window in currentWindows)
            window!.SetupForCurrentProject();
    }

    /// <summary>
    /// Makes sure all assets windows in current list are active.
    /// </summary>
    private static void ValidatePropertiesWindows()
    {
        foreach (AssetsWindow? window in currentWindows.ToArray()) // Dont forget to create iterator copy
        {
            if (window == null || !window.IsLoaded)
                currentWindows.Remove(window);
        }
    }

    private bool SetupForCurrentProject()
    {
        if (ProjectManager.CurrentProjectPath == null || ProjectManager.CurrentProjectName == null)
        {
            Debug.Warning("Could not load assets, no project is loaded");
            headerText.Text = "No Project Loaded";
            assetCountText.Text = "0 assets";
            return false;
        }
        assetsPanel.Children.Clear();

        headerText.Text = ProjectManager.CurrentProjectPath;
        Dispatcher.UIThread.Post(() => LoadAssetsAtPath(ProjectManager.CurrentProjectPath));
        return true;
    }

    private void LoadAssetsAtPath(string path)
    {
        try
        {
            DirectoryInfo pathInfo = new DirectoryInfo(path);

            if (!pathInfo.Exists)
            {
                // Assets directory doesn't exist, show message
                ShowEmptyState("Assets folder is empty");
                assetCountText.Text = "0 assets";
                return;
            }

            // Clear existing assets
            assetsPanel.Children.Clear();

            // Load folders first
            var folders = pathInfo.GetDirectories();
            foreach (var folder in folders)
            {
                CreateFolderAsset(folder);
            }

            // Load files
            var files = pathInfo.GetFiles();
            foreach (var file in files)
            {
                CreateFileAsset(file);
            }

            // Update count
            int totalAssets = folders.Length + files.Length;
            assetCountText.Text = $"{totalAssets} asset{(totalAssets != 1 ? "s" : "")}";

            // Show empty state if no assets
            if (totalAssets == 0)
            {
                ShowEmptyState("No assets found");
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"Failed to load assets: {ex.Message}");
            ShowEmptyState($"Error: {ex.Message}");
            assetCountText.Text = "Error";
        }
    }

    private void CreateFolderAsset(DirectoryInfo folder)
    {
        var folderBorder = new Border
        {
            Width = 80,
            Height = 80,
            Background = EditorColor.FromRGB(0x22, 0x22, 0x22),
            BorderThickness = new Thickness(1),
            BorderBrush = EditorColor.FromRGB(0x44, 0x44, 0x44),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(5),
            Padding = new Thickness(5),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var folderStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Folder icon using MaterialIcons
        var folderIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Folder,
            Width = 32,
            Height = 32,
            Foreground = Brushes.SteelBlue
        };

        // Folder name (truncated if too long)
        string folderName = folder.Name;
        if (folderName.Length > 10)
        {
            folderName = folderName.Substring(0, 8) + "..";
        }

        var folderNameText = new TextBlock
        {
            Text = folderName,
            Foreground = Brushes.White,
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 70
        };

        folderStack.Children.Add(folderIcon);
        folderStack.Children.Add(folderNameText);
        folderBorder.Child = folderStack;

        // Hover effect
        folderBorder.PointerEntered += (s, e) =>
        {
            folderBorder.Background = EditorColor.FromRGB(0x33, 0x33, 0x33);
        };

        folderBorder.PointerExited += (s, e) =>
        {
            folderBorder.Background = EditorColor.FromRGB(0x22, 0x22, 0x22);
        };

        // Add double-click to open folder
        folderBorder.DoubleTapped += (s, e) =>
        {
            Debug.Info($"Opening folder: {folder.Name}");
            // Future: Navigate into folder
        };

        // Add context menu
        var contextMenu = new ContextMenu
        {
            Background = EditorColor.FromRGB(0x33, 0x33, 0x33),
            BorderBrush = EditorColor.FromRGB(0x55, 0x55, 0x55)
        };

        contextMenu.Items.Add(CreateMenuItem("Open", MaterialIconKind.FolderOpen, () =>
        {
            Debug.Info($"Opening folder: {folder.Name}");
        }));

        contextMenu.Items.Add(CreateMenuItem("Rename", MaterialIconKind.Pencil, () =>
        {
            Debug.Info($"Renaming folder: {folder.Name}");
        }));

        contextMenu.Items.Add(new Separator());

        contextMenu.Items.Add(CreateMenuItem("Delete", MaterialIconKind.Delete, () =>
        {
            Debug.Info($"Deleting folder: {folder.Name}");
        }, Brushes.Red));

        folderBorder.ContextMenu = contextMenu;

        assetsPanel.Children.Add(folderBorder);
    }

    private void CreateFileAsset(FileInfo file)
    {
        var fileBorder = new Border
        {
            Width = 80,
            Height = 80,
            Background = EditorColor.FromRGB(0x22, 0x22, 0x22),
            BorderThickness = new Thickness(1),
            BorderBrush = EditorColor.FromRGB(0x44, 0x44, 0x44),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(5),
            Padding = new Thickness(5),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var fileStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // File icon based on extension
        var fileIcon = CreateFileIcon(file.Extension);

        // File name (truncated if too long)
        string fileName = Path.GetFileNameWithoutExtension(file.Name);
        if (fileName.Length > 10)
        {
            fileName = fileName.Substring(0, 8) + "..";
        }

        var fileNameText = new TextBlock
        {
            Text = fileName,
            Foreground = Brushes.White,
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 70
        };

        // File extension
        var extensionText = new TextBlock
        {
            Text = file.Extension.ToUpper(),
            Foreground = Brushes.Gray,
            FontSize = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        fileStack.Children.Add(fileIcon);
        fileStack.Children.Add(fileNameText);
        fileStack.Children.Add(extensionText);
        fileBorder.Child = fileStack;

        // Hover effect
        fileBorder.PointerEntered += (s, e) =>
        {
            fileBorder.Background = EditorColor.FromRGB(0x33, 0x33, 0x33);
        };

        fileBorder.PointerExited += (s, e) =>
        {
            fileBorder.Background = EditorColor.FromRGB(0x22, 0x22, 0x22);
        };

        // Add double-click to open file
        fileBorder.DoubleTapped += (s, e) =>
        {
            Debug.Info($"Opening file: {file.Name}");
            // Future: Open file in appropriate editor
        };

        // Add context menu
        var contextMenu = new ContextMenu
        {
            Background = EditorColor.FromRGB(0x33, 0x33, 0x33),
            BorderBrush = EditorColor.FromRGB(0x55, 0x55, 0x55)
        };

        contextMenu.Items.Add(CreateMenuItem("Open", MaterialIconKind.FileDocument, () =>
        {
            Debug.Info($"Opening file: {file.Name}");
        }));

        contextMenu.Items.Add(CreateMenuItem("Rename", MaterialIconKind.Pencil, () =>
        {
            Debug.Info($"Renaming file: {file.Name}");
        }));

        contextMenu.Items.Add(CreateMenuItem("Copy Path", MaterialIconKind.ContentCopy, () =>
        {
            Debug.Info($"Copying path: {file.FullName}");
        }));

        contextMenu.Items.Add(new Separator());

        contextMenu.Items.Add(CreateMenuItem("Delete", MaterialIconKind.Delete, () =>
        {
            Debug.Info($"Deleting file: {file.Name}");
        }, Brushes.Red));

        fileBorder.ContextMenu = contextMenu;

        assetsPanel.Children.Add(fileBorder);
    }

    private MaterialIcon CreateFileIcon(string extension)
    {
        MaterialIconKind iconKind = extension.ToLower() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" or ".tiff" => MaterialIconKind.Image,
            ".obj" or ".fbx" or ".gltf" or ".glb" or ".stl" => MaterialIconKind.CubeOutline,
            ".wav" or ".mp3" or ".ogg" or ".flac" => MaterialIconKind.Music,
            ".cs" or ".js" or ".ts" or ".cpp" or ".h" => MaterialIconKind.CodeBraces,
            ".json" or ".xml" or ".yml" or ".yaml" => MaterialIconKind.CodeJson,
            ".txt" or ".md" or ".rtf" => MaterialIconKind.TextBox,
            ".shader" or ".hlsl" or ".glsl" => MaterialIconKind.Eyedropper,
            ".mat" or ".material" => MaterialIconKind.Palette,
            ".scene" or ".prefab" => MaterialIconKind.ViewDashboard,
            _ => MaterialIconKind.FileDocument
        };

        Color iconColor = extension.ToLower() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp" => Colors.LightSkyBlue,
            ".obj" or ".fbx" or ".gltf" => Colors.LightSeaGreen,
            ".wav" or ".mp3" or ".ogg" => Colors.MediumPurple,
            ".cs" or ".js" or ".ts" => Colors.Gold,
            ".json" or ".xml" => Colors.LightGreen,
            ".shader" or ".hlsl" => Colors.Violet,
            ".mat" or ".material" => Colors.Orange,
            _ => Colors.LightGray
        };

        return new MaterialIcon
        {
            Kind = iconKind,
            Width = 32,
            Height = 32,
            Foreground = new SolidColorBrush(iconColor)
        };
    }

    private MenuItem CreateMenuItem(string text, MaterialIconKind icon, Action action, IBrush? foreground = null)
    {
        var menuItem = new MenuItem
        {
            Header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new MaterialIcon
                    {
                        Kind = icon,
                        Width = 16,
                        Height = 16,
                        Foreground = foreground ?? Brushes.White
                    },
                    new TextBlock
                    {
                        Text = text,
                        Foreground = foreground ?? Brushes.White
                    }
                }
            },
            Foreground = foreground ?? Brushes.White
        };

        menuItem.Click += (s, e) => action();
        return menuItem;
    }

    private void ShowEmptyState(string message)
    {
        assetsPanel.Children.Clear();

        var emptyStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20)
        };

        var icon = new MaterialIcon
        {
            Kind = MaterialIconKind.FolderOpenOutline,
            Width = 48,
            Height = 48,
            Foreground = Brushes.Gray
        };

        var messageText = new TextBlock
        {
            Text = message,
            Foreground = Brushes.Gray,
            FontStyle = FontStyle.Italic,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        emptyStack.Children.Add(icon);
        emptyStack.Children.Add(messageText);
        assetsPanel.Children.Add(emptyStack);
    }
}