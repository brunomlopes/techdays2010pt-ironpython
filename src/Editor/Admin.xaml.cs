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
        public event Action<Window, Step> StepChanged;

        
        public Admin(StepDirectory stepDirectory)
        {
            _stepDirectory = stepDirectory;
            InitializeComponent();
            _toggler = this.Toggler();
            _stepDirectory.StepsUpdated += (d, e) => this.Dispatcher.Invoke((Action)(() => StepsUpdated(d,e)));
            LoadItems(_stepDirectory.Steps);
        }

        public void SelectFirst()
        {
            var step = _stepDirectory.Steps.First();
            StepList.SelectedItem = step;
            StepChanged(this, step);
        }

        private void StepsUpdated(StepDirectory stepDirectory, IEnumerable<Step> steps)
        {
            Step newSelectedStep = null;
            if(StepList.SelectedItems.Count > 0)
            {
                var stepName = StepList.SelectedItems.Cast<Step>().First().Metadata.Name;
                newSelectedStep = steps.SingleOrDefault(
                    s => stepName.ToLowerInvariant() == s.Metadata.Name.ToLowerInvariant());
            }
            LoadItems(steps);

            if(newSelectedStep != null)
            {
                StepList.SelectedIndex = StepList.Items.IndexOf(newSelectedStep);
                StepChanged(this, newSelectedStep);
            }
        }

        private void LoadItems(IEnumerable<Step> steps)
        {
            StepList.Items.Clear();
            foreach (var step in steps.Where(s => s.Metadata.Name.ToLowerInvariant().Contains(Filter.Text.ToLowerInvariant())))
            {
                StepList.Items.Add(step);
            }
        }

        public void Toggle()
        {
            _toggler.MoveNext();
        }

        private void StepList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count == 0)
                return;
            StepChanged(this, e.AddedItems.Cast<Step>().First());
        }

        private void Filter_KeyUp(object sender, KeyEventArgs e)
        {
            LoadItems(_stepDirectory.Steps);
        }
    }
}
