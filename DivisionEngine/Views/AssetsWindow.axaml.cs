using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DivisionEngine.MathLib;
using DivisionEngine.Projects;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Path = System.IO.Path;

namespace DivisionEngine.Editor;

/// <summary>
/// Represents all loaded assets windows.
/// </summary>
public partial class AssetsWindow : EditorWindow
{
    private static readonly List<AssetsWindow?> currentWindows = [];

    private readonly ScrollViewer scrollViewer;
    private readonly WrapPanel assetsPanel;

    private readonly StackPanel header;
    private readonly TextBlock headerText;
    private readonly TextBlock itemCountText;
    private readonly Button upDirButton;

    private string currentPath;

    public AssetsWindow()
    {
        InitializeComponent();

        assetsPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
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
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        itemCountText = new TextBlock
        {
            Text = "0 items",
            FontSize = 12,
            Foreground = EditorColor.FromRGB(128, 128, 128),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(5)
        };
        MaterialIcon upFolderIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.FolderUpload,
            Width = 16,
            Height = 16,
            Foreground = EditorColor.FromRGB(68, 68, 68),
        };
        upDirButton = new Button
        {
            Content = upFolderIcon,
            Background = EditorColor.FromRGB(12, 12, 12),
            Margin = new Thickness(2, 2, 2, 2),
            Padding = new Thickness(4, 2, 4, 2)
        };
        upDirButton.Click += (s, e) => NavigateUpOneLevel();
        header.Children.Add(upDirButton);
        header.Children.Add(headerText);
        header.Children.Add(itemCountText);

