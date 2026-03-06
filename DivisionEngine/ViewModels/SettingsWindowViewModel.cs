using Material.Icons;

namespace DivisionEngine.Editor.ViewModels
{
    public partial class SettingsWindowViewModel : EditorWindowViewModel
    {
        public SettingsWindowViewModel()
        {
            Title = "Settings";
            Icon = MaterialIconKind.FileSettings;
        }
    }
}
