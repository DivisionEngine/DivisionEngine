using Avalonia.Controls;

namespace DivisionEngine.Editor
{
    /// <summary>
    /// API for common Division-Avalonia UI utilities.
    /// </summary>
    internal static class EditorUI
    {
        /// <summary>
        /// Checks and sees if there is a parent TabItem to this control.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static TabItem? FindParentTabItem(Control? control)
        {
            Control? current = control;
            while (current != null)
            {
                if (current is TabItem tabItem) return tabItem;
                current = current.Parent as Control;
            }
            Debug.Warning("Could not find parent tab control");
            return null;
        }
    }
}
