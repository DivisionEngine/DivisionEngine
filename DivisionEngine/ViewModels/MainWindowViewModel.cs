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

        public static MainWindowViewModel? vm;
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

        private double? _progressVal;
        private bool? _showProgress;

        /// <summary>
        /// Editor progress bar value between 0.0 and 1.0.
        /// </summary>
        public double? ProgressValue
        {
            get => _progressVal;
            set => this.RaiseAndSetIfChanged(ref _progressVal, value);
        }
        /// <summary>
        /// Enables or disables the progress bar.
        /// </summary>
        public bool? ShowProgress
        {
            get => _showProgress;
            set => this.RaiseAndSetIfChanged(ref _showProgress, value);
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
            EditorUI.Progress = 0;
            ShowProgress = true;
            try
            {
                await App.SetEditorRenderingAsync(false);
                EditorUI.Progress = 0.25;

                // Get project name
                ProjectNameDialog projectNameDialog = new ProjectNameDialog();
                string? projectName = await projectNameDialog.ShowDialog<string>(mainWindow);
                EditorUI.Progress = 0.5;

                if (string.IsNullOrWhiteSpace(projectName))
                {
                    await App.SetEditorRenderingAsync(true);
                    EditorUI.ShowProgress = false;
                    return;
                }

                // Choose folder location
                var folderResult = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Project Location",
                    AllowMultiple = false,
                    SuggestedStartLocation = await mainWindow.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
                });
                EditorUI.Progress = 0.75;

                if (folderResult.Count == 0 || string.IsNullOrEmpty(folderResult[0].Path.LocalPath))
                {
                    await App.SetEditorRenderingAsync(true);
                    EditorUI.ShowProgress = false;
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
                    EditorUI.Progress = 1.0;
                    if (!overwrite)
                    {
                        await App.SetEditorRenderingAsync(true);
                        EditorUI.ShowProgress = false;
                        return;
                    }
                }
                
                // Save project
                bool success = ProjectManager.SaveNewProject(projectName, projectPath);
                if (success) // Check if successfully saved project
                    AssetsWindow.LoadAssetsForCurrentProject();
                else Debug.Error("Failed to save project");
                EditorUI.ShowProgress = false;

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
