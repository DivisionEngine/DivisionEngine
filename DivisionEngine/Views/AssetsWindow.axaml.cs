//
// Copyright (C) 2026 Rex Woodfield
//
// This file is part of Division Engine.
//
// Division Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Division Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Division Engine.  If not, see <https://www.gnu.org/licenses/>.
//
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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

    /// <summary>
    /// Current view state of this assets window.
    /// </summary>
    public ViewState CurrentView { get; private set; }

    // Window vars
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
    private readonly TextBox directoryField;
    private readonly TextBlock itemCountText;
    private readonly Button upDirButton;
    private readonly Button viewButton;
    private readonly MaterialIcon viewButtonIcon;

    // Data vars
    private string currentPath;
    private static bool subscribedProjectEvents = false; // Track if subscribed to events
    private bool inProjectMode => ProjectManager.IsCurrentLoaded; // Track if in project mode
    private string curRelativeDBPath = ""; // Relative path for database queries
    private bool subscribedDatabaseEvents = false;

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
            Spacing = 2,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Top,
        };
        directoryField = new TextBox
        {
            Text = string.Empty,
            Watermark = "No Project Loaded",
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
        directoryField.TextChanged += DirectoryField_TextChanged;
        upDirButton.Click += (s, e) => NavigateUpOneLevel();
        viewButton.Click += (s, e) => ToggleViewState();
        header.Children.Add(upDirButton);
        header.Children.Add(viewButton);
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
    /// Called when the director field is updated.
    /// </summary>
    private void DirectoryField_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string? newPath = directoryField.Text;
        if (!string.IsNullOrEmpty(newPath) && Directory.Exists(newPath))
        {
            currentPath = newPath;

            // Clear panels
            assetsTilePanel.Children.Clear();
            listNamePanel.Children.Clear();
            listSizePanel.Children.Clear();

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
        foreach (AssetsWindow? window in currentWindows) window!.Setup(ProjectManager.CurrentProjectPath);
    }

    /// <summary>
    /// Has the assets window load all assets at a path.
    /// </summary>
    public static void LoadAssets(string path)
    {
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
            directoryField.Watermark = "No Project Loaded";
            directoryField.Text = string.Empty;
            itemCountText.Text = "0 items";
            return false;
        }
        currentPath = path;

        if (inProjectMode) // Check if in project mode
        {
            string assetsRoot = Path.Combine(ProjectManager.CurrentProjectPath!, "Assets");
            if (path.StartsWith(assetsRoot))
            {
                curRelativeDBPath = Path.GetRelativePath(assetsRoot, path);
                if (curRelativeDBPath == ".") curRelativeDBPath = "";
            }
            else
            {
                // Path is outside Assets folder, still display but cannot use asset DB
                curRelativeDBPath = "";
            }
        }

        // Clear panels
        assetsTilePanel.Children.Clear();
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
    //private void LoadAssetsAtPath(string path)
    //{
    //    try
    //    {
    //        DirectoryInfo pathInfo = new DirectoryInfo(path);
    //        if (!pathInfo.Exists)
    //        {
    //            ShowEmptyState("Directory does not exist");
    //            itemCountText.Text = "0 items";
    //            return;
    //        }

    //        // Load correct view
    //        int totalAssets = LoadViewStateInterchange(pathInfo);

    //        // Update count
    //        itemCountText.Text = $"{totalAssets} item{(totalAssets != 1 ? "s" : "")}";

    //        // Show empty state if no assets
    //        if (totalAssets == 0) ShowEmptyState("No Items");
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.Error($"Failed to load assets", ex);
    //        ShowEmptyState($"Error: {ex.Message}");
    //        itemCountText.Text = "Error";
    //    }
    //}

    private void LoadAssetsAtPathNew(string path)
    {
        try
        {
            // If we're in project mode and this path is within the Assets folder,
            // use the AssetDatabase for files
            if (inProjectMode)
            {
                string assetsRoot = Path.Combine(ProjectManager.CurrentProjectPath!, "Assets");
                if (path.StartsWith(assetsRoot))
                {
                    LoadAssetsUsingDatabase(path);
                    return;
                }
            }

            // Fall back to file system mode
            LoadAssetsUsingFileSystem(path);
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
        AssetDatabase? database = ProjectManager.AssetDatabase;
        if (database == null)
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
        string assetsRoot = Path.Combine(ProjectManager.CurrentProjectPath!, "Assets");
        string relativePath = Path.GetRelativePath(assetsRoot, path);
        if (relativePath == ".") relativePath = "";

        // Get folders from file system (always needed)
        DirectoryInfo[] folders = pathInfo.GetDirectories();

        // Get assets from database
        List<AssetMetadata> assets = [.. database.GetAssetsInFolder(relativePath)];
        int totalAssets = folders.Length + assets.Count;

        // Load correct view
        if (CurrentView == ViewState.Tiles)
        {
            assetsTilePanel.IsVisible = true;
            assetsListPanel.IsVisible = false;
            scrollViewer.Content = assetsTilePanel;

            foreach (DirectoryInfo? folder in folders) CreateFolderTile(folder);
            foreach (AssetMetadata? asset in assets) CreateAssetTile(asset);
        }
        else
        {
            assetsTilePanel.IsVisible = false;
            assetsListPanel.IsVisible = true;
            scrollViewer.Content = assetsListPanel;

            foreach (DirectoryInfo? folder in folders) CreateFolderListItem(folder);
            foreach (AssetMetadata? asset in assets) CreateAssetListItem(asset);
        }

        itemCountText.Text = $"{totalAssets} item{(totalAssets != 1 ? "s" : "")}";
        if (totalAssets == 0) ShowEmptyState("No Items");
    }

    /// <summary>
    /// Original file system loading (for when no project is loaded)
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
    /// Creates a tile for an asset from the database.
    /// </summary>
    private void CreateAssetTile(AssetMetadata asset)
    {
        Border assetBorder = new Border
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
        StackPanel assetStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        MaterialIcon assetIcon = CreateAssetTypeIcon(asset.Type, 48); // Asset icon based on type
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
            Text = GetAssetTypeShortName(asset.Type),
            Foreground = Brushes.LightGray,
            FontSize = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        assetStack.Children.Add(assetIcon);
        assetStack.Children.Add(assetNameText);
        assetStack.Children.Add(typeText);
        assetBorder.Child = assetStack;

        // Hover effect
        assetBorder.PointerEntered += (s, e) => { assetBorder.Background = EditorColor.FromRGB(17, 17, 17); };
        assetBorder.PointerExited += (s, e) => { assetBorder.Background = EditorColor.FromRGB(24, 24, 24); };

        // Double-click to show info (placeholder)
        assetBorder.DoubleTapped += (s, e) =>
        {
            Debug.Info($"Asset: {asset.FileName} | Type: {asset.Type} | GUID: {asset.ID}");
        };
        assetsTilePanel.Children.Add(assetBorder);
    }

    /// <summary>
    /// Creates a list item for an asset.
    /// </summary>
    private void CreateAssetListItem(AssetMetadata asset)
    {
        // Asset icon based on type
        MaterialIcon assetIcon = CreateAssetTypeIcon(asset.Type, 22);
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
    /// Creates an icon for an asset type.
    /// </summary>
    private static MaterialIcon CreateAssetTypeIcon(AssetType type, double size)
    {
        MaterialIconKind iconKind = type switch
        {
            AssetType.Texture => MaterialIconKind.Image,
            AssetType.SDF => MaterialIconKind.CubeOutline,
            AssetType.Material => MaterialIconKind.Palette,
            AssetType.Script => MaterialIconKind.CodeBraces,
            AssetType.Audio => MaterialIconKind.Audio,
            AssetType.Font => MaterialIconKind.FormatFont,
            _ => MaterialIconKind.FileDocument,
        };
        float4 iconColor = type switch
        {
            AssetType.Texture => ColorPalette.SkyBlue,
            AssetType.SDF => ColorPalette.LightSeaGreen,
            AssetType.Material => ColorPalette.Orange,
            AssetType.Script => ColorPalette.Khaki,
            AssetType.Audio => ColorPalette.Coral,
            AssetType.Font => ColorPalette.Khaki,
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
    /// Gets a short display name for asset type.
    /// </summary>
    private static string GetAssetTypeShortName(AssetType type) => type switch
    {
        AssetType.Texture => "TEX",
        AssetType.SDF => "SDF",
        AssetType.Material => "MAT",
        AssetType.Script => "SCR",
        AssetType.Audio => "AUD",
        AssetType.Font => "FNT",
        _ => "AST",
    };

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

    /// <summary>
    /// Called when a project is loaded.
    /// </summary>
    private static void OnProjectLoaded()
    {
        ValidateWindows();
        foreach (AssetsWindow? window in currentWindows)
            window?.OnProjectLoadedInternal();
    }

    /// <summary>
    /// Called when a project is about to close.
    /// </summary>
    private static void OnProjectClosing()
    {
        ValidateWindows();
        foreach (AssetsWindow? window in currentWindows)
            window?.OnProjectClosingInternal();
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
        // Navigate to the Assets folder root
        string assetsPath = Path.Combine(ProjectManager.CurrentProjectPath!, "Assets");
        if (Directory.Exists(assetsPath))
        {
            // Subscribe to database events
            if (!subscribedDatabaseEvents && ProjectManager.AssetDatabase != null)
            {
                ProjectManager.AssetDatabase.FolderChanged += OnAssetFolderChanged;
                subscribedDatabaseEvents = true;
            }

            Dispatcher.UIThread.Post(() => LoadAssets(assetsPath));
        }
    }

    /// <summary>
    /// Instance method for project closing.
    /// </summary>
    private void OnProjectClosingInternal()
    {
        // Optional: Show saving indicator, etc.
        Dispatcher.UIThread.Post(() => {
            Debug.Info("Project closing, preparing assets window...");
        });
    }

    /// <summary>
    /// Instance method for project closed.
    /// </summary>
    private void OnProjectClosedInternal()
    {
        // Unsubscribe from database events
        if (subscribedDatabaseEvents && ProjectManager.AssetDatabase != null)
        {
            ProjectManager.AssetDatabase.FolderChanged -= OnAssetFolderChanged;
            subscribedDatabaseEvents = false;
        }

        // Clear and show empty state
        Dispatcher.UIThread.Post(() => {
            currentPath = string.Empty;
            curRelativeDBPath = "";
            directoryField.Watermark = "No Project Loaded";
            directoryField.Text = string.Empty;
            itemCountText.Text = "0 items";
            ShowEmptyState("No Project Loaded");
        });
    }

    // Handle folder changes
    private void OnAssetFolderChanged(string folderPath)
    {
        // Check if this folder is the one we're currently viewing
        if (!string.IsNullOrEmpty(currentPath) && folderPath.StartsWith(currentPath))
        {
            // Refresh the current view
            Dispatcher.UIThread.Post(() => LoadAssets(currentPath));
        }
    }
}