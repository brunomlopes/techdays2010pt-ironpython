using System.Collections;
using System.Windows;

namespace Editor
{
    public static class WindowExtensionMethods
    {
        public static IEnumerable Toggler(this Window window)
        {
            while (true)
            {
                window.Show();
                yield return new object();
                window.Hide();
                yield return new object();
            }
        }
    }
}