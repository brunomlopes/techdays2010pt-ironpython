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

namespace Editor
{
    public partial class Admin : Window
    {
        private IEnumerator _toggler;

        public Admin()
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
