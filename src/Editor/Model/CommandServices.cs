namespace Editor.Model
{
    public class CommandServices
    {
        public MainWindow MainWindow { get; private set; }

        public CommandServices(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
        }

        public void Execute(string command, string parameters)
        {
            CommandCenter.ExecuteFromName(command, parameters);
        }
    }
}