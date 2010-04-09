using System.Windows.Input;

namespace Editor
{
    public class GlobalShortcuts
    {
        public static RoutedCommand ToggleAdmin = new RoutedCommand();
        public static RoutedCommand ToggleLog = new RoutedCommand();

        public static void InitializeShortcuts()
        {
            ToggleAdmin.InputGestures.Add(new KeyGesture(Key.F11));
            ToggleLog.InputGestures.Add(new KeyGesture(Key.F12));
        }
    }
}