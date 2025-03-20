using Avalonia.Controls;

namespace CertBox
{
    public static class ControlExtensions
    {
        public static bool IsDescendantOf(this Control control, Control ancestor)
        {
            var current = control;
            while (current != null)
            {
                if (current == ancestor)
                    return true;

                current = current.Parent as Control;
            }

            return false;
        }
    }
}