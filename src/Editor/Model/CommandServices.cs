namespace Editor.Model
{
    public class CommandServices
    {
        public MainWindow MainWindow { get; private set; }

        public CommandServices(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
        }
    }
}