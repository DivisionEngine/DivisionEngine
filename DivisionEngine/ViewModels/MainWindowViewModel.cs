using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using DivisionEngine.Editor.Systems;
using DivisionEngine.Projects;
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

        private readonly Window mainWindow;

        // Main Window Menu Commands

        public Action? RequestClose { get; set; }

        // Editor window tab collections

        public ObservableCollection<EditorWindowViewModel> CenterTabs { get; } = [];
        public ObservableCollection<EditorWindowViewModel> BottomTabs { get; } = [];
        public ObservableCollection<EditorWindowViewModel> LeftTabs { get; } = [];
        public ObservableCollection<EditorWindowViewModel> RightTabs { get; } = [];

        // Tab selection variables

        private EditorWindowViewModel? _selectedLeftTab;
        private EditorWindowViewModel? _selectedRightTab;
        private EditorWindowViewModel? _selectedCenterTab;
        private EditorWindowViewModel? _selectedBottomTab;

        public EditorWindowViewModel? SelectedLeftTab
        {
            get => _selectedLeftTab;
            set => this.RaiseAndSetIfChanged(ref _selectedLeftTab, value);
        }
        public EditorWindowViewModel? SelectedRightTab
        {
            get => _selectedRightTab;
            set => this.RaiseAndSetIfChanged(ref _selectedRightTab, value);
        }
        public EditorWindowViewModel? SelectedCenterTab
        {
            get => _selectedCenterTab;
            set => this.RaiseAndSetIfChanged(ref _selectedCenterTab, value);
        }
        public EditorWindowViewModel? SelectedBottomTab
        {
            get => _selectedBottomTab;
            set => this.RaiseAndSetIfChanged(ref _selectedBottomTab, value);
        }

        // Main Window API

        /// <summary>
        /// Editor progress bar value between 0.0 and 1.0.
        /// </summary>
        public double ProgressValue { get; set; } = 0.5;
        /// <summary>
        /// Enables or disables the progress bar.
        /// </summary>
        public bool ShowProgress { get; set; } = true;

        /// <summary>
        /// Builds the main window view model and initializes default tabs.
        /// </summary>
        public MainWindowViewModel(Window mainWindow)
        {
            this.mainWindow = mainWindow;

            // Initialize default tabs
            LeftTabs.Add(new WorldWindowViewModel());
            CenterTabs.Add(new EnvironmentWindowViewModel());
            RightTabs.Add(new PropertiesWindowViewModel());
            BottomTabs.Add(new AssetsWindowViewModel());
            BottomTabs.Add(new ConsoleWindowViewModel());
        }

        [RelayCommand]
        private async Task OpenProject()
        {
            try
            {
                await App.SetEditorRenderingAsync(false);

                // Open folder dialog for selecting project directory
                var result = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Open Project Folder",
                    AllowMultiple = false,
                    SuggestedStartLocation = await mainWindow.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
                });

                if (result.Count > 0 && !string.IsNullOrEmpty(result[0].Path.LocalPath))
                {
                    string projectPath = result[0].Path.LocalPath;
                    if (ProjectManager.IsDivisionProject(projectPath)) // Check if this is a valid project directory
                    {
                        bool success = ProjectManager.LoadProject(projectPath);
                        if (success) // Check if loaded project
                        {
                            AssetsWindow.LoadAssetsForCurrentProject();
                        }
                        else Debug.Error($"Failed to load project: {projectPath}");
                    }
                    else Debug.Info("Selected folder is not a valid Division Engine project");
                }

                await App.SetEditorRenderingAsync(true);
            }
            catch (Exception ex)
            {
                Debug.Error($"Error opening project: {ex.Message}");
            }
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
                Debug.Error($"Error saving project: {ex.Message}");
            }
            ProjectManager.SaveCurrentProject();
        }

        [RelayCommand]
        private async Task SaveProjectAs()
        {
            try
            {
                await App.SetEditorRenderingAsync(false);

                // Get project name
                ProjectNameDialog projectNameDialog = new ProjectNameDialog();
                string? projectName = await projectNameDialog.ShowDialog<string>(mainWindow);

                if (string.IsNullOrWhiteSpace(projectName))
                {
                    await App.SetEditorRenderingAsync(true);
                    return;
                }

                // Choose folder location
                var folderResult = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Project Location",
                    AllowMultiple = false,
                    SuggestedStartLocation = await mainWindow.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
                });

                if (folderResult.Count == 0 || string.IsNullOrEmpty(folderResult[0].Path.LocalPath))
                {
                    await App.SetEditorRenderingAsync(true);
                    return;
                }

                string selectedFolder = folderResult[0].Path.LocalPath;
                string projectPath = Path.Combine(selectedFolder, projectName);

                // Check if folder already exists
                if (Directory.Exists(projectPath) && Directory.GetFiles(projectPath, "*.divproj").Length > 0)
                {
                    ConfirmationDialog confirmDialog = new ConfirmationDialog
                    {
                        Title = "Project Exists",
                        Message = $"A project named '{projectName}' already exists at this location. Overwrite?"
                    };

                    bool overwrite = await confirmDialog.ShowDialog<bool>(mainWindow);
                    if (!overwrite)
                    {
                        await App.SetEditorRenderingAsync(true);
                        return;
                    }
                }
                
                // Save project
                bool success = ProjectManager.SaveNewProject(projectName, projectPath);
                if (success) // Check if successfully saved project
                    AssetsWindow.LoadAssetsForCurrentProject();
                else Debug.Error("Failed to save project");

                await App.SetEditorRenderingAsync(true);
            }
            catch (Exception ex)
            {
                Debug.Error($"Error saving project: {ex.Message}");
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
            Debug.Info("Redo Triggered");
            // Implement Redo functionality here
        }

        [RelayCommand]
        private void About()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/DivisionEngine/DivisionEngine",
                UseShellExecute = true,
            });
        }

        /// <summary>
        /// Creates an entity straight from the "Add" menu.
        /// </summary>
        /// <param name="entityType">Entity type to add (as camel case string key)</param>
        [RelayCommand]
        private void CreateEntity(string entityType)
        {
            try
            {
                uint entityId = entityType switch
                {
                    "empty" => DefaultEntities.Empty(),
                    "emptyTransform" => DefaultEntities.EmptyTransform(),
                    "camera" => DefaultEntities.Camera(),
                    "environment" => DefaultEntities.Environment(),
                    "sphere" => DefaultEntities.SDFSphere(),
                    "box" => DefaultEntities.SDFBox(),
                    "roundedBox" => DefaultEntities.SDFRoundedBox(),
                    "torus" => DefaultEntities.SDFTorus(),
                    "pyramid" => DefaultEntities.SDFPyramid(),
                    "plane" => DefaultEntities.SDFPlane(),
                    "cylinder" => DefaultEntities.SDFCylinder(),
                    "capsule" => DefaultEntities.SDFCapsule(),
                    "cone" => DefaultEntities.SDFCone(),
                    _ => DefaultEntities.EmptyTransform()
                };
                Debug.Info($"Created {entityType} entity with ID: {entityId}");
            }
            catch (Exception e)
            {
                Debug.Error($"Failed to create entity: {e.Message}");
            }
        }

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
                _ => null
            };

            if (args[0] == "Environment" && !App.RendererVisible) // Re-enable render window if disabled.
                RenderWindowManagementSystem.SetVisible(true);

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
        /// Called when a tab must be closed on one of the main panel areas.
        /// </summary>
        /// <param name="vm">Tab type to close</param>
        [RelayCommand]
        private void CloseTab(EditorWindowViewModel? vm)
        {
            if (vm is null) return;
            else if (LeftTabs.Contains(vm))
            {
                LeftTabs.Remove(vm);
                if (LeftTabs.Count > 0)
                    SelectedLeftTab = LeftTabs[LeftTabs.Count - 1];
                else SelectedLeftTab = null;
            }
            else if (RightTabs.Contains(vm))
            {
                RightTabs.Remove(vm);
                if (RightTabs.Count > 0)
                    SelectedRightTab = RightTabs[RightTabs.Count - 1];
                else SelectedRightTab = null;
            }
            else if (BottomTabs.Contains(vm))
            {
                BottomTabs.Remove(vm);
                if (BottomTabs.Count > 0)
                    SelectedBottomTab = BottomTabs[BottomTabs.Count - 1];
                else SelectedBottomTab = null;
            }
            else if (CenterTabs.Contains(vm))
            {
                CenterTabs.Remove(vm);
                if (CenterTabs.Count > 0)
                    SelectedCenterTab = CenterTabs[CenterTabs.Count - 1];
                else SelectedCenterTab = null;
            }
        }
    }
}
