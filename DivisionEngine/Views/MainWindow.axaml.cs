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
        private static Flyout? tasksFlyout;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent(); // Initialize the main window components
            if (DataContext is MainWindowViewModel vm) vm.RequestClose = Close;
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

        /// <summary>
        /// Attaches a context menu to each tab control element.
        /// </summary>
        /// <param name="tabControl">Tab control to attach menu to</param>
        /// <param name="panelType">Panel type of tab control</param>
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

        /// <summary>
        /// Creates the context menu for each tab.
        /// </summary>
        /// <param name="panelType">Tab panel type</param>
        /// <param name="viewModel">Tab editor view model</param>
        /// <returns>Generated tab context menu</returns>
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

        /// <summary>
        /// Initializes the universal progress bar at the bottom of the editor.
        /// </summary>
        private void SetupUniversalProgressBar()
        {
            EditorTaskManager.TasksChanged += UpdateUniversalProgressBar;

            // Find the progress bar container
            Border? progressBarContainer = this.FindControl<Border>("ProgressBarContainer");
            if (progressBarContainer != null)
            {
                // Create editor tasks flyout
                tasksFlyout = new Flyout
                {
                    Placement = PlacementMode.Top, // Shows above progress bar
                    ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway,
                    FlyoutPresenterClasses = { "tasks-foldout" }
                };
                UpdateTaskFoldoutContent(tasksFlyout); // Set flyout content

                // Attach click handler to show the flyout
                progressBarContainer.PointerPressed += (s, e) =>
                {
                    if (e.GetCurrentPoint(progressBarContainer).Properties.IsLeftButtonPressed)
                    {
                        UpdateTaskFoldoutContent(tasksFlyout); // Refresh before showing
                        tasksFlyout.ShowAt(progressBarContainer);
                    }
                };
            }
            UpdateUniversalProgressBar();
        }

        /// <summary>
        /// Updates the universal progress bar at the bottom of the editor.
        /// </summary>
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
                    progressText.Foreground = avgProgress >= 1 ? Brushes.White : Brushes.Aquamarine;
                }
                if (taskCountText != null)
                {
                    taskCountText.Text = $"{tasks.Count} task{(tasks.Count != 1 ? "s" : "")}";
                    taskCountText.Foreground = avgProgress >= 1 ? Brushes.White : Brushes.Aquamarine;
                }
                UniversalProgressBar.Foreground = avgProgress >= 1 ? Brushes.SeaGreen : Brushes.Teal;
                UniversalProgressBar.IsVisible = true;
            }
            else
            {
                UniversalProgressBar.IsVisible = false;
                UniversalProgressBar.Value = 0;
                if (progressText != null) progressText.Text = "0%";
                if (taskCountText != null) taskCountText.Text = "";
            }

            if (tasksFlyout != null) UpdateTaskFoldoutContent(tasksFlyout);
        }

        /// <summary>
        /// Updates the editor task manager context menu.
        /// </summary>
        /// <param name="flyout">List of tasks in the context menu</param>
        private static void UpdateTaskFoldoutContent(Flyout flyout)
        {
            List<EditorTask> tasks = [.. EditorTaskManager.GetAll()];
            if (tasks.Count == 0)
            {
                flyout.Content = new TextBlock
                {
                    Text = "No background tasks",
                    Foreground = Brushes.Gray,
                    Padding = new Thickness(20, 10),
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                return;
            }

            // Create main container that stretches full width
            StackPanel container = new StackPanel
            {
                MinWidth = 300,
                MaxWidth = 500,
                Spacing = 4,
            };

            // Add each task as a full-width item
            foreach (EditorTask task in tasks)
            {
                Border taskBorder = new Border
                {
                    Child = CreateTaskFoldoutItem(task), // Reuse your existing CreateTaskMenuItem but modify for full width
                    BorderBrush = EditorColor.FromRGB(10, 10, 10),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    CornerRadius = new CornerRadius(4),
                    Background = EditorColor.FromRGB(24, 24, 24),
                    Padding = new Thickness(8, 6),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
                container.Children.Add(taskBorder);
            }

            // Add clear completed section
            List<EditorTask> completedTasks = [.. tasks.Where(t => t.Progress >= 1)];
            if (completedTasks.Count > 0)
            {
                Button clearButton = new Button
                {
                    Content = $"Clear completed ({completedTasks.Count})",
                    Background = EditorColor.FromRGB(51, 51, 51),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10, 8),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Cursor = new Cursor(StandardCursorType.Hand),
                };
                clearButton.Click += (s, e) =>
                {
                    List<EditorTask> currentTasks = [.. EditorTaskManager.GetAll()];
                    IEnumerable<EditorTask> toRemove = [.. currentTasks.Where(t => t.Progress >= 1)];
                    foreach (EditorTask task in toRemove) EditorTaskManager.Remove(task.Id);
                    flyout.Hide();
                };
                container.Children.Add(clearButton);
            }
            flyout.Content = container;
        }

        /// <summary>
        /// Creates a full-width task item for the foldout.
        /// </summary>
        private static Grid CreateTaskFoldoutItem(EditorTask task)
        {
            Grid grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    new ColumnDefinition(GridLength.Auto),
                },
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                },
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            // Icon
            MaterialIcon icon = new MaterialIcon
            {
                Kind = task.Icon,
                Width = 16,
                Height = 16,
                Foreground = task.Progress >= 1 ? Brushes.SeaGreen : Brushes.Teal,
                Margin = new Thickness(0, 2, 8, 0),
                VerticalAlignment = VerticalAlignment.Top,
            };
            Grid.SetColumn(icon, 0);
            Grid.SetRow(icon, 0);
            Grid.SetRowSpan(icon, 2);

            // Name and close button container
            Grid nameContainer = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)), // Name
                    new ColumnDefinition(GridLength.Auto), // Close button
                },
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            TextBlock nameText = new TextBlock
            {
                Text = task.Name,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            Grid.SetColumn(nameText, 0);
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
                Margin = new Thickness(4, 0, 0, 0),
            };
            closeButton.PointerEntered += (s, e) => closeButton.Foreground = Brushes.White;
            closeButton.PointerExited += (s, e) => closeButton.Foreground = Brushes.Gray;
            closeButton.Click += (s, e) =>
            {
                if (closeButton.Tag is Guid taskId)
                    EditorTaskManager.Remove(taskId);
                e.Handled = true;
            };
            Grid.SetColumn(closeButton, 1);

            nameContainer.Children.Add(nameText);
            nameContainer.Children.Add(closeButton);
            Grid.SetColumn(nameContainer, 1);
            Grid.SetRow(nameContainer, 0);

            // Description
            TextBlock descText = new TextBlock
            {
                Text = task.Description,
                FontSize = 10,
                Foreground = EditorColor.FromRGB(180, 180, 180),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 4),
            };
            Grid.SetColumn(descText, 1);
            Grid.SetColumnSpan(descText, 2);
            Grid.SetRow(descText, 1);

            // Progress text
            TextBlock progressText = new TextBlock
            {
                Text = $"{task.Progress:P0}",
                FontSize = 10,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 4, 0),
            };
            Grid.SetColumn(progressText, 2);
            Grid.SetRow(progressText, 0);

            // Progress bar
            ProgressBar progressBar = new ProgressBar
            {
                Value = task.Progress * 100,
                Maximum = 100,
                Height = 4,
                Background = EditorColor.FromRGB(40, 40, 40),
                Foreground = task.Progress >= 1 ? Brushes.SeaGreen : Brushes.Teal,
                Margin = new Thickness(0, 4, 0, 0),
            };
            Grid.SetColumn(progressBar, 0);
            Grid.SetColumnSpan(progressBar, 3);
            Grid.SetRow(progressBar, 2);

            grid.Children.Add(icon);
            grid.Children.Add(nameContainer);
            grid.Children.Add(descText);
            grid.Children.Add(progressText);
            grid.Children.Add(progressBar);
            return grid;
        }

        protected override void OnClosed(EventArgs e)
        {
            EditorTaskManager.TasksChanged -= UpdateUniversalProgressBar;
            base.OnClosed(e);
        }
    }
}