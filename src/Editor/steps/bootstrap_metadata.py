from System.Windows import Visibility

class StepMetadata(DefaultPythonMetadata):
    def __init__(self, step_path):
        super(StepMetadata, self).__init__(step_path)

    def Update(self, window, step):
        super(StepMetadata, self).Update(window, step)
        window._MainWindow__Interpreter.Visibility = Visibility.Collapsed;
        window._MainWindow__LogControl.Visibility = Visibility.Collapsed;
        window._MainWindow__Execute.Visibility = Visibility.Collapsed;
