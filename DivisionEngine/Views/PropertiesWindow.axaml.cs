using System.Collections.Generic;

namespace DivisionEngine.Editor;

public partial class PropertiesWindow : EditorWindow
{
    public PropertiesWindow()
    {
        InitializeComponent();
    }

    public static bool SetupPropertiesForEntity(uint entityId)
    {
        if (WorldManager.CurrentWorld == null || !W.EntityExists(entityId)) return false;
        List<IComponent> entityComps = W.GetAllComponents(entityId);

        foreach (IComponent comp in entityComps)
        {
            
        }

        return true;
    }

    public static void SetupPropertiesForWorld(World world)
    {

    }
}