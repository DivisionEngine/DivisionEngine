using ReactiveUI;

namespace DivisionEngine.Editor.ViewModels
{
    /// <summary>
    /// Base class of all editor windows.
    /// </summary>
    public partial class EditorWindowViewModel : ViewModelBase
    {
        private string title = "Untitled";
        public string Title
        {
            get => title;
            set => this.RaiseAndSetIfChanged(ref title, value);
        }
    }
}
