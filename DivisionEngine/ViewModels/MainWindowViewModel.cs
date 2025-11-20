using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DivisionEngine.Editor.ViewModels
{
    /// <summary>
    /// The view model for the Division Engine editor's parent window.
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
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

        /// <summary>
        /// Builds the main window view model and initializes default tabs.
        /// </summary>
        public MainWindowViewModel()
        {
            LeftTabs.Add(new WorldWindowViewModel());

            CenterTabs.Add(new EnvironmentWindowViewModel());

            RightTabs.Add(new PropertiesWindowViewModel());

            BottomTabs.Add(new AssetsWindowViewModel());
            BottomTabs.Add(new ConsoleWindowViewModel());
        }

        [RelayCommand]
        private void OpenProject()
        {
            Debug.Info("Opening Project");
            // Implement Open Project functionality here
        }

        [RelayCommand]
        private void SaveProject()
        {
            Debug.Info("Saving Project");
            // Implement Save Project functionality here
        }

        [RelayCommand]
        private void SaveProjectAs()
        {
            Debug.Info("Saving Project As");
            // Implement Save Project As functionality here
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
            Debug.Info("About Help Triggered");
            // Implement About Help functionality here
        }

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

        [RelayCommand]
        private void CloseTab(EditorWindowViewModel? vm)
        {
            if (vm is null) return;
            else if (LeftTabs.Contains(vm))
            {
                LeftTabs.Remove(vm);
                SelectedLeftTab = LeftTabs[LeftTabs.Count - 1];
            }
            else if (RightTabs.Contains(vm))
            {
                RightTabs.Remove(vm);
                SelectedRightTab = RightTabs[RightTabs.Count - 1];
            }
            else if (BottomTabs.Contains(vm))
            {
                BottomTabs.Remove(vm);
                SelectedBottomTab = BottomTabs[BottomTabs.Count - 1];
            }
            else if (CenterTabs.Contains(vm))
            {
                CenterTabs.Remove(vm);
                SelectedCenterTab = CenterTabs[CenterTabs.Count - 1];
            }
        }
    }
}