        Border separatorBorder = new Border
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            Height = 1
        };
        Grid grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(30, GridUnitType.Pixel), // Header
                new RowDefinition(1, GridUnitType.Pixel),  // Separator
                new RowDefinition(1, GridUnitType.Star),   // Scrollable area (takes remaining space)
            }
        };
        Grid.SetRow(header, 0);
        Grid.SetRow(separatorBorder, 1);
        Grid.SetRow(scrollViewer, 2);
        grid.Children.Add(header);
        grid.Children.Add(separatorBorder);
        grid.Children.Add(scrollViewer);
        this.FindControl<Border>("MainBorder")!.Child = grid;

        currentPath = string.Empty;
        currentWindows.Add(this);
        Dispatcher.UIThread.Post(LoadAssetsForCurrentProject);
    }

    /// <summary>
    /// Has the assets window load the current project assets.
    /// </summary>
    public static void LoadAssetsForCurrentProject()
    {
        ValidatePropertiesWindows();
        Debug.Info("Asset Windows: " + currentWindows.Count);
        foreach (AssetsWindow? window in currentWindows)
            window!.Setup(ProjectManager.CurrentProjectPath);
    }

    /// <summary>
    /// Has the assets window load all assets at a path.
    /// </summary>
    public static void LoadAssets(string path)
    {
        ValidatePropertiesWindows();
        foreach (AssetsWindow? window in currentWindows)
            window!.Setup(path);
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

    private bool Setup(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.Warning("Could not load assets, no project is loaded");
            headerText.Text = "No Project Loaded";
            itemCountText.Text = "0 items";
            return false;
        }
        currentPath = path;
        assetsPanel.Children.Clear();

        headerText.Text = path;
        Dispatcher.UIThread.Post(() => LoadAssetsAtPath(path));
        return true;
    }

    private bool NavigateUpOneLevel()
    {
        if (string.IsNullOrEmpty(currentPath)) return false;

        DirectoryInfo dir = new DirectoryInfo(currentPath);
        if (dir.Parent == null) return false;

        Dispatcher.UIThread.Post(() => LoadAssets(dir.Parent.FullName));
        return true;
    }

    private void LoadAssetsAtPath(string path)
    {
        try
        {
            DirectoryInfo pathInfo = new DirectoryInfo(path);
            if (!pathInfo.Exists)
            {
                ShowEmptyState("Directory does not exist");
                itemCountText.Text = "0 items";
                return;
            }

            // Load folders
            DirectoryInfo[] folders = pathInfo.GetDirectories();
            foreach (var folder in folders) CreateFolderAsset(folder);

            // Load files
            FileInfo[] files = pathInfo.GetFiles();
            foreach (var file in files) CreateFileAsset(file);

            // Update count
            int totalAssets = folders.Length + files.Length;
            itemCountText.Text = $"{totalAssets} item{(totalAssets != 1 ? "s" : "")}";

            // Show empty state if no assets
            if (totalAssets == 0) ShowEmptyState("No Items");
        }
        catch (Exception ex)
        {
            Debug.Error($"Failed to load assets: {ex.Message}");
            ShowEmptyState($"Error: {ex.Message}");
            itemCountText.Text = "Error";
        }
    }

    /// <summary>
    /// Creates a folder asset and adds it to the list.
    /// </summary>
    /// <param name="folder">Folder to add to list</param>
    private void CreateFolderAsset(DirectoryInfo folder)
    {
        Border folderBorder = new Border
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
        StackPanel folderStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        MaterialIcon folderIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Folder,
            Width = 48,
            Height = 48,
            Foreground = EditorColor.FromColor(ColorPalette.Mint)
        };

        // Folder name (truncated if too long)
        string folderName = folder.Name;
        if (folderName.Length > 10) folderName = string.Concat(folderName.AsSpan(0, 8), "..");

        TextBlock folderNameText = new TextBlock
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
            string newPath = folder.FullName;
            Dispatcher.UIThread.Post(() => LoadAssets(newPath));
        };

        // Add context menu
        ContextMenu contextMenu = new ContextMenu
        {
            Background = EditorColor.FromRGB(0x33, 0x33, 0x33),
            BorderBrush = EditorColor.FromRGB(0x55, 0x55, 0x55)
        };
        contextMenu.Items.Add(CreateMenuItem("Open", MaterialIconKind.FolderOpen, () =>
        {
            Process.Start("explorer.exe", folder.FullName);
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

    /// <summary>
    /// Creates a file asset and adds it to the list.
    /// </summary>
    /// <param name="file">File to add to list</param>
    private void CreateFileAsset(FileInfo file)
    {
        Border fileBorder = new Border
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
        StackPanel fileStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // File icon based on extension
        MaterialIcon fileIcon = CreateFileIcon(file.Extension);

        // File name (truncated if too long)
        string fileName = Path.GetFileNameWithoutExtension(file.Name);
        if (fileName.Length > 10) fileName = string.Concat(fileName.AsSpan(0, 8), "..");

        TextBlock fileNameText = new TextBlock
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
        TextBlock extensionText = new TextBlock
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
        ContextMenu contextMenu = new ContextMenu
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

    private static MaterialIcon CreateFileIcon(string extension)
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
        float4 iconColor = extension.ToLower() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp" => ColorPalette.SkyBlue,
            ".obj" or ".fbx" or ".gltf" => ColorPalette.LightSeaGreen,
            ".wav" or ".mp3" or ".ogg" => ColorPalette.Purple,
            ".cs" or ".js" or ".ts" => ColorPalette.Gold,
            ".json" or ".xml" => ColorPalette.PaleGreen,
            ".shader" or ".hlsl" => ColorPalette.Violet,
            ".mat" or ".material" => ColorPalette.Orange,
            _ => ColorPalette.Gray,
        };
        return new MaterialIcon
        {
            Kind = iconKind,
            Width = 48,
            Height = 48,
            Foreground = EditorColor.FromColor(iconColor)
        };
    }

    private static MenuItem CreateMenuItem(string text, MaterialIconKind icon, Action action, IBrush? foreground = null)
    {
        MenuItem menuItem = new MenuItem
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
        StackPanel emptyStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20)
        };
        MaterialIcon icon = new MaterialIcon
        {
            Kind = MaterialIconKind.FolderOpenOutline,
            Width = 64,
            Height = 64,
            Foreground = Brushes.Gray
        };
        TextBlock messageText = new TextBlock
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