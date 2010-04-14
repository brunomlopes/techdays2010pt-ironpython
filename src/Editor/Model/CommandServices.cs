namespace Editor.Model
{
    public class CommandServices
    {
        public CommandCenter CommandCenter { get; set; }
        public MainWindow MainWindow { get; private set; }
        public StepDirectory StepDirectory { get; set; }

        public CommandServices(MainWindow mainWindow, StepDirectory stepDirectory)
        {
            MainWindow = mainWindow;
            StepDirectory = stepDirectory;
        }
    }
}