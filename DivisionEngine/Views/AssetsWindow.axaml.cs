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
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DivisionEngine.MathLib;
using DivisionEngine.Projects;
using DivisionEngine.Projects.Assets;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Math = DivisionEngine.MathLib.Math;
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

    /// <summary>
    /// Current view state of this assets window.
    /// </summary>
    public ViewState CurrentView { get; private set; }

    // Window vars
    private const double listItemHeight = 25;
    private static readonly List<AssetsWindow?> currentWindows = [];

    // Display panels
    private readonly ScrollViewer scrollViewer;
    private readonly UniformGrid assetsTileGrid;
    private readonly Grid assetsListPanel;
    private readonly StackPanel listNamePanel;
    private readonly StackPanel listSizePanel;

    // Header
    private readonly StackPanel header;
    private readonly TextBox directoryField;
    private readonly TextBlock itemCountText;
    private readonly Button upDirButton;
    private readonly Button viewButton;
    private readonly ComboBox filterDropdown;
    private readonly MaterialIcon viewButtonIcon;

    // Data vars
    private string currentPath;
    private AssetType currentFilter = AssetType.None;
    private static bool subscribedProjectEvents = false; // Track if subscribed to events
    private bool subscribedToFolderEvents = false; // Track if subscribed to asset folder events

    /// <summary>
    /// Static constructor to subscribe to all project events once.
    /// </summary>
    static AssetsWindow() => SubscribeToProjectEvents();

    /// <summary>
    /// Subscribe to project manager events.
    /// </summary>
    private static void SubscribeToProjectEvents()
    {
        if (subscribedProjectEvents) return;
        ProjectManager.ProjectLoaded += OnProjectLoaded;
        ProjectManager.ProjectClosing += OnProjectClosing;
        ProjectManager.ProjectClosed += OnProjectClosed;
        subscribedProjectEvents = true;
    }

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
        assetsTileGrid = new UniformGrid
        {
            Columns = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
        };
        scrollViewer = new ScrollViewer
        {
            Content = assetsTileGrid,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        };
        scrollViewer.SizeChanged += (s, e) =>
        {
            if (CurrentView == ViewState.Tiles) Dispatcher.UIThread.Post(UpdateTileColumns);
        };

        // Header setup
        header = new StackPanel
        {
            Background = EditorColor.FromRGB(28, 28, 28),
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Top,
        };
        directoryField = new TextBox
        {
            Text = string.Empty,
            PlaceholderText = "No Project Loaded",
            FontSize = 11,
            Foreground = Brushes.White,
            Margin = new Thickness(5),
            BorderThickness = new Thickness(0),
            Background = EditorColor.FromRGB(20, 20, 20),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
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
            Margin = new Thickness(8, 2, 2, 2),
            Padding = new Thickness(3, 1, 3, 1),
            VerticalAlignment = VerticalAlignment.Center,
        };
        viewButtonIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.FormatListBulleted,
            Width = 18,
            Height = 18,
            Foreground = EditorColor.FromRGB(80, 80, 80),
            VerticalAlignment = VerticalAlignment.Center,
        };
        viewButton = new Button
        {
            Content = viewButtonIcon,
            Background = EditorColor.FromRGB(12, 12, 12),
            Margin = new Thickness(2, 2, 2, 2),
            Padding = new Thickness(3, 1, 3, 1),
            VerticalAlignment = VerticalAlignment.Center,
        };

        // Filter dropdown
        filterDropdown = new ComboBox
        {
            MinWidth = 80,
            Margin = new Thickness(2, 2, 2, 2),
            Foreground = Brushes.White,
            Background = EditorColor.FromRGB(17, 17, 17),
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        filterDropdown.Items.Add(new ComboBoxItem { Content = "All Assets", Tag = AssetType.None });
        filterDropdown.Items.Add(new ComboBoxItem { Content = "Textures", Tag = AssetType.Texture });
        filterDropdown.Items.Add(new ComboBoxItem { Content = "Models/SDF", Tag = AssetType.SDF });
        filterDropdown.Items.Add(new ComboBoxItem { Content = "Materials", Tag = AssetType.Material });
        filterDropdown.Items.Add(new ComboBoxItem { Content = "Scripts", Tag = AssetType.Script });
        filterDropdown.Items.Add(new ComboBoxItem { Content = "Audio", Tag = AssetType.Audio });
        filterDropdown.Items.Add(new ComboBoxItem { Content = "Fonts", Tag = AssetType.Font });
        filterDropdown.SelectionChanged += (s, e) =>
        {
            if (s is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
            {
                currentFilter = (AssetType)item.Tag!;
                LoadAssets(currentPath ?? string.Empty); // Reload with new filter
            }
        };
        filterDropdown.SelectedIndex = 0; // Default to "All Assets"

        directoryField.TextChanged += DirectoryField_TextChanged;
        upDirButton.Click += (s, e) => NavigateUpOneLevel();
        viewButton.Click += (s, e) => ToggleViewState();
        header.Children.Add(upDirButton);
        header.Children.Add(viewButton);
        header.Children.Add(filterDropdown);
        header.Children.Add(directoryField);
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
                new RowDefinition(32, GridUnitType.Pixel), // Header
                new RowDefinition(1, GridUnitType.Pixel),  // Separator
                new RowDefinition(1, GridUnitType.Star),   // Scrollable area
            }
        };
        Grid.SetRow(header, 0);
        Grid.SetRow(separatorBorder, 1);
        Grid.SetRow(scrollViewer, 2);
        grid.Children.Add(header);
        grid.Children.Add(separatorBorder);
        grid.Children.Add(scrollViewer);
        this.FindControl<Border>("MainBorder")!.Child = grid;

        AttachBackgroundContextMenu();

        // Finish assets window setup
        CurrentView = ViewState.Tiles;
        currentPath = string.Empty;
        currentWindows.Add(this);
        Dispatcher.UIThread.Post(() => Setup(GetDefaultAssetsPath()));
    }

    private static string? GetDefaultAssetsPath()
    {
        if (!ProjectManager.IsCurrentLoaded) return null;
        string assetsPath = Path.Combine(ProjectManager.CurrentProjectPath!, "Assets");
        return Directory.Exists(assetsPath) ? assetsPath : ProjectManager.CurrentProjectPath;
    }

    /// <summary>
    /// Attaches the background context menu to the assets window.
    /// </summary>
    private void AttachBackgroundContextMenu()
    {
        ContextMenu backgroundContextMenu = new ContextMenu
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            BorderBrush = EditorColor.FromRGB(128, 128, 128)
        };

        backgroundContextMenu.Items.Add(EditorUI.CreateContextMenuItem("Show in Explorer", MaterialIconKind.FolderOpen, () =>
        {
            if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                Process.Start("explorer.exe", currentPath);
        }));

        backgroundContextMenu.Items.Add(new Separator());
        backgroundContextMenu.Items.Add(EditorUI.CreateContextMenuItem("New Folder", MaterialIconKind.FolderPlus, () =>
        {
            CreateNewAsset(true);
        }));
        backgroundContextMenu.Items.Add(EditorUI.CreateContextMenuItem("New Component", MaterialIconKind.CodeBraces, () =>
        {
            CreateNewAsset(false, "cs",
                "using DivisionEngine;\n" +
                "using DivisionEngine.Components;\n" +
                "using DivisionEngine.Components.FieldAttributes;\n" +
                "using DivisionEngine.MathLib;\n" +
                "\n" +
                "public class NewComponent : IComponent\n" +
                "{\n" +
                "   [Range(0f, 1f)] private float demoValue;\n" +
                "   \n" +
                "   public NewComponent()\n" +
                "   {\n" +
                "       demoValue = 1f;\n" +
                "   }\n" +
                "   \n" +
                "   public IComponent Clone() => new NewComponent\n" +
                "   {\n" +
                "       demoValue = demoValue,\n" +
                "   };\n" +
                "}\n"
            );
        }));
        backgroundContextMenu.Items.Add(EditorUI.CreateContextMenuItem("New Text File", MaterialIconKind.FileDocument, () =>
        {
            CreateNewAsset(false, "txt", "");
        }));
        backgroundContextMenu.Items.Add(EditorUI.CreateContextMenuItem("New JSON File", MaterialIconKind.CodeJson, () =>
        {
            CreateNewAsset(false, "json", "{\n    \n}");
        }));

        scrollViewer.ContextMenu = backgroundContextMenu;
    }

    /// <summary>
    /// Creates a temporary rename box for new file/folder creation.
    /// </summary>
    private void CreateNewAsset(bool isFolder, string extension = "", string defaultContent = "")
    {
        // Create temporary item with text box
        Border tempItem = new Border
        {
            Width = 80,
            Height = isFolder ? 85 : 85,
            Background = EditorColor.FromRGB(34, 34, 68),
            BorderThickness = new Thickness(2),
            BorderBrush = EditorColor.FromRGB(100, 100, 200),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(5),
            Padding = new Thickness(5),
        };
        StackPanel itemStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        // Icon
        MaterialIcon icon = isFolder
            ? new MaterialIcon { Kind = MaterialIconKind.Folder, Width = 48, Height = 48, Foreground = EditorColor.FromColor(ColorPalette.Mint) }
            : CreateFileIcon($".{extension}", 48);

        // Text box for name input
        TextBox nameBox = new TextBox
        {
            Text = isFolder ? "New Folder" : $"New{extension.ToUpper()}File",
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 70,
            MaxLength = 50,
            Background = EditorColor.FromRGB(40, 40, 40),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = EditorColor.FromRGB(100, 100, 100),
            Margin = new Thickness(0, 5, 0, 0)
        };

        // Extension text for files
        TextBlock? extensionText = null;
        if (!isFolder && !string.IsNullOrEmpty(extension))
        {
            extensionText = new TextBlock
            {
                Text = $".{extension.ToUpper()}",
                Foreground = Brushes.LightGray,
                FontSize = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
        }

        itemStack.Children.Add(icon);
        itemStack.Children.Add(nameBox);
        if (extensionText != null) itemStack.Children.Add(extensionText);
        tempItem.Child = itemStack;

        // Add to tile panel at the beginning
        assetsTileGrid.Children.Insert(0, tempItem);

        // Focus and select the text
        nameBox.Focus();
        nameBox.SelectAll();

        // Handle name submission
        bool completed = false;
        void CompleteCreation(bool cancel)
        {
            if (completed) return;
            completed = true;

            if (!cancel)
            {
                string newName = nameBox.Text.Trim();
                if (string.IsNullOrEmpty(newName)) cancel = true;
                else
                {
                    // Remove invalid characters
                    char[] invalidChars = Path.GetInvalidFileNameChars();
                    foreach (char c in invalidChars)
                        newName = newName.Replace(c.ToString(), "");

                    if (string.IsNullOrEmpty(newName)) cancel = true;
                    else Task.Run(() => CreateFileOrFolderAsync(newName, isFolder, extension, defaultContent));
                }
            }

            // Remove temporary item
            assetsTileGrid.Children.Remove(tempItem);
        }

        nameBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CompleteCreation(false);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CompleteCreation(true);
                e.Handled = true;
            }
        };

        nameBox.LostFocus += (s, e) =>
        {
            // Small delay to allow "Enter" key to process first
            Dispatcher.UIThread.Post(() => CompleteCreation(true), DispatcherPriority.Background);
        };

        // Update tile columns after adding
        UpdateTileColumns();
    }

    /// <summary>
    /// Creates the actual file or folder on disk.
    /// </summary>
    private async void CreateFileOrFolderAsync(string name, bool isFolder, string extension, string defaultContent)
    {
        try
        {
            if (isFolder)
            {
                string newPath = Path.Combine(currentPath, name);
                Directory.CreateDirectory(newPath);
                Debug.Info($"Created folder: {newPath}");
            }
            else
            {
                string fileName = extension == "cs" ? name : $"{name}.{extension}";
                if (!fileName.EndsWith($".{extension}")) fileName += $".{extension}";
                string newPath = Path.Combine(currentPath, fileName);

                // Write content to file
                await File.WriteAllTextAsync(newPath, defaultContent);
                Debug.Info($"Created file: {newPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"Failed to create {(isFolder ? "folder" : "file")}: {name}", ex);

            // TODO: add notify in future
            //await Dispatcher.UIThread.Post(() =>
            //{
            //    ShowNotification($"Failed to create: {ex.Message}", true);
            //});
        }
    }

    /// <summary>
    /// Called when the directory field is updated.
    /// </summary>
    private void DirectoryField_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string? newPath = directoryField.Text;
        if (!string.IsNullOrEmpty(newPath) && Directory.Exists(newPath))
        {
            currentPath = newPath;

            // Clear panels
            assetsTileGrid.Children.Clear();
            listNamePanel.Children.Clear();
            listSizePanel.Children.Clear();

            Debug.Info($"Directory field updating assets path to: {currentPath}");

            // Dispatch asset loading
            Dispatcher.UIThread.Post(() => LoadAssetsAtPathNew(newPath));
        }
    }

    /// <summary>
    /// Has the assets window load the current project assets.
    /// </summary>
    public static void LoadAssetsForCurrentProject()
    {
        ValidateWindows();
        foreach (AssetsWindow? window in currentWindows)
        {
            string? path = string.IsNullOrEmpty(window!.currentPath) ? GetDefaultAssetsPath() : window.currentPath;
            window.Setup(path);
        }
    }

    /// <summary>
    /// Has the assets window load all assets at a path.
    /// </summary>
    public static void LoadAssets(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        Debug.Log($"Loading assets at folder: {path}");
        ValidateWindows();
        foreach (AssetsWindow? window in currentWindows) window!.Setup(path);
    }

    /// <summary>
    /// Makes sure all assets windows in current list are active.
    /// </summary>
    private static void ValidateWindows()
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
            directoryField.PlaceholderText = "No Project Loaded";
            directoryField.Text = string.Empty;
            itemCountText.Text = "0 items";
            return false;
        }
        currentPath = path;

        // Clear panels
        assetsTileGrid.Children.Clear();
        listNamePanel.Children.Clear();
        listSizePanel.Children.Clear();

        // Dispatch asset loading
        if (path == directoryField.Text) Dispatcher.UIThread.Post(() => LoadAssetsAtPathNew(path));
        else directoryField.Text = path;
        return true;
    }

    /// <summary>
    /// Navigates up one level by reloading assets in the parent directory.
    /// </summary>
    /// <returns>Successful navigation up one directory level or not</returns>
    private bool NavigateUpOneLevel()
    {
        if (string.IsNullOrEmpty(currentPath)) return false;
        DirectoryInfo dir = new DirectoryInfo(currentPath);
        if (dir.Parent == null) return false;
        Dispatcher.UIThread.Post(() => Setup(dir.Parent.FullName));
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

    private void LoadAssetsAtPathNew(string path)
    {
        try
        {
            // Loads assets using the database if path is in assets folder.
            string assetsRoot = Path.Combine(ProjectManager.CurrentProjectPath!, "Assets");
            assetsListPanel.Children.Clear();
            assetsTileGrid.Children.Clear();
            if (path.StartsWith(assetsRoot)) LoadAssetsUsingDatabase(path);
            else LoadAssetsUsingFileSystem(path);
        }
        catch (Exception ex)
        {
            Debug.Error($"Failed to load assets", ex);
            ShowEmptyState($"Error: {ex.Message}");
            itemCountText.Text = "Error";
        }
    }

    /// <summary>
    /// Load assets using the AssetDatabase (project mode)
    /// </summary>
    private void LoadAssetsUsingDatabase(string path)
    {
        if (!ProjectManager.IsCurrentLoaded || AssetDatabase.Folders.Count == 0)
        {
            // Fall back to file system if database isn't available
            LoadAssetsUsingFileSystem(path);
            return;
        }

        DirectoryInfo pathInfo = new DirectoryInfo(path);
        if (!pathInfo.Exists)
        {
            ShowEmptyState("Directory does not exist");
            itemCountText.Text = "0 items";
            return;
        }

        // Calculate relative path for database queries
        string relativePath = AssetDatabase.GetProjectRelativePath(path);
        if (relativePath == ".") relativePath = "";

        // Get folders from file system (always needed)
        DirectoryInfo[] folders = pathInfo.GetDirectories();

        // Get assets from database, using relativePath
        List<AssetMetadata> assets = [.. AssetDatabase.GetAssetsInFolder(relativePath)];

        // Apply filter if not showing all
        if (currentFilter != AssetType.None)
            assets = [.. assets.Where(a => a.Type == currentFilter)];

        int totalAssets = folders.Length + assets.Count;

        // Load correct view
        if (CurrentView == ViewState.Tiles)
        {
            assetsTileGrid.IsVisible = true;
            assetsListPanel.IsVisible = false;
            scrollViewer.Content = assetsTileGrid;

            foreach (DirectoryInfo? folder in folders) CreateFolderTile(folder);
            foreach (AssetMetadata? asset in assets) CreateAssetTile(asset);

            UpdateTileColumns(); // Update columns after adding items
        }
        else
        {
            assetsTileGrid.IsVisible = false;
            assetsListPanel.IsVisible = true;
            scrollViewer.Content = assetsListPanel;

            foreach (DirectoryInfo? folder in folders) CreateFolderListItem(folder);
            foreach (AssetMetadata? asset in assets) CreateAssetListItem(asset);
        }

        itemCountText.Text = $"{totalAssets} item{(totalAssets != 1 ? "s" : "")}";
        if (totalAssets == 0) ShowEmptyState("No Assets");
    }

    /// <summary>
    /// Original file system loading (for when no project is loaded).
    /// </summary>
    private void LoadAssetsUsingFileSystem(string path)
    {
        DirectoryInfo pathInfo = new DirectoryInfo(path);
        if (!pathInfo.Exists)
        {
            ShowEmptyState("Directory does not exist");
            itemCountText.Text = "0 items";
            return;
        }

        int totalAssets = LoadViewStateInterchange(pathInfo);
        itemCountText.Text = $"{totalAssets} item{(totalAssets != 1 ? "s" : "")}";

        if (totalAssets == 0) ShowEmptyState("No Items");
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
            assetsTileGrid.IsVisible = true;
            assetsListPanel.IsVisible = false;
            scrollViewer.Content = assetsTileGrid;
            foreach (DirectoryInfo folder in folders) CreateFolderTile(folder);
            foreach (FileInfo file in files) CreateFileTile(file);

            UpdateTileColumns(); // Update columns after adding items
        }
        else
        {
            assetsTileGrid.IsVisible = false;
            assetsListPanel.IsVisible = true;
            scrollViewer.Content = assetsListPanel;
            foreach (DirectoryInfo folder in folders) CreateFolderListItem(folder);
            foreach (FileInfo file in files) CreateFileListItem(file);
        }

        return folders.Length + files.Length;
    }

    private void UpdateTileColumns()
    {
        if (CurrentView != ViewState.Tiles) return;

        // Get the actual width of the scroll viewer's viewport
        double availableWidth = scrollViewer.Bounds.Width;

        // Subtract scrollbar width if visible (usually around 15-20 pixels)
        if (scrollViewer.VerticalScrollBarVisibility == ScrollBarVisibility.Auto)
            availableWidth -= 18; // Approximate scrollbar width

        if (availableWidth > 0)
        {
            // Tile width is 90 (80 + 5+5 margin)
            int newColumns = Math.Max(1, (int)(availableWidth / 90));
            if (assetsTileGrid.Columns != newColumns) assetsTileGrid.Columns = newColumns;
        }
    }

    /// <summary>
    /// Creates a folder tile and adds it to the tiles panel.
    /// </summary>
    /// <param name="folder">Folder to add to tiles panel</param>
    private void CreateFolderTile(DirectoryInfo folder)
    {
        Border folderBorder = CreateTileBorder();
        SetupTileHoverEffects(folderBorder);

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
        folderBorder.DoubleTapped += (s, e) => Dispatcher.UIThread.Post(() => Setup(folder.FullName));

        // Add context menu
        ContextMenu contextMenu = new ContextMenu
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            BorderBrush = EditorColor.FromRGB(128, 128, 128)
        };
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Open", MaterialIconKind.FolderOpen, () => Process.Start("explorer.exe", folder.FullName)));
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Rename", MaterialIconKind.Pencil, () =>
        {
            string folderNameWithoutExt = folder.Name;
            ShowInPlaceRename(folderBorder, folderNameWithoutExt, folder.FullName, true);
        }));
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Delete", MaterialIconKind.Delete, async () =>
        {
            try
            {
                if (await ConfirmDeletion(folder.Name, "Folder", true)) Directory.Delete(folder.FullName, true);
                Debug.Info($"Deleted folder: {folder.Name}");
            }
            catch (Exception ex)
            {
                Debug.Error($"Failed to delete folder: {folder.Name}", ex);
            }
        }, Brushes.Red));

        folderBorder.ContextMenu = contextMenu;
        assetsTileGrid.Children.Add(folderBorder);
    }

    /// <summary>
    /// Creates a file tile and adds it to the tiles panel.
    /// </summary>
    /// <param name="file">File to add to tiles panel</param>
    private void CreateFileTile(FileInfo file)
    {
        Border fileBorder = CreateTileBorder();
        SetupTileHoverEffects(fileBorder);

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
        fileBorder.DoubleTapped += (s, e) => EditorUI.OpenFile(file);

        // Add context menu
        ContextMenu contextMenu = new ContextMenu
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            BorderBrush = EditorColor.FromRGB(128, 128, 128),
        };
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Open", MaterialIconKind.FileDocument, () => EditorUI.OpenFile(file)));
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Rename", MaterialIconKind.Pencil, () =>
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
            string extension = file.Extension;
            ShowInPlaceRename(fileBorder, fileNameWithoutExt, file.FullName, false, extension);
        }));
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Copy Path", MaterialIconKind.ContentCopy, () =>
        {
            DataTransfer clipboardData = new DataTransfer(); // This new clipboard thing is hella annoying wtf
            clipboardData.Add(DataTransferItem.CreateText(file.FullName));
            TopLevel.GetTopLevel(this)?.Clipboard?.SetDataAsync(clipboardData);
            Debug.Info($"Copying path: {file.FullName}");
        }));
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Delete", MaterialIconKind.Delete, async () =>
        {
            try
            {
                if (await ConfirmDeletion(file.Name, "File")) File.Delete(file.FullName);
                Debug.Info($"Deleted file: {file.Name}");
            }
            catch (Exception ex)
            {
                Debug.Error($"Failed to delete file: {file.Name}", ex);
            }
        }, Brushes.Red));

        fileBorder.ContextMenu = contextMenu;
        assetsTileGrid.Children.Add(fileBorder);
    }

    /// <summary>
    /// Creates a tile for an asset from the database.
    /// </summary>
    private void CreateAssetTile(AssetMetadata asset)
    {
        Border assetBorder = CreateTileBorder();
        SetupTileHoverEffects(assetBorder);

        StackPanel assetStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        MaterialIcon assetIcon = EditorUI.CreateAssetTypeIcon(asset.Type, 48); // Asset icon based on type
        string assetName = Path.GetFileNameWithoutExtension(asset.FileName); // Asset name (truncated if too long)
        if (assetName.Length > 12) assetName = string.Concat(assetName.AsSpan(0, 10), "..");
        TextBlock assetNameText = new TextBlock
        {
            Text = assetName,
            Foreground = Brushes.White,
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 80,
        };
        TextBlock typeText = new TextBlock // Asset type indicator
        {
            Text = GetAssetTypeDisplayName(asset.Type),
            Foreground = Brushes.LightGray,
            FontSize = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        assetStack.Children.Add(assetIcon);
        assetStack.Children.Add(assetNameText);
        assetStack.Children.Add(typeText);
        assetBorder.Child = assetStack;

        // TODO: Replace this placeholder function
        assetBorder.DoubleTapped += (s, e) =>
        {
            Debug.Info($"Asset: {asset.FileName} | Type: {asset.Type} | GUID: {asset.ID}");
        };

        // Add context menu for assets
        ContextMenu contextMenu = new ContextMenu
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            BorderBrush = EditorColor.FromRGB(128, 128, 128)
        };

        // Get full file path if possible
        string fullPath = Path.Combine(ProjectManager.CurrentProjectPath ?? "", asset.RelativePath);
        bool fileExists = File.Exists(fullPath);

        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Open", MaterialIconKind.FileDocument, () =>
        {
            if (fileExists) EditorUI.OpenFile(new FileInfo(fullPath));
        }));
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Show in Explorer", MaterialIconKind.FolderOpen, () =>
        {
            if (!string.IsNullOrEmpty(fullPath) && Directory.Exists(Path.GetDirectoryName(fullPath)))
                Process.Start("explorer.exe", Path.GetDirectoryName(fullPath)!);
        }));
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Copy GUID", MaterialIconKind.Identifier, async () =>
        {
            IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                DataTransfer clipboardData = new DataTransfer(); // New clipboard setup
                clipboardData.Add(DataTransferItem.CreateText(asset.ID));
                TopLevel.GetTopLevel(this)?.Clipboard?.SetDataAsync(clipboardData);
                Debug.Info($"Copied asset GUID: {asset.ID}");
            }
        }));
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Copy Path", MaterialIconKind.ContentCopy, async () =>
        {
            IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                DataTransfer clipboardData = new DataTransfer();
                clipboardData.Add(DataTransferItem.CreateText(asset.RelativePath));
                TopLevel.GetTopLevel(this)?.Clipboard?.SetDataAsync(clipboardData);
                Debug.Info($"Copied asset path: {asset.RelativePath}");
            }
        }));
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Rename", MaterialIconKind.Pencil, () =>
        {
            if (!fileExists) return;

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(asset.FileName);
            string extension = Path.GetExtension(asset.FileName);
            ShowInPlaceRename(assetBorder, fileNameWithoutExt, fullPath, false, extension);
        }));
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(EditorUI.CreateContextMenuItem("Delete", MaterialIconKind.Delete, async () =>
        {
            if (!fileExists) return;

            try
            {
                if (await ConfirmDeletion(asset.FileName, "Asset")) File.Delete(fullPath);
                Debug.Info($"Deleted asset: {asset.FileName}");
            }
            catch (Exception ex)
            {
                Debug.Error($"Failed to delete asset: {asset.FileName}", ex);
            }
        }, Brushes.Red));

        assetBorder.ContextMenu = contextMenu;
        assetsTileGrid.Children.Add(assetBorder);
    }

    private static Border CreateTileBorder(double width = 80, double height = 85) => new Border
    {
        Width = width,
        Height = height,
        BorderThickness = new Thickness(0, 0, 1, 1),
        BorderBrush = EditorColor.FromRGB(10, 10, 10),
        Background = EditorColor.FromRGB(20, 20, 20),
        CornerRadius = new CornerRadius(4),
        Margin = new Thickness(5),
        Padding = new Thickness(5),
        Cursor = new Cursor(StandardCursorType.Hand),
    };

    private static void SetupTileHoverEffects(Border border)
    {
        border.PointerEntered += (_, _) =>
        {
            border.BorderThickness = new Thickness(1, 0, 2, 2);
            border.BorderBrush = EditorColor.FromRGB(12, 12, 12);
            border.Background = EditorColor.FromRGB(24, 24, 24);
        };
        border.PointerExited += (_, _) =>
        {
            border.BorderThickness = new Thickness(0, 0, 1, 1);
            border.BorderBrush = EditorColor.FromRGB(10, 10, 10);
            border.Background = EditorColor.FromRGB(20, 20, 20);
        };
    }

    /// <summary>
    /// Shows a confirmation dialog for deletion.
    /// </summary>
    /// <param name="itemName">Name of the item being deleted</param>
    /// <param name="itemType">Type of item (file, folder, asset)</param>
    /// <param name="isFolder">Whether this is a folder deletion</param>
    /// <returns>True if user confirmed deletion</returns>
    private static async Task<bool> ConfirmDeletion(string itemName, string itemType, bool isFolder = false)
    {
        string message = isFolder
            ? $"Are you sure you want to delete '{itemName}' and ALL its contents?\n\nThis action cannot be undone."
            : $"Are you sure you want to delete '{itemName}'?\n\nThis action cannot be undone.";
        ConfirmationDialog confirmDialog = new ConfirmationDialog($"Delete {itemType}", message);
        return await confirmDialog.ShowDialog<bool>(App.MainWindow!);
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
    /// Creates a list item for an asset.
    /// </summary>
    private void CreateAssetListItem(AssetMetadata asset)
    {
        // Asset icon based on type
        MaterialIcon assetIcon = EditorUI.CreateAssetTypeIcon(asset.Type, 22);
        assetIcon.VerticalAlignment = VerticalAlignment.Center;
        assetIcon.Padding = new Thickness(8, 2, 0, 2);

        // Asset name and size
        string assetName = Path.GetFileNameWithoutExtension(asset.FileName);
        if (assetName.Length > 64) assetName = string.Concat(assetName.AsSpan(0, 60), "..");
        string fileSize = EditorUI.FormatFileSize(asset.FileSize);

        TextBlock assetNameText = new TextBlock
        {
            Text = assetName,
            Foreground = Brushes.White,
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(2),
        };
        TextBlock assetSizeText = new TextBlock
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
        TextBlock assetTypeText = new TextBlock
        {
            Text = GetAssetTypeDisplayName(asset.Type),
            Foreground = Brushes.LightGray,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
        };
        StackPanel listName = new StackPanel
        {
            Children = { assetIcon, assetNameText, assetTypeText },
            Spacing = 2,
            Orientation = Orientation.Horizontal,
            Height = listItemHeight,
        };
        StackPanel listSize = new StackPanel
        {
            Children = { assetSizeText },
            Spacing = 2,
            Orientation = Orientation.Horizontal,
            Height = listItemHeight,
        };

        listName.DoubleTapped += (s, e) =>
        {
            Debug.Info($"Asset: {asset.FileName} | Type: {asset.Type} | GUID: {asset.ID}");
        };
        listSize.DoubleTapped += (s, e) =>
        {
            Debug.Info($"Asset: {asset.FileName} | Type: {asset.Type} | GUID: {asset.ID}");
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
            ".obj" or ".fbx" or ".gltf" or ".glb" or ".stl" => MaterialIconKind.CubeOutline,
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
            ".obj" or ".fbx" or ".gltf" or ".glb" or ".stl" => ColorPalette.LightSeaGreen,
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

    /// <summary>
    /// Shows an in-place rename text box for a tile.
    /// </summary>
    private static void ShowInPlaceRename(Border targetBorder, string currentName, string currentFullPath, bool isFolder, string extension = "")
    {
        // Save original content
        Control? originalContent = targetBorder.Child;
        Border editBorder = new Border // Create edit UI
        {
            Width = 80,
            Height = 85,
            Background = EditorColor.FromRGB(34, 34, 68),
            BorderThickness = new Thickness(2),
            BorderBrush = EditorColor.FromRGB(100, 100, 200),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(5),
            Padding = new Thickness(5),
        };
        StackPanel editStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        MaterialIcon icon; // Keep the same icon
        if (isFolder)
        {
            icon = new MaterialIcon
            {
                Kind = MaterialIconKind.Folder,
                Width = 48,
                Height = 48,
                Foreground = EditorColor.FromColor(ColorPalette.Mint),
                Margin = new Thickness(0, 0, 0, 5)
            };
        }
        else
        {
            icon = CreateFileIcon(extension, 48);
            icon.Margin = new Thickness(0, 0, 0, 5);
        }

        TextBox nameBox = new TextBox // Text box for name input
        {
            Text = currentName,
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 70,
            MaxLength = 50,
            Background = EditorColor.FromRGB(40, 40, 40),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = EditorColor.FromRGB(100, 100, 100),
            Margin = new Thickness(0, 5, 0, 0)
        };

        editStack.Children.Add(icon);
        editStack.Children.Add(nameBox);

        if (!isFolder && !string.IsNullOrEmpty(extension))
        {
            TextBlock extensionText = new TextBlock
            {
                Text = extension.ToUpper(),
                Foreground = Brushes.LightGray,
                FontSize = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            editStack.Children.Add(extensionText);
        }

        editBorder.Child = editStack;
        targetBorder.Child = editBorder; // Replace the border content

        // Focus and select text
        nameBox.Focus();
        nameBox.SelectAll();

        bool completed = false;
        void CompleteRename(bool cancel)
        {
            if (completed) return;
            completed = true;

            if (!cancel)
            {
                string newName = nameBox.Text.Trim();
                if (string.IsNullOrEmpty(newName)) cancel = true;
                else
                {
                    // Remove invalid characters
                    char[] invalidChars = Path.GetInvalidFileNameChars();
                    foreach (char c in invalidChars) newName = newName.Replace(c.ToString(), "");

                    if (string.IsNullOrEmpty(newName)) cancel = true;
                    else
                    {
                        // Perform rename
                        string directory = Path.GetDirectoryName(currentFullPath)!;
                        string newPath;

                        if (isFolder)
                        {
                            newPath = Path.Combine(directory, newName);
                            if (newPath != currentFullPath)
                            {
                                try
                                {
                                    Directory.Move(currentFullPath, newPath);
                                    Debug.Info($"Renamed folder: {Path.GetFileName(currentFullPath)} -> {newName}");
                                }
                                catch (Exception ex)
                                {
                                    Debug.Error($"Failed to rename folder: {Path.GetFileName(currentFullPath)}", ex);
                                }
                            }
                        }
                        else
                        {
                            newPath = Path.Combine(directory, newName + extension);
                            if (newPath != currentFullPath)
                            {
                                try
                                {
                                    File.Move(currentFullPath, newPath);
                                    Debug.Info($"Renamed file: {Path.GetFileName(currentFullPath)} -> {newName + extension}");
                                }
                                catch (Exception ex)
                                {
                                    Debug.Error($"Failed to rename file: {Path.GetFileName(currentFullPath)}", ex);
                                }
                            }
                        }
                    }
                }
            }

            // Restore original content
            targetBorder.Child = originalContent;
        }

        nameBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CompleteRename(false);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CompleteRename(true);
                e.Handled = true;
            }
        };
        nameBox.LostFocus += (s, e) => Dispatcher.UIThread.Post(() => CompleteRename(true), DispatcherPriority.Background);
    }

    /// <summary>
    /// Gets a display name for asset type.
    /// </summary>
    private static string GetAssetTypeDisplayName(AssetType type) => type switch
    {
        AssetType.Texture => "Texture",
        AssetType.SDF => "SDF",
        AssetType.Material => "Material",
        AssetType.Script => "Script",
        AssetType.Audio => "Audio",
        AssetType.Font => "Font",
        _ => "Asset",
    };

    /// <summary>
    /// Shows an empty state in the display panel area.
    /// </summary>
    /// <param name="message">Empty message to display</param>
    private void ShowEmptyState(string message)
    {
        assetsTileGrid.Children.Clear();
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
        
        if (CurrentView == ViewState.Tiles) assetsTileGrid.Children.Add(emptyStack);
        else listNamePanel.Children.Add(emptyStack);
    }

    /// <summary>
    /// Called when a project is loaded.
    /// </summary>
    private static void OnProjectLoaded()
    {
        ValidateWindows();
        foreach (AssetsWindow? window in currentWindows) window?.OnProjectLoadedInternal();
    }

    /// <summary>
    /// Called when a project is about to close.
    /// </summary>
    private static void OnProjectClosing()
    {
        ValidateWindows();
        foreach (AssetsWindow? window in currentWindows) window?.OnProjectClosingInternal();
    }

    /// <summary>
    /// Called when a project is closed.
    /// </summary>
    private static void OnProjectClosed()
    {
        ValidateWindows();
        foreach (AssetsWindow? window in currentWindows) window?.OnProjectClosedInternal();
    }

    /// <summary>
    /// Instance method for project loaded.
    /// </summary>
    private void OnProjectLoadedInternal()
    {
        if (!subscribedToFolderEvents)
        {
            Debug.Warning("Subscribed to asset folder change event");
            AssetDatabase.FolderChanged += OnAssetFolderChanged;
            subscribedToFolderEvents = true;
        }

        // Navigate to the Assets folder root
        string assetsPath = Path.Combine(ProjectManager.CurrentProjectPath!, "Assets");
        if (Directory.Exists(assetsPath)) Dispatcher.UIThread.Post(() => LoadAssets(assetsPath));
    }

    /// <summary>
    /// Instance method for project closing.
    /// </summary>
    private void OnProjectClosingInternal()
    {
        // Optional: Show saving indicator, etc.
        Dispatcher.UIThread.Post(() => {
            //Debug.Info("Project closing, preparing assets window...");
        });
    }

    /// <summary>
    /// Instance method for project closed.
    /// </summary>
    private void OnProjectClosedInternal()
    {
        if (subscribedToFolderEvents)
        {
            Debug.Warning("Unsubscribed from asset folder change event");
            AssetDatabase.FolderChanged -= OnAssetFolderChanged;
            subscribedToFolderEvents = false;
        }

        // Clear and show empty state
        Dispatcher.UIThread.Post(() => {
            currentPath = string.Empty;
            directoryField.PlaceholderText = "No Project Loaded";
            directoryField.Text = string.Empty;
            itemCountText.Text = "0 items";
            ShowEmptyState("No Project Loaded");
        });
    }

    // Handle folder changes
    private void OnAssetFolderChanged(string folderPath)
    {
        if (string.IsNullOrEmpty(currentPath)) return;
        if (!IsSameOrAncestorPath(currentPath, folderPath)) return;

        Dispatcher.UIThread.Post(() => LoadAssetsAtPathNew(currentPath), DispatcherPriority.Background);
    }

    private static bool IsSameOrAncestorPath(string viewedPath, string changedPath)
    {
        string a = Path.TrimEndingDirectorySeparator(Path.GetFullPath(viewedPath));
        string b = Path.TrimEndingDirectorySeparator(Path.GetFullPath(changedPath));

        if (a.Equals(b, StringComparison.OrdinalIgnoreCase)) return true;

        return b.StartsWith(a + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || a.StartsWith(b + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}