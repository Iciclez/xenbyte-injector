using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Xenbyte_Injector.Components;

namespace Xenbyte_Injector.Pages
{
    public partial class AutomaticSuspenderPage : Page
    {
        public bool AutoSuspenderIsRunning { get; set; }
        public AutomaticSuspenderPage()
        {
            InitializeComponent();

            AutoSuspenderIsRunning = false;
        }

        private void autoSuspendCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AutoSuspenderIsRunning = false;

            string processName = Path.GetFileNameWithoutExtension(processNameTextBox.Text.Trim());
            int delay = 0;
            if (!int.TryParse(delayTextBox.Text, out delay) || processName == string.Empty)
            {
                autoSuspendCheckBox.IsChecked = false;
                return;
            }

            processNameTextBox.IsEnabled = false;
            delayTextBox.IsEnabled = false;
            AutoSuspenderIsRunning = true;

            Task.Factory.StartNew(() =>
            {
                HashSet<int> suspendedContainer = new HashSet<int>();
                IEnumerable<int> processIdContainer = null;

                while (AutoSuspenderIsRunning)
                {
                    processIdContainer = Process.GetProcessesByName(processName).ToList().Select(o => o.Id);

                    if (processIdContainer.Count() > 0)
                    {
                        Task.Delay(delay > 0 ? delay : 333);

                        foreach (int processId in processIdContainer)
                        {
                            if (!suspendedContainer.Contains(processId))
                            {
                                new Inject(Process.GetProcessById(processId)).Suspend();
                                suspendedContainer.Add(processId);
                            }
                        }
                    }
                    
                }
            });

        }

        private void autoSuspendCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AutoSuspenderIsRunning = false;
            processNameTextBox.IsEnabled = true;
            delayTextBox.IsEnabled = true;

            new Inject(Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processNameTextBox.Text.Trim())).ToList()).Resume();
        }
    }
}
