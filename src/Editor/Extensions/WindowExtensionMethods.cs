using System.Collections;
using System.Windows;
using Editor.Model;


namespace Editor.Extensions
{
    public static class WindowExtensionMethods
    {
        public static IEnumerator Toggler(this Window window)
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