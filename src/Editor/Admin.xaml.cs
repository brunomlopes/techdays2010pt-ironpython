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
using Editor.Model;

namespace Editor
{
    public partial class Admin : Window
    {
        private readonly StepDirectory _stepDirectory;
        private IEnumerator _toggler;

        public Admin(StepDirectory stepDirectory)
        {
            _stepDirectory = stepDirectory;
            InitializeComponent();
            _toggler = this.Toggler().GetEnumerator();

            LoadItems(_stepDirectory.Steps);

        }

        private void LoadItems(IEnumerable<Step> steps)
        {
            StepList.Items.Clear();
            foreach (var step in steps)
            {
                StepList.Items.Add(step.Metadata.Name);
            }
        }

        public void Toggle()
        {
            _toggler.MoveNext();
        }

        
    }
}
