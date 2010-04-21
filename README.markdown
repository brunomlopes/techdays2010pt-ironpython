This is the application and presentation I used for my TechDays 2010 IronPython presentation.

To "watch" the presentation just build it with Visual Studio 2010 and run it.
The slides are IronPython.pptx and the ironpython sample code is in src\Editor\steps

The keys are as follows (defined in GlobalShortcuts.cs):

F1-Previous slide
F2-Next slide

F5-Executes code in the text editor

F7-Zoom in the interface
F8-Zoom out the interface

F11-Show admin window (with slides and command interface)
F12-Show log window

The admin window includes a bottom text box where you can execute commands defined in python.
The app loads the commands from the commands directory.