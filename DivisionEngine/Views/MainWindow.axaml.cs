using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DivisionEngine.Editor.ViewModels;

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
                    TabItem? tabItem = FindParentTabItem(source);

                    if (tabItem != null && tabItem.DataContext is EditorWindowViewModel viewModel)
                    {
                        var contextMenu = CreateTabContextMenu(panelType, viewModel);
                        contextMenu.Open(tabItem);
                        e.Handled = true;
                    }
                }
            }, RoutingStrategies.Tunnel);
        }

        private static TabItem? FindParentTabItem(Control? control)
        {
            Control? current = control;
            while (current != null)
            {
                if (current is TabItem tabItem) return tabItem;
                current = current.Parent as Control;
            }
            Debug.Warning("Could not find parent tab control to build context menu");
            return null;
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
    }
}