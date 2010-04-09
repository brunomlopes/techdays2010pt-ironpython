using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NLog;

namespace Editor
{
    /// <summary>
    /// Interaction logic for Log.xaml
    /// </summary>
    public partial class Log : Window
    {
        private IEnumerator _toggler;

        public Target Target
        {
            get { return LogControl.Target; }
        }

        public Log()
        {
            InitializeComponent();
            _toggler = this.Toggler().GetEnumerator();
        }

        public void Toggle()
        {
            _toggler.MoveNext();
        }
    }
}
