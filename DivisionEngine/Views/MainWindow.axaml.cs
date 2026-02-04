using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using DivisionEngine.Editor.Tasks;
using DivisionEngine.Editor.ViewModels;
using Material.Icons.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DivisionEngine.Editor
{
    /// <summary>
    /// Represents the main UI window of the Division Engine editor.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent(); // Initialize the main window components

            if (DataContext is MainWindowViewModel vm)
            {
                vm.RequestClose = Close;
            }
#if DEBUG
            this.AttachDevTools(); // Enable developer tools in debug mode
#endif
            AttachContextMenus();
            SetupUniversalProgressBar(); // Build editor task system UI
        }

        /// <summary>
        /// Attaches a context menu to each tab control.
        /// </summary>
        private void AttachContextMenus()
        {
            // Wait for the controls to be loaded
            Loaded += (s, e) =>
            {
                TabControl leftTabsControl = this.Find<TabControl>("leftTabs")!;
                TabControl centerTabsControl = this.Find<TabControl>("centerTabs")!;
                TabControl bottomTabsControl = this.Find<TabControl>("bottomTabs")!;
                TabControl rightTabsControl = this.Find<TabControl>("rightTabs")!;
                AttachContextMenuToTabControl(leftTabsControl, "left");
                AttachContextMenuToTabControl(centerTabsControl, "center");
                AttachContextMenuToTabControl(bottomTabsControl, "bottom");
                AttachContextMenuToTabControl(rightTabsControl, "right");
            };
        }

        private void AttachContextMenuToTabControl(TabControl tabControl, string panelType)
        {
            tabControl.AddHandler(PointerReleasedEvent, (sender, e) =>
            {
                if (e.InitialPressMouseButton == MouseButton.Right)
                {
                    Control? source = e.Source as Control;
                    TabItem? tabItem = EditorUI.FindParentTabItem(source);

                    if (tabItem != null && tabItem.DataContext is EditorWindowViewModel viewModel)
                    {
                        ContextMenu contextMenu = CreateTabContextMenu(panelType, viewModel);
                        contextMenu.Open(tabItem);
                        e.Handled = true;
                    }
                }
            }, RoutingStrategies.Tunnel);
        }

        private ContextMenu CreateTabContextMenu(string panelType, EditorWindowViewModel viewModel)
        {
            ContextMenu contextMenu = new ContextMenu();
            if (DataContext is MainWindowViewModel mainViewModel)
            {
                MenuItem closeMenuItem = new MenuItem
                {
                    Header = "Close",
                    Command = mainViewModel.CloseTabCommand,
                    CommandParameter = viewModel
                };
                Separator separator = new Separator();
                MenuItem duplicateMenuItem = new MenuItem
                {
                    Header = "Duplicate Tab",
                    //Command = mainViewModel.DuplicateTabCommand,
                    CommandParameter = viewModel
                };

                contextMenu.Items.Add(closeMenuItem);
                contextMenu.Items.Add(separator);
                contextMenu.Items.Add(duplicateMenuItem);
            }
            return contextMenu;
        }

        private void SetupUniversalProgressBar()
        {
            EditorTaskManager.TasksChanged += UpdateUniversalProgressBar;

            // Make progress bar clickable
            UniversalProgressBar.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(UniversalProgressBar).Properties.IsLeftButtonPressed)
                    TaskContextMenu.Open(UniversalProgressBar);
            };
            UpdateUniversalProgressBar();
        }

        private void UpdateUniversalProgressBar()
        {
            List<EditorTask> tasks = [.. EditorTaskManager.GetAll()];
            TextBlock? progressText = this.FindControl<TextBlock>("ProgressPercentageText");
            TextBlock? taskCountText = this.FindControl<TextBlock>("TaskCountText");

            if (tasks.Count > 0)
            {
                float avgProgress = tasks.Average(t => t.Progress);
                UniversalProgressBar.Value = avgProgress * 100;
                if (progressText != null)
                {
                    progressText.Text = $"{avgProgress:P0}";
                    progressText.Foreground = avgProgress >= 1 ? Brushes.LightGreen : Brushes.White;
                }
                if (taskCountText != null)
                {
                    taskCountText.Text = $"{tasks.Count} task{(tasks.Count != 1 ? "s" : "")}";
                    taskCountText.Foreground = avgProgress >= 1 ? Brushes.LightGreen : Brushes.Orange;
                }

                UniversalProgressBar.Foreground = avgProgress >= 1 // Update progress bar color
                    ? EditorColor.FromRGB(76, 175, 80)  // Green
                    : EditorColor.FromRGB(255, 106, 0); // Orange
                UniversalProgressBar.IsVisible = true;
            }
            else
            {
                UniversalProgressBar.IsVisible = false;
                UniversalProgressBar.Value = 0;
                if (progressText != null) progressText.Text = "0%";
                if (taskCountText != null) taskCountText.Text = "";
            }
            UpdateTaskContextMenu(tasks);
        }

        private void UpdateTaskContextMenu(List<EditorTask> tasks)
        {
            TaskContextMenu.Items.Clear();
            if (tasks.Count == 0)
            {
                TaskContextMenu.Items.Add(new MenuItem
                {
                    Header = "No background tasks",
                    IsEnabled = false,
                    Foreground = Brushes.Gray,
                });
                return;
            }

            foreach (EditorTask task in tasks) // Add each task to the menu
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = CreateTaskMenuItem(task),
                    Foreground = Brushes.White,
                    MinWidth = 280,
                };
                TaskContextMenu.Items.Add(menuItem);
            }

            // Add clear completed option
            // ALWAYS GET FRESH DATA
            List<EditorTask> completedTasks = [.. tasks.Where(t => t.Progress >= 1)];
            if (completedTasks.Count > 0)
            {
                TaskContextMenu.Items.Add(new Separator());
                MenuItem clearItem = new MenuItem
                {
                    Header = $"Clear completed ({completedTasks.Count})",
                };

                clearItem.Click += (s, e) =>
                {
                    // Re-fetch tasks to ensure we have current state
                    List<EditorTask> currentTasks = [.. EditorTaskManager.GetAll()];
                    IEnumerable<EditorTask> toRemove = [.. currentTasks.Where(t => t.Progress >= 1)];

                    foreach (EditorTask task in toRemove)
                        EditorTaskManager.Remove(task.Id);
                    TaskContextMenu.Close(); // Close context menu after clearing
                };
                TaskContextMenu.Items.Add(clearItem);
            }
        }

        private static Grid CreateTaskMenuItem(EditorTask task)
        {
            Grid grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto), // Icon
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)), // Content
                    new ColumnDefinition(GridLength.Auto), // Progress & Close button
                },
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto), // Name & X button
                    new RowDefinition(GridLength.Auto), // Description
                    new RowDefinition(GridLength.Auto), // Progress bar
                },
                MinWidth = 280, // Ensure minimum width
                Margin = new Thickness(0, 2, 0, 2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            MaterialIcon icon = new MaterialIcon
            {
                Kind = task.Icon,
                Width = 14,
                Height = 14,
                Foreground = task.Progress >= 1 ? Brushes.Green : Brushes.Orange,
                Margin = new Thickness(0, 4, 8, 0),
                VerticalAlignment = VerticalAlignment.Top,
            };
            Grid.SetColumn(icon, 0);
            Grid.SetRow(icon, 0);
            Grid.SetRowSpan(icon, 2); // Span across name and description

            TextBlock nameText = new TextBlock
            {
                Text = task.Name,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            Grid.SetColumn(nameText, 1);
            Grid.SetRow(nameText, 0);

            Button closeButton = new Button
            {
                Content = "×",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.Gray,
                Padding = new Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = task.Id,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
            };
            closeButton.PointerEntered += (s, e) => closeButton.Foreground = Brushes.White;
            closeButton.PointerExited += (s, e) => closeButton.Foreground = Brushes.Gray;
            closeButton.Click += (s, e) =>
            {
                if (closeButton.Tag is Guid taskId) EditorTaskManager.Remove(taskId);
                e.Handled = true;
            };
            Grid.SetColumn(closeButton, 2);
            Grid.SetRow(closeButton, 0);

            TextBlock descText = new TextBlock
            {
                Text = task.Description,
                FontSize = 10,
                Foreground = EditorColor.FromRGB(180, 180, 180),
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 28, 4), // Right margin for close button area
            };
            Grid.SetColumn(descText, 1);
            Grid.SetColumnSpan(descText, 2); // Span under both progress % and X button
            Grid.SetRow(descText, 1);

            TextBlock progressText = new TextBlock
            {
                Text = $"{task.Progress:P0}",
                FontSize = 10,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 24, 0), // Leave space for X button
            };
            Grid.SetColumn(progressText, 2);
            Grid.SetRow(progressText, 0);

            ProgressBar progressBar = new ProgressBar
            {
                Value = task.Progress * 100,
                Maximum = 100,
                Height = 4,
                Background = EditorColor.FromRGB(40, 40, 40),
                Foreground = task.Progress >= 1 ? Brushes.Green : Brushes.Orange,
                Margin = new Thickness(0, 4, 0, 0),
            };
            Grid.SetColumn(progressBar, 0);
            Grid.SetColumnSpan(progressBar, 3);
            Grid.SetRow(progressBar, 2);

            grid.Children.Add(icon);
            grid.Children.Add(nameText);
            grid.Children.Add(descText);
            grid.Children.Add(progressText);
            grid.Children.Add(progressBar);
            grid.Children.Add(closeButton);
            return grid;
        }

        protected override void OnClosed(EventArgs e)
        {
            EditorTaskManager.TasksChanged -= UpdateUniversalProgressBar;
            base.OnClosed(e);
        }
    }
}