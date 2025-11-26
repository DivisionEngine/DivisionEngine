using Avalonia;
using Avalonia.Controls;
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
        }
    }
}