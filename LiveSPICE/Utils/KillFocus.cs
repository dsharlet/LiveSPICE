using System.Windows;
using System.Windows.Input;

namespace LiveSPICE
{
    public static class KillFocusExtension
    {
        public static void KillFocus(this FrameworkElement E)
        {
            // http://stackoverflow.com/questions/2914495/wpf-how-to-programmatically-remove-focus-from-a-textbox

            // Move to a parent that can take focus
            FrameworkElement parent = (FrameworkElement)E.Parent;
            while (parent != null && parent is IInputElement && !((IInputElement)parent).Focusable)
            {
                parent = (FrameworkElement)parent.Parent;
            }

            DependencyObject scope = FocusManager.GetFocusScope(E);
            FocusManager.SetFocusedElement(scope, parent as IInputElement);
        }
    }
}
