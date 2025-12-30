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
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace DivisionEngine.Editor;

/// <summary>
/// Represents all loaded assets windows.
/// </summary>
public partial class AssetsWindow : EditorWindow
{
    /// <summary>
    /// Represents the assets window view state.
    /// </summary>
    public enum ViewState
    {
        Tiles, List
    }

    private const double listItemHeight = 25;
    private static readonly List<AssetsWindow?> currentWindows = [];

    // Display panels
    private readonly ScrollViewer scrollViewer;
    private readonly WrapPanel assetsTilePanel;
    private readonly Grid assetsListPanel;
    private readonly StackPanel listNamePanel;
    private readonly StackPanel listSizePanel;

    // Header
    private readonly StackPanel header;
    private readonly TextBlock headerText;
    private readonly TextBlock itemCountText;
    private readonly Button upDirButton;
    private readonly Button viewButton;
    private readonly MaterialIcon viewButtonIcon;

    /// <summary>
    /// Current view state of this assets window.
    /// </summary>
    public ViewState CurrentView { get; private set; }
    private string currentPath;

    /// <summary>
    /// Build a new assets window and link up callbacks.
    /// </summary>
    public AssetsWindow()
    {
        InitializeComponent();

        // Display panels setup

        // List panel
        listNamePanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = EditorColor.FromRGB(34, 34, 34),
            Margin = new Thickness(4, 2, 2, 2)
        };
        GridSplitter listSplitterA = new GridSplitter
        {
            ResizeDirection = GridResizeDirection.Columns,
        };
        listSizePanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = EditorColor.FromRGB(34, 34, 34),
            Margin = new Thickness(4, 2, 2, 2),
        };
        assetsListPanel = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)), // Name
                new ColumnDefinition(new GridLength(2, GridUnitType.Pixel)), // Splitter
                new ColumnDefinition(new GridLength(500, GridUnitType.Pixel)), // Size
            },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            IsVisible = false,
        };
        Grid.SetColumn(listNamePanel, 0);
        Grid.SetColumn(listSplitterA, 1);
        Grid.SetColumn(listSizePanel, 2);

        assetsListPanel.Children.Add(listNamePanel);
        assetsListPanel.Children.Add(listSplitterA);
        assetsListPanel.Children.Add(listSizePanel);

        // Tiles panel
        assetsTilePanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        scrollViewer = new ScrollViewer
        {
            Content = assetsTilePanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        // Header setup
        header = new StackPanel
        {
            Background = EditorColor.FromRGB(28, 28, 28),
            Orientation = Orientation.Horizontal,
            Spacing = 0,
            Height = 30,
            VerticalAlignment = VerticalAlignment.Top,
        };
        headerText = new TextBlock
        {
            Text = "No Project Loaded",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(5),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        itemCountText = new TextBlock
        {
            Text = "0 items",
            FontSize = 12,
            Foreground = EditorColor.FromRGB(128, 128, 128),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(5),
        };
        MaterialIcon upFolderIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.FolderUpload,
            Width = 18,
            Height = 18,
            Foreground = EditorColor.FromRGB(80, 80, 80),
        };
        upDirButton = new Button
        {
            Content = upFolderIcon,
            Background = EditorColor.FromRGB(12, 12, 12),
            Margin = new Thickness(2, 2, 2, 2),
            Padding = new Thickness(3, 1, 3, 1),
        };
        viewButtonIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.FormatListBulleted,
            Width = 18,
            Height = 18,
            Foreground = EditorColor.FromRGB(80, 80, 80),
        };
        viewButton = new Button
        {
            Content = viewButtonIcon,
            Background = EditorColor.FromRGB(12, 12, 12),
            Margin = new Thickness(2, 2, 2, 2),
            Padding = new Thickness(3, 1, 3, 1),
        };
        upDirButton.Click += (s, e) => NavigateUpOneLevel();
        viewButton.Click += (s, e) => ToggleViewState();
        header.Children.Add(upDirButton);
        header.Children.Add(viewButton);
        header.Children.Add(headerText);
        header.Children.Add(itemCountText);

        // Build assets window layout
        Border separatorBorder = new Border
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            Height = 1,
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

        // Finish assets window setup
        CurrentView = ViewState.Tiles;
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

    /// <summary>
    /// Sets up this assets window with a path to load assets.
    /// </summary>
    /// <param name="path">Path to load assets at</param>
    /// <returns>Whether setup was successful</returns>
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
        
        // Clear panels
        assetsTilePanel.Children.Clear();
        listNamePanel.Children.Clear();
        listSizePanel.Children.Clear();

        // Dispatch asset loading
        headerText.Text = path;
        Dispatcher.UIThread.Post(() => LoadAssetsAtPath(path));
        return true;
    }

    /// <summary>
    /// Navigates up one level by reloading assets in the parent directory.
    /// </summary>
    /// <returns></returns>
    private bool NavigateUpOneLevel()
    {
        if (string.IsNullOrEmpty(currentPath)) return false;

        DirectoryInfo dir = new DirectoryInfo(currentPath);
        if (dir.Parent == null) return false;

        Dispatcher.UIThread.Post(() => LoadAssets(dir.Parent.FullName));
        return true;
    }

    /// <summary>
    /// Toggles between view states.
    /// </summary>
    private void ToggleViewState()
    {
        if (CurrentView == ViewState.Tiles)
        {
            CurrentView = ViewState.List;
            viewButtonIcon.Kind = MaterialIconKind.ViewGrid;
            Setup(currentPath);
        }
        else
        {
            CurrentView = ViewState.Tiles;
            viewButtonIcon.Kind = MaterialIconKind.FormatListBulleted;
            Setup(currentPath);
        }
    }

    /// <summary>
    /// Loads all assets at a specific path.
    /// </summary>
    /// <param name="path">Path to load assets at</param>
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

            // Load correct view
            int totalAssets = LoadViewStateInterchange(pathInfo);

            // Update count
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

    private static async Task<long> AccumulateFolderSize(DirectoryInfo pathInfo)
    {
        long totalSize = 0;
        int taskDelayTracker = 10;
        EnumerationOptions options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            MaxRecursionDepth = int.MaxValue,
            RecurseSubdirectories = true,
        };

        foreach (FileInfo file in pathInfo.EnumerateFiles("*", options))
        {
            totalSize += file.Length;
            taskDelayTracker--;
            if (taskDelayTracker < 0)
            {
                await Task.Delay(1);
                taskDelayTracker = 10;
            }
        }
        return totalSize;
    }

    /// <summary>
    /// Loads the current path based on the view state.
    /// </summary>
    /// <param name="pathInfo">Path to load into view</param>
    /// <returns>Final item count</returns>
    private int LoadViewStateInterchange(DirectoryInfo pathInfo)
    {
        DirectoryInfo[] folders = pathInfo.GetDirectories();
        FileInfo[] files = pathInfo.GetFiles();
        if (CurrentView == ViewState.Tiles)
        {
            assetsTilePanel.IsVisible = true;
            assetsListPanel.IsVisible = false;
            scrollViewer.Content = assetsTilePanel;
            foreach (DirectoryInfo folder in folders) CreateFolderTile(folder);
            foreach (FileInfo file in files) CreateFileTile(file);
        }
        else
        {
            assetsTilePanel.IsVisible = false;
            assetsListPanel.IsVisible = true;
            scrollViewer.Content = assetsListPanel;
            foreach (DirectoryInfo folder in folders) CreateFolderListItem(folder);
            foreach (FileInfo file in files) CreateFileListItem(file);
        }

        scrollViewer.HorizontalAlignment = CurrentView == ViewState.Tiles
            ? HorizontalAlignment.Left
            : HorizontalAlignment.Stretch;

        return folders.Length + files.Length;
    }

    /// <summary>
    /// Creates a folder tile and adds it to the tiles panel.
    /// </summary>
    /// <param name="folder">Folder to add to tiles panel</param>
    private void CreateFolderTile(DirectoryInfo folder)
    {
        Border folderBorder = new Border
        {
            Width = 80,
            Height = 85,
            Background = EditorColor.FromRGB(24, 24, 24),
            BorderThickness = new Thickness(1),
            BorderBrush = EditorColor.FromRGB(68, 68, 68),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(5),
            Padding = new Thickness(5),
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        StackPanel folderStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        MaterialIcon folderIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Folder,
            Width = 48,
            Height = 48,
            Foreground = EditorColor.FromColor(ColorPalette.Mint),
            Margin = new Thickness(0, 0, 0, 5),
        };

        // Folder name (truncated if too long)
        string folderName = folder.Name;
        if (folderName.Length > 12) folderName = string.Concat(folderName.AsSpan(0, 10), "..");

        TextBlock folderNameText = new TextBlock
        {
            Text = folderName,
            Foreground = Brushes.White,
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 80,
        };

        folderStack.Children.Add(folderIcon);
        folderStack.Children.Add(folderNameText);
        folderBorder.Child = folderStack;

        // Hover effect
        folderBorder.PointerEntered += (s, e) => { folderBorder.Background = EditorColor.FromRGB(17, 17, 17); };
        folderBorder.PointerExited += (s, e) => { folderBorder.Background = EditorColor.FromRGB(24, 24, 24); };

        // Add double-click to open folder
        folderBorder.DoubleTapped += (s, e) => Dispatcher.UIThread.Post(() => LoadAssets(folder.FullName));

        // Add context menu
        ContextMenu contextMenu = new ContextMenu
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            BorderBrush = EditorColor.FromRGB(128, 128, 128)
        };
        contextMenu.Items.Add(CreateMenuItem("Open", MaterialIconKind.FolderOpen, () => Process.Start("explorer.exe", folder.FullName)));
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
        assetsTilePanel.Children.Add(folderBorder);
    }

    /// <summary>
    /// Creates a file tile and adds it to the tiles panel.
    /// </summary>
    /// <param name="file">File to add to tiles panel</param>
    private void CreateFileTile(FileInfo file)
    {
        Border fileBorder = new Border
        {
            Width = 80,
            Height = 85,
            Background = EditorColor.FromRGB(24, 24, 24),
            BorderThickness = new Thickness(1),
            BorderBrush = EditorColor.FromRGB(68, 68, 68),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(5),
            Padding = new Thickness(5),
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        StackPanel fileStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        // File icon based on extension
        MaterialIcon fileIcon = CreateFileIcon(file.Extension, 48);

        // File name (truncated if too long)
        string fileName = Path.GetFileNameWithoutExtension(file.Name);
        if (fileName.Length > 12) fileName = string.Concat(fileName.AsSpan(0, 10), "..");

        TextBlock fileNameText = new TextBlock
        {
            Text = fileName,
            Foreground = Brushes.White,
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 80,
        };

        // File extension
        TextBlock extensionText = new TextBlock
        {
            Text = file.Extension.ToUpper(),
            Foreground = Brushes.LightGray,
            FontSize = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        fileStack.Children.Add(fileIcon);
        fileStack.Children.Add(fileNameText);
        fileStack.Children.Add(extensionText);
        fileBorder.Child = fileStack;

        // Hover effect
        fileBorder.PointerEntered += (s, e) => { fileBorder.Background = EditorColor.FromRGB(17, 17, 17); };
        fileBorder.PointerExited += (s, e) => { fileBorder.Background = EditorColor.FromRGB(24, 24, 24); };

        // Add double-click to open file
        fileBorder.DoubleTapped += (s, e) => EditorUI.OpenFile(file);

        // Add context menu
        ContextMenu contextMenu = new ContextMenu
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            BorderBrush = EditorColor.FromRGB(128, 128, 128)
        };
        contextMenu.Items.Add(CreateMenuItem("Open", MaterialIconKind.FileDocument, () => EditorUI.OpenFile(file)));
        contextMenu.Items.Add(CreateMenuItem("Rename", MaterialIconKind.Pencil, () =>
        {
            Debug.Info($"Renaming file: {file.Name}");
        }));
        contextMenu.Items.Add(CreateMenuItem("Copy Path", MaterialIconKind.ContentCopy, () =>
        {
            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(file.FullName);
            Debug.Info($"Copying path: {file.FullName}");
        }));
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(CreateMenuItem("Delete", MaterialIconKind.Delete, () =>
        {
            Debug.Info($"Deleting file: {file.Name}");
        }, Brushes.Red));

        fileBorder.ContextMenu = contextMenu;
        assetsTilePanel.Children.Add(fileBorder);
    }

    /// <summary>
    /// Creates a folder item and adds it to the list panel.
    /// </summary>
    /// <param name="folder">Folder to add to list panel</param>
    private void CreateFolderListItem(DirectoryInfo folder)
    {
        MaterialIcon folderIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Folder,
            Width = 22,
            Height = 22,
            Foreground = EditorColor.FromColor(ColorPalette.Mint),
            Margin = new Thickness(0, 0, 0, 5),
            Padding = new Thickness(8, 2, 0, 2),
            VerticalAlignment = VerticalAlignment.Center,
        };

        // Folder name (truncated if too long)
        string folderName = folder.Name;
        if (folderName.Length > 64) folderName = string.Concat(folderName.AsSpan(0, 10), "..");

        TextBlock folderNameText = new TextBlock
        {
            Text = folderName,
            Foreground = Brushes.White,
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(2),
        };
        TextBlock folderSizeText = new TextBlock
        {
            Text = "...",
            Foreground = Brushes.White,
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(4, 2, 2, 2),
            Height = listItemHeight,
        };

        StackPanel listName = new StackPanel
        {
            Children = { folderIcon, folderNameText },
            Spacing = 2,
            Orientation = Orientation.Horizontal,
            Height = listItemHeight,
        };
        StackPanel listSize = new StackPanel
        {
            Children = { folderSizeText },
            Spacing = 2,
            Orientation = Orientation.Horizontal,
            Height = listItemHeight,
        };
        listName.DoubleTapped += (s, e) => Dispatcher.UIThread.Post(() => LoadAssets(folder.FullName));
        listSize.DoubleTapped += (s, e) => Dispatcher.UIThread.Post(() => LoadAssets(folder.FullName));

        Dispatcher.UIThread.Post(async () =>
        {
            DirectoryInfo pathInfo = new DirectoryInfo(folder.FullName);
            folderSizeText.Text = EditorUI.FormatFileSize(await AccumulateFolderSize(pathInfo));
        });

        listNamePanel.Children.Add(listName);
        listSizePanel.Children.Add(listSize);
    }

    /// <summary>
    /// Creates a file item and adds it to the list panel.
    /// </summary>
    /// <param name="file">File to add to list panel</param>
    private void CreateFileListItem(FileInfo file)
    {
        // File icon based on extension
        MaterialIcon fileIcon = CreateFileIcon(file.Extension, 22);
        fileIcon.VerticalAlignment = VerticalAlignment.Center;
        fileIcon.Padding = new Thickness(8, 2, 0, 2);

        // File name and size
        string fileName = Path.GetFileNameWithoutExtension(file.Name);
        if (fileName.Length > 64) fileName = string.Concat(fileName.AsSpan(0, 10), ".."); // Truncate if too long
        string fileSize = EditorUI.FormatFileSize(file.Length);

        TextBlock fileNameText = new TextBlock
        {
            Text = fileName,
            Foreground = Brushes.White,
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(2),
        };
        TextBlock fileSizeText = new TextBlock
        {
            Text = fileSize,
            Foreground = Brushes.White,
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(4, 2, 2, 2),
            Height = listItemHeight,
        };

        StackPanel listName = new StackPanel
        {
            Children = { fileIcon, fileNameText },
            Spacing = 2,
            Orientation = Orientation.Horizontal,
            Height = listItemHeight,
        };
        StackPanel listSize = new StackPanel
        {
            Children = { fileSizeText },
            Spacing = 2,
            Orientation = Orientation.Horizontal,
            Height = listItemHeight,
        };
        listNamePanel.Children.Add(listName);
        listSizePanel.Children.Add(listSize);
    }

    /// <summary>
    /// Creates the icon for each file type.
    /// </summary>
    /// <param name="extension">File extension</param>
    /// <param name="size">Icon size</param>
    /// <returns>Material file icon</returns>
    private static MaterialIcon CreateFileIcon(string extension, double size)
    {
        MaterialIconKind iconKind = extension.ToLower() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" or ".tiff" or ".ico" => MaterialIconKind.Image,
            //".obj" or ".fbx" or ".gltf" or ".glb" or ".stl" => MaterialIconKind.CubeOutline,
            ".wav" or ".mp3" or ".ogg" or ".flac" => MaterialIconKind.Audio,
            ".cs" or ".js" or ".ts" or ".cpp" or ".h" or ".manifest" => MaterialIconKind.CodeBraces,
            ".dll" => MaterialIconKind.Library,
            ".json" or ".xml" or ".yml" or ".yaml" or ".axaml" => MaterialIconKind.CodeJson,
            ".txt" or ".md" or ".rtf" or ".csproj" or ".gitignore" or ".gitattributes" or ".sln" => MaterialIconKind.TextBox,
            ".shader" or ".hlsl" => MaterialIconKind.Eyedropper,
            ".mat" => MaterialIconKind.Palette,
            ".wld" => MaterialIconKind.ViewDashboard,
            _ => MaterialIconKind.FileDocument,
        };
        float4 iconColor = extension.ToLower() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" or ".tiff" or ".ico" => ColorPalette.SkyBlue,
            //".obj" or ".fbx" or ".gltf" or ".glb" or ".stl" => ColorPalette.LightSeaGreen,
            ".wav" or ".mp3" or ".ogg" or ".flac" => ColorPalette.Coral,
            ".cs" or ".js" or ".ts" or ".cpp" or ".h" or ".manifest" => ColorPalette.Khaki,
            ".dll" => ColorPalette.SandyBrown,
            ".json" or ".xml" or ".yml" or ".yaml" or ".axaml" => ColorPalette.PaleGreen,
            ".txt" or ".md" or ".rtf" or ".csproj" or ".gitignore" or ".gitattributes" or ".sln" => ColorPalette.Khaki,
            ".shader" or ".hlsl" => ColorPalette.SalmonPink,
            ".mat" => ColorPalette.Orange,
            ".wld" => ColorPalette.PaleGreen,
            _ => ColorPalette.Gray,
        };
        return new MaterialIcon
        {
            Kind = iconKind,
            Width = size,
            Height = size,
            Foreground = EditorColor.FromColor(iconColor),
            Margin = new Thickness(0, 0, 0, 5),
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
                        Foreground = foreground ?? Brushes.White,
                    },
                    new TextBlock
                    {
                        Text = text,
                        Foreground = foreground ?? Brushes.White,
                    }
                }
            },
            Foreground = foreground ?? Brushes.White,
        };
        menuItem.Click += (s, e) => action();
        return menuItem;
    }

    /// <summary>
    /// Shows an empty state in the display panel area.
    /// </summary>
    /// <param name="message">Empty message to display</param>
    private void ShowEmptyState(string message)
    {
        assetsTilePanel.Children.Clear();
        listNamePanel.Children.Clear();
        StackPanel emptyStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20),
        };
        MaterialIcon icon = new MaterialIcon
        {
            Kind = MaterialIconKind.FolderOpenOutline,
            Width = 64,
            Height = 64,
            Foreground = Brushes.Gray,
        };
        TextBlock messageText = new TextBlock
        {
            Text = message,
            Foreground = Brushes.Gray,
            FontStyle = FontStyle.Italic,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        emptyStack.Children.Add(icon);
        emptyStack.Children.Add(messageText);
        
        if (CurrentView == ViewState.Tiles)
            assetsTilePanel.Children.Add(emptyStack);
        else listNamePanel.Children.Add(emptyStack);
    }
}