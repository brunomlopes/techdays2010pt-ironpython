using System.Windows.Input;

namespace Editor
{
    public class GlobalShortcuts
    {
        public static RoutedCommand ToggleAdmin = new RoutedCommand();
        public static RoutedCommand ToggleLog = new RoutedCommand();
        public static RoutedCommand ZoomIn = new RoutedCommand();
        public static RoutedCommand ZoomOut = new RoutedCommand();

        public static void InitializeShortcuts()
        {
            ToggleAdmin.InputGestures.Add(new KeyGesture(Key.F11));
            ToggleLog.InputGestures.Add(new KeyGesture(Key.F12));

            ZoomOut.InputGestures.Add(new KeyGesture(Key.F5));
            ZoomIn.InputGestures.Add(new KeyGesture(Key.F6));
        }
    }
}