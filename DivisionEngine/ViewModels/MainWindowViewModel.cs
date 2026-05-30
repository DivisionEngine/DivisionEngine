//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using DivisionEngine.Editor.Settings;
using DivisionEngine.Editor.Tasks;
using DivisionEngine.Projects;
using DivisionEngine.Settings;
using Material.Icons.Avalonia;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DivisionEngine.Editor.ViewModels
{
    /// <summary>
    /// The view model for the Division Engine editor's parent window.
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        // Window storage
        public static MainWindowViewModel? vm;
        private readonly Window mainWindow;

        // Main Window Menu Commands
        public Action? RequestClose { get; set; }

        // Editor window tab collections
        public ObservableCollection<EditorWindowViewModel> CenterTabs { get; } = [];
        public ObservableCollection<EditorWindowViewModel> BottomTabs { get; } = [];
        public ObservableCollection<EditorWindowViewModel> LeftTabs { get; } = [];
        public ObservableCollection<EditorWindowViewModel> RightTabs { get; } = [];

        // Recent projects
        private ObservableCollection<string> recentProjects = [];
        public ObservableCollection<string> RecentProjects
        {
            get => recentProjects;
            set => this.RaiseAndSetIfChanged(ref recentProjects, value);
        }
        private ObservableCollection<object> recentProjectMenuItems = [];
        public ObservableCollection<object> RecentProjectMenuItems
        {
            get => recentProjectMenuItems;
            set => this.RaiseAndSetIfChanged(ref recentProjectMenuItems, value);
        }

        public bool HasRecentProjects => RecentProjects.Count > 0;

        // Tab selection variables
        private EditorWindowViewModel? selectedLeftTab;
        private EditorWindowViewModel? selectedRightTab;
        private EditorWindowViewModel? selectedCenterTab;
        private EditorWindowViewModel? selectedBottomTab;

        public EditorWindowViewModel? SelectedLeftTab
        {
            get => selectedLeftTab;
            set => this.RaiseAndSetIfChanged(ref selectedLeftTab, value);
        }
        public EditorWindowViewModel? SelectedRightTab
        {
            get => selectedRightTab;
            set => this.RaiseAndSetIfChanged(ref selectedRightTab, value);
        }
        public EditorWindowViewModel? SelectedCenterTab
        {
            get => selectedCenterTab;
            set => this.RaiseAndSetIfChanged(ref selectedCenterTab, value);
        }
        public EditorWindowViewModel? SelectedBottomTab
        {
            get => selectedBottomTab;
            set => this.RaiseAndSetIfChanged(ref selectedBottomTab, value);
        }

        // Main Window API

        private double? progressVal;
        private bool? showProgress;
        private string? recentControlsText;

        /// <summary>
        /// Editor progress bar value between 0.0 and 1.0.
        /// </summary>
        public double? ProgressValue
        {
            get => progressVal;
            set => this.RaiseAndSetIfChanged(ref progressVal, value);
        }

        /// <summary>
        /// Enables or disables the progress bar.
        /// </summary>
        public bool? ShowProgress
        {
            get => showProgress;
            set => this.RaiseAndSetIfChanged(ref showProgress, value);
        }

        /// <summary>
        /// States what the recent controls text should be.
        /// </summary>
        public string? RecentControlsText
        {
            get => recentControlsText;
            set => this.RaiseAndSetIfChanged(ref recentControlsText, value);
        }

        /// <summary>
        /// Builds the main window view model and initializes default tabs.
        /// </summary>
        public MainWindowViewModel(Window mainWindow)
        {
            this.mainWindow = mainWindow;
            vm = this;

            // Initialize default tabs
            LeftTabs.Add(new WorldWindowViewModel());

            CenterTabs.Add(new EnvironmentWindowViewModel());

            RightTabs.Add(new PropertiesWindowViewModel());
            RightTabs.Add(new SettingsWindowViewModel());

            BottomTabs.Add(new AssetsWindowViewModel());
            BottomTabs.Add(new ConsoleWindowViewModel());
            BottomTabs.Add(new DeveloperWindowViewModel());

            LoadRecentProjects(); // Load recent projects
        }

        private void LoadRecentProjects()
        {
            EditorSettings settings = EditorSettings.Instance;
            RecentProjects = new ObservableCollection<string>(settings.RecentProjects);
            this.RaisePropertyChanged(nameof(HasRecentProjects));
            UpdateRecentProjectMenuItems();

            Debug.Info($"Loaded {RecentProjects.Count} recent projects");
        }

        private void AddToRecentProjects(string path)
        {
            EditorSettings.Instance.AddRecentProject(path);
            SettingsManager.SaveSettings(EditorSettings.Instance);
            LoadRecentProjects(); // Reload to update the collection
        }

        private void UpdateRecentProjectMenuItems()
        {
            ObservableCollection<object> items = [];

            // Add project items
            foreach (string project in RecentProjects)
            {
                MenuItem menuItem = new MenuItem
                {
                    Foreground = Brushes.White,
                    Command = OpenRecentProjectCommand,
                    CommandParameter = project,
                    Header = CreateRecentProjectHeader(project),
                };
                items.Add(menuItem);
            }

            // Add separator and clear all if there are items
            if (items.Count > 0)
            {
                items.Add(new Separator());
                MenuItem clearMenuItem = new MenuItem
                {
                    Foreground = Brushes.White,
                    Command = ClearRecentProjectsCommand,
                    Header = CreateClearAllHeader(),
                };
                items.Add(clearMenuItem);
            }
            RecentProjectMenuItems = items;
        }

        private static StackPanel CreateRecentProjectHeader(string projectPath)
        {
            StackPanel displayPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(2, 2, 10, 2),
            };
            displayPanel.Children.Add(new MaterialIcon
            {
                Kind = Material.Icons.MaterialIconKind.Folder,
                Width = 16,
                Height = 16,
                Foreground = EditorColor.FromRGB(200, 200, 200),
                VerticalAlignment = VerticalAlignment.Center,
            });

            StackPanel textPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 0,
            };
            textPanel.Children.Add(new TextBlock
            {
                Text = Path.GetFileName(projectPath),
                FontWeight = FontWeight.SemiBold,
                FontSize = 12,
                Foreground = Brushes.White,
            });
            textPanel.Children.Add(new TextBlock
            {
                Text = Path.GetDirectoryName(projectPath) ?? "",
                FontSize = 10,
                Foreground = Brushes.Gray,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 300,
            });

            displayPanel.Children.Add(textPanel);
            return displayPanel;
        }

        private static StackPanel CreateClearAllHeader()
        {
            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
            };
            panel.Children.Add(new MaterialIcon
            {
                Kind = Material.Icons.MaterialIconKind.Delete,
                Width = 14,
                Height = 14,
                Foreground = EditorColor.FromRGB(200, 128, 128),
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Clear All",
                Foreground = Brushes.White,
            });
            return panel;
        }

        [RelayCommand]
        private async Task OpenProject()
        {
            try
            {
                await App.SetEditorRenderingAsync(false);

                var suggestedLocation = await GetSuggestedProjectLocation(); // Get the starting folder
                var result = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Open Project Folder",
                    AllowMultiple = false,
                    SuggestedStartLocation = suggestedLocation
                });

                if (result.Count > 0 && !string.IsNullOrEmpty(result[0].Path.LocalPath))
                {
                    string projectPath = result[0].Path.LocalPath;
                    projectPath = Path.TrimEndingDirectorySeparator(projectPath);
                    await OpenProjectAtPath(projectPath);
                }

                await App.SetEditorRenderingAsync(true);
            }
            catch (Exception ex)
            {
                Debug.Error($"Error opening project", ex);
                await App.SetEditorRenderingAsync(true);
            }
        }

        /// <summary>
        /// Determines the best starting folder for the open project dialog.
        /// </summary>
        private async Task<IStorageFolder?> GetSuggestedProjectLocation()
        {
            try
            {
                // Use the most recent project's folder (if it exists)
                if (RecentProjects.Count > 0)
                {
                    string mostRecentProject = RecentProjects[0];
                    string? parentDirectory = Path.GetDirectoryName(mostRecentProject);
                    if (!string.IsNullOrEmpty(parentDirectory) && Directory.Exists(parentDirectory))
                    {
                        var parentFolder = await mainWindow.StorageProvider.TryGetFolderFromPathAsync(parentDirectory);
                        if (parentFolder != null)
                        {
                            Debug.Info($"Using parent folder of most recent project: {parentDirectory}");
                            return parentFolder;
                        }
                    }
                }

                // Fallback to Documents folder
                var documents = await mainWindow.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
                if (documents != null)
                {
                    Debug.Info("Using Documents folder as fallback");
                    return documents;
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"Error determining suggested project location", ex);
            }

            return null;
        }

        [RelayCommand]
        private async Task OpenRecentProject(string path)
        {
            try
            {
                await App.SetEditorRenderingAsync(false);
                await OpenProjectAtPath(path);
                await App.SetEditorRenderingAsync(true);
            }
            catch (Exception ex)
            {
                Debug.Error($"Error opening recent project: {path}", ex);
                await App.SetEditorRenderingAsync(true);
            }
        }

        private async Task OpenProjectAtPath(string projectPath)
        {
            if (ProjectManager.IsDivisionProject(projectPath)) // Check if this is a valid project directory
            {
                bool success = ProjectManager.LoadProject(projectPath);
                if (success) // Check if loaded project
                {
                    AddToRecentProjects(projectPath);
                    AssetsWindow.LoadAssetsForCurrentProject();
                }
                else Debug.Error($"Failed to load project: {projectPath}");
            }
            else Debug.Info("Selected folder is not a valid Division Engine project");
        }

        [RelayCommand]
        private void ClearRecentProjects()
        {
            EditorSettings.Instance.ClearRecentProjects();
            SettingsManager.SaveSettings(EditorSettings.Instance); // Save immediately 
            LoadRecentProjects(); // Reload in ViewModel
            this.RaisePropertyChanged(nameof(HasRecentProjects)); // Force UI update
        }

        [RelayCommand]
        private async Task SaveProject()
        {
            try
            {
                if (ProjectManager.IsCurrentLoaded) ProjectManager.SaveCurrentProject();
                else await SaveProjectAs();
            }
            catch (Exception ex)
            {
                Debug.Error($"Error saving project", ex);
            }
        }

        [RelayCommand]
        private async Task SaveProjectAs()
        {
            EditorTask t = EditorTaskManager.Create("Saving Project", "Creating new project...", 0f);
            ShowProgress = true;
            try
            {
                await App.SetEditorRenderingAsync(false);
                EditorTaskManager.Update(t.Id, 0.25f);

                // Get project name
                ProjectNameDialog projectNameDialog = new ProjectNameDialog();
                string? projectName = await projectNameDialog.ShowDialog<string>(mainWindow);
                EditorTaskManager.Update(t.Id, 0.5f);

                if (string.IsNullOrWhiteSpace(projectName))
                {
                    await App.SetEditorRenderingAsync(true);
                    EditorTaskManager.Complete(t.Id);
                    return;
                }

                // Get suggested location for saving
                var suggestedLocation = await GetSuggestedProjectLocation();

                // Choose folder location
                var folderResult = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Project Location",
                    AllowMultiple = false,
                    SuggestedStartLocation = suggestedLocation
                });
                EditorTaskManager.Update(t.Id, 0.75f);

                if (folderResult.Count == 0 || string.IsNullOrEmpty(folderResult[0].Path.LocalPath))
                {
                    await App.SetEditorRenderingAsync(true);
                    EditorTaskManager.Complete(t.Id);
                    return;
                }

                string selectedFolder = folderResult[0].Path.LocalPath;
                string projectPath = Path.Combine(selectedFolder, projectName);

                // Check if folder already exists
                if (Directory.Exists(projectPath) && Directory.GetFiles(projectPath, "*.divp").Length > 0)
                {
                    ConfirmationDialog confirmDialog = new ConfirmationDialog("Project Exists",
                        $"A project named '{projectName}' already exists at this location. Overwrite?");
                    bool overwrite = await confirmDialog.ShowDialog<bool>(mainWindow);
                    EditorTaskManager.Update(t.Id, 1f);
                    if (!overwrite)
                    {
                        await App.SetEditorRenderingAsync(true);
                        EditorTaskManager.Complete(t.Id);
                        return;
                    }
                }

                // Save project
                bool success = ProjectManager.SaveNewProject(projectName, projectPath);
                if (success)
                {
                    AddToRecentProjects(projectPath);
                    AssetsWindow.LoadAssetsForCurrentProject();
                }
                else Debug.Error("Failed to save project");
                EditorTaskManager.Complete(t.Id);

                await App.SetEditorRenderingAsync(true);
            }
            catch (Exception ex)
            {
                Debug.Error($"Error saving project", ex);
                EditorTaskManager.Complete(t.Id);
            }
        }

        [RelayCommand]
        private void Exit()
        {
            Debug.Info("Exiting Division Engine Editor");
            RequestClose?.Invoke();
        }

        [RelayCommand]
        private void Undo()
        {
            Debug.Info("Undo Triggered");
            // Implement Undo functionality here
        }

        [RelayCommand]
        private void Redo()
        {
            Debug.Info("Redo Triggered: Division Engine is an SDF-based game engine written entirely in C#. Utilizing Avalonia UI for the interface and Silk.NET for native rendering, Division Engine features a comprehensive build pipeline that dynamically builds HLSL shader code from .NET code, thanks to a library called ComputeSharp.\r\n\r\nNote: This engine is still in preview and has known issues; it is specifically for experimentation and education only.\r\n\r\nThe render pipeline is built using an OpenGL backend with HLSL shaders written in C# using ComputeSharp.\r\n\r\nPicture this:\r\n\r\nSDF-based rendering\r\nGPU compute acceleration in C#\r\nOpen source\r\nECS backend, fast data handling\r\nConvenient editor tooling\r\nEditor Preview Screenshots:\r\nScreenshot 2025-12-01 163210 Screenshot 2025-12-23 200053\r\nWhat Are SDFs?\r\nSigned Distance Fields are spatial fields that store information represented as a grid sampling of the closest distance to the surface of an object defined as a polygonal model. Usually, the convention of using negative values inside the object and positive values outside the object is applied. Signed distance fields are important in computer graphics and related fields. Often, they are used for collision detection in cloth animation, soft-body physics effects, malleable geometry, volumetric effects, and fluid simulation. (https://developer.nvidia.com/gpugems/gpugems3/part-v-physics-simulation/chapter-34-signed-distance-fields-using-single-pass-gpu)\r\n\r\nHow to Work with ECS\r\nECS or an entity-component-system framework is a way of organizing game data such that it is memory efficient and hyper-performant. Entities are simply IDs with components stored as a dictionary in an \"ECS World\" object. Systems are code files written that operate on an awake --> update --> fixed update --> render schedule, allowing components to be manipulated during different engine loops/stages. For more information on ECS, check out how the Unity game engine implemented its ECS framework here: https://unity.com/ecs\r\n\r\nResources:\r\nFollow the development: https://trello.com/b/mWtyHBMf/division-engine\r\n\r\nTutorials by Inigo Quilez (Not sponsored, just useful for learning constructive geometry):\r\n\r\nBuild mathematical worlds: https://youtu.be/0ifChJ0nJfM?si=ypKU1rz-8JloPlj2\r\nBuild a 3D landscape: https://youtu.be/BFld4EBO2RE?si=EASXvq-ez2qBOIHN\r\nPaint a 3D character with math: https://youtu.be/8--5LwHRhjk?si=fH9QwvCz6dLptHE1\r\nFramework\r\nDivision Engine is built using three core packages: Silk.NET, ComputeSharp, and AvaloniaUI. Check them out here:\r\n\r\nSilk.NET\r\nComputeSharp\r\nAvaloniaUI");
            // Implement Redo functionality here
        }

        [RelayCommand]
        private static void About()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/DivisionEngine/DivisionEngine",
                UseShellExecute = true,
            });
        }

        [RelayCommand]
        private static void Roadmap()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://trello.com/b/mWtyHBMf/division-engine",
                UseShellExecute = true,
            });
        }

        [RelayCommand]
        private static void Forum()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/orgs/DivisionEngine/discussions",
                UseShellExecute = true,
            });
        }

        /// <summary>
        /// Creates an entity straight from the "Add" menu.
        /// </summary>
        /// <param name="entityType">Entity type to add (as camel case string key)</param>
        [RelayCommand]
        private static void CreateEntity(string entityType) => EditorUI.CreateEntityStatic(entityType);

        /// <summary>
        /// Adds a new window to a tab list on the main window.
        /// </summary>
        /// <param name="param">Window key to add</param>
        [RelayCommand]
        private void AddWindowToTab(string param)
        {
            string[] args = param.Split(',');

            EditorWindowViewModel? vm = args[0] switch
            {
                "Assets" => new AssetsWindowViewModel(),
                "Console" => new ConsoleWindowViewModel(),
                "Environment" => new EnvironmentWindowViewModel(),
                "Properties" => new PropertiesWindowViewModel(),
                "World" => new WorldWindowViewModel(),
                "Settings" => new SettingsWindowViewModel(),
                "Developer" => new DeveloperWindowViewModel(),
                _ => null
            };

            if (args[0] == "Environment" && !App.RendererVisible) // Re-enable render window if disabled.
                _ = App.SetEditorRenderingAsync(true);

            if (vm == null)
            {
                Debug.Error($"Unknown window type: {args[0]}");
                return;
            }

            switch (args[1])
            {
                case "left":
                    LeftTabs.Add(vm);
                    SelectedLeftTab = vm;
                    break;
                case "right":
                    RightTabs.Add(vm);
                    SelectedRightTab = vm;
                    break;
                case "bottom":
                    BottomTabs.Add(vm);
                    SelectedBottomTab = vm;
                    break;
                default:
                    CenterTabs.Add(vm);
                    SelectedCenterTab = vm;
                    break;
            }
        }

        /// <summary>
        /// Adds a window to the specified panel.
        /// </summary>
        public void AddWindowToPanel(EditorWindowViewModel viewModel, string panelType)
        {
            // Re-enable renderer if needed (special case for Environment window)
            if (viewModel is EnvironmentWindowViewModel && !App.RendererVisible)
                _ = App.SetEditorRenderingAsync(true);

            switch (panelType)
            {
                case "left":
                    LeftTabs.Add(viewModel);
                    SelectedLeftTab = viewModel;
                    break;
                case "right":
                    RightTabs.Add(viewModel);
                    SelectedRightTab = viewModel;
                    break;
                case "bottom":
                    BottomTabs.Add(viewModel);
                    SelectedBottomTab = viewModel;
                    break;
                default:
                    CenterTabs.Add(viewModel);
                    SelectedCenterTab = viewModel;
                    break;
            }
        }

        /// <summary>
        /// Called when a tab must be closed on one of the main panel areas.
        /// </summary>
        /// <param name="vm">Tab type to close</param>
        [RelayCommand]
        private void CloseTab(EditorWindowViewModel? vm)
        {
            if (vm is null) return;
            else if (LeftTabs.Remove(vm))
            {
                if (LeftTabs.Count > 0) SelectedLeftTab = LeftTabs[^1];
                else SelectedLeftTab = null;
            }
            else if (RightTabs.Remove(vm))
            {
                if (RightTabs.Count > 0) SelectedRightTab = RightTabs[^1];
                else SelectedRightTab = null;
            }
            else if (BottomTabs.Remove(vm))
            {
                if (BottomTabs.Count > 0) SelectedBottomTab = BottomTabs[^1];
                else SelectedBottomTab = null;
            }
            else if (CenterTabs.Remove(vm))
            {
                if (CenterTabs.Count > 0) SelectedCenterTab = CenterTabs[^1];
                else SelectedCenterTab = null;
            }
        }

        [RelayCommand]
        private async Task ApplyDefaultLayout()
        {
            EditorLayoutData layout = new EditorLayoutData
            {
                LeftPanelWidth = 200,
                RightPanelWidth = 300,
                BottomPanelHeight = 250,
                LeftTabs = "WorldWindow",
                RightTabs = "PropertiesWindow,SettingsWindow",
                CenterTabs = "EnvironmentWindow",
                BottomTabs = "AssetsWindow,ConsoleWindow,DeveloperWindow",
                SelectedLeftTab = "WorldWindow",
                SelectedRightTab = "PropertiesWindow",
                SelectedCenterTab = "EnvironmentWindow",
                SelectedBottomTab = "AssetsWindow"
            };
            await ApplyLayout(layout);
        }

        [RelayCommand]
        private async Task ApplyFocusEditorLayout()
        {
            // Focus on code/script editing
            EditorLayoutData layout = new EditorLayoutData
            {
                LeftPanelWidth = 250,
                RightPanelWidth = 350,
                BottomPanelHeight = 200,
                LeftTabs = "WorldWindow,AssetsWindow",
                RightTabs = "PropertiesWindow",
                CenterTabs = "EnvironmentWindow",
                BottomTabs = "ConsoleWindow,DeveloperWindow",
                SelectedLeftTab = "AssetsWindow",
                SelectedRightTab = "PropertiesWindow",
                SelectedCenterTab = "EnvironmentWindow",
                SelectedBottomTab = "ConsoleWindow"
            };
            await ApplyLayout(layout);
        }

        [RelayCommand]
        private async Task ApplyFocusAssetsLayout()
        {
            // Focus on asset browsing and management
            EditorLayoutData layout = new EditorLayoutData
            {
                LeftPanelWidth = 150,
                RightPanelWidth = 400,
                BottomPanelHeight = 200,
                LeftTabs = "WorldWindow",
                RightTabs = "PropertiesWindow",
                CenterTabs = "AssetsWindow",
                BottomTabs = "ConsoleWindow,DeveloperWindow",
                SelectedLeftTab = "WorldWindow",
                SelectedRightTab = "PropertiesWindow",
                SelectedCenterTab = "AssetsWindow",
                SelectedBottomTab = "ConsoleWindow"
            };
            await ApplyLayout(layout);
        }

        [RelayCommand]
        private async Task ApplyWideViewportLayout()
        {
            // Maximize the viewport for scene editing
            EditorLayoutData layout = new EditorLayoutData
            {
                LeftPanelWidth = 180,
                RightPanelWidth = 250,
                BottomPanelHeight = 180,
                LeftTabs = "WorldWindow",
                RightTabs = "PropertiesWindow,SettingsWindow",
                CenterTabs = "EnvironmentWindow",
                BottomTabs = "AssetsWindow,ConsoleWindow",
                SelectedLeftTab = "WorldWindow",
                SelectedRightTab = "PropertiesWindow",
                SelectedCenterTab = "EnvironmentWindow",
                SelectedBottomTab = "AssetsWindow"
            };
            await ApplyLayout(layout);
        }

        [RelayCommand]
        private async Task ApplyMinimalLayout()
        {
            // Minimal UI for pure viewing
            EditorLayoutData layout = new EditorLayoutData
            {
                LeftPanelWidth = 0,
                RightPanelWidth = 0,
                BottomPanelHeight = 0,
                LeftTabs = "",
                RightTabs = "",
                CenterTabs = "EnvironmentWindow",
                BottomTabs = "",
                SelectedLeftTab = "",
                SelectedRightTab = "",
                SelectedCenterTab = "EnvironmentWindow",
                SelectedBottomTab = ""
            };
            await ApplyLayout(layout);
        }

        [RelayCommand]
        private async Task ApplyDebugLayout()
        {
            // Layout optimized for debugging (console and developer tools visible)
            EditorLayoutData layout = new EditorLayoutData
            {
                LeftPanelWidth = 200,
                RightPanelWidth = 350,
                BottomPanelHeight = 300,
                LeftTabs = "WorldWindow",
                RightTabs = "PropertiesWindow",
                CenterTabs = "EnvironmentWindow",
                BottomTabs = "ConsoleWindow,DeveloperWindow,AssetsWindow",
                SelectedLeftTab = "WorldWindow",
                SelectedRightTab = "PropertiesWindow",
                SelectedCenterTab = "EnvironmentWindow",
                SelectedBottomTab = "ConsoleWindow"
            };
            await ApplyLayout(layout);
        }

        /// <summary>
        /// Update the project's layout data.
        /// </summary>
        /// <param name="layout">New layout to switch to</param>
        /// <returns>Async layout change operation task</returns>
        private async Task ApplyLayout(EditorLayoutData layout)
        {
            if (ProjectManager.CurrentProjectData != null)
            {
                ProjectManager.CurrentProjectData.EditorLayout = layout;
                if (mainWindow is MainWindow window) await window.LoadEditorLayoutFromProjectAsync();
            }
        }
    }
}
