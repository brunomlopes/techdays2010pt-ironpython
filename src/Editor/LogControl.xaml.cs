using System.Windows.Controls;
using NLog;

namespace Editor
{
    public partial class LogControl : UserControl
    {
        public Target Target { get; private set; }

        public LogControl()
        {
            InitializeComponent();
            Target = new TextBlockTarget(LogTextBlock, LogTextScroll);
        }

        public void Clear()
        {
            LogTextBlock.Text = string.Empty;
        }

        private class TextBlockTarget : Target
        {
            private readonly TextBlock _block;
            private readonly ScrollViewer _viewer;

            public TextBlockTarget(TextBlock block, ScrollViewer viewer)
            {
                _block = block;
                _viewer = viewer;
            }

            protected override void Write(LogEventInfo logEvent)
            {
                _block.Text += logEvent.Message;
                _viewer.ScrollToBottom();
                
            }
        }
    }
}