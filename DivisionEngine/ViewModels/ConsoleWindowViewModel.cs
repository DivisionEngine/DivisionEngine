using System.Collections.ObjectModel;

namespace DivisionEngine.Editor.ViewModels
{
    public partial class ConsoleWindowViewModel : EditorWindowViewModel
    {
        public ObservableCollection<LogEntry> Logs { get; } = [];

        public ConsoleWindowViewModel()
        {
            Title = "Console";
            Debug.OnLogUpdate += Logs.Add;
        }
    }
}
