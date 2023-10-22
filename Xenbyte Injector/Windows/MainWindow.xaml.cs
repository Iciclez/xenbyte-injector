using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xenbyte_Injector.Components;
using Xenbyte_Injector.ViewModels;

namespace Xenbyte_Injector.Windows
{
    public partial class MainWindow : Window
    {
        private string config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xenbyte Injector", "config.json");
        private ObservableCollection<DllObjectViewModel> dllObjectCollection = new ObservableCollection<DllObjectViewModel>();
        private Timer autoRefreshTimer = new Timer();
        public bool AutoInjectionIsRunning { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            Title = $"Xenbyte Injector | Process Id: {Process.GetCurrentProcess().Id:X8} ({Process.GetCurrentProcess().Id})";

            processListView.DataContext = new ObservableCollection<object>();
            dllListBox.DataContext = dllObjectCollection;

            refreshProcessListMenuItem_Click(null, null);

            autoRefreshTimer.Interval = 5000;
            autoRefreshTimer.Elapsed += new ElapsedEventHandler((sender, e) => Dispatcher.Invoke(() => refreshProcessListMenuItem_Click(null, null)));

            AutoInjectionIsRunning = false;

            LoadConfiguration();
        }

        public object GetAnonymousObjectPropertyValue(object anonymousObject, string property)
        {
            return anonymousObject.GetType().GetProperty(property).GetValue(anonymousObject, null);
        }

        public void SaveConfiguration()
        {
            string directory = Path.GetDirectoryName(config);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(config, JsonSerializer.SerializeToUtf8Bytes(new Configuration
            {
                AutomaticInjectionDataType = autoInjectionComboBox.SelectedIndex,
                AutomaticInjectionData = autoInjectionTextBox.Text,
                AutomaticRefreshProcess = autoRefreshCheckBox.IsChecked != null && (bool)autoRefreshCheckBox.IsChecked,
                AutomaticRefreshProcessDelay = (int)autoRefreshTimer.Interval,
                Freeze = freezeProcessCheckBox.IsChecked != null && (bool)freezeProcessCheckBox.IsChecked,
                DllList = dllObjectCollection.Select(o => new DllObject { Path = o.Tag, IsChecked = o.IsChecked })
            }));
        }

        public void LoadConfiguration()
        {
            if (File.Exists(config))
            {
                Configuration configuration = JsonSerializer.Deserialize<Configuration>(File.ReadAllBytes(config));

                autoInjectionComboBox.SelectedIndex = configuration.AutomaticInjectionDataType;
                autoInjectionTextBox.Text = configuration.AutomaticInjectionData;
                autoRefreshCheckBox.IsChecked = configuration.AutomaticRefreshProcess;
                autoRefreshTimer.Interval = configuration.AutomaticRefreshProcessDelay;
                freezeProcessCheckBox.IsChecked = configuration.Freeze;

                foreach (DllObject dll in configuration.DllList)
                {
                    if (File.Exists(dll.Path) && !dllObjectCollection.Select(o => o.Tag.ToLowerInvariant()).Contains(dll.Path.ToLowerInvariant()))
                    {
                        dllObjectCollection.Add(new DllObjectViewModel()
                        {
                            Name = Path.Combine(new DirectoryInfo(Path.GetDirectoryName(dll.Path)).Name, Path.GetFileName(dll.Path)),
                            Icon = Utilities.GetFileIcon(Path.GetExtension(dll.Path)),
                            Tag = dll.Path,
                            IsChecked = dll.IsChecked
                        });
                    }

                }
            }
        }

        private void addToLibraryListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "Dll files (*.dll)|*.dll|Executable files (*.exe)|*.exe"
            };

            if (openFileDialog.ShowDialog() == true && File.Exists(openFileDialog.FileName) && !dllObjectCollection.Select(o => o.Tag.ToLowerInvariant()).Contains(openFileDialog.FileName.ToLowerInvariant()))
            {
                dllObjectCollection.Add(new DllObjectViewModel()
                {
                    //Name = Path.GetFileName(openFileDialog.FileName), 
                    Name = Path.Combine(new DirectoryInfo(Path.GetDirectoryName(openFileDialog.FileName)).Name, Path.GetFileName(openFileDialog.FileName)),
                    Icon = Utilities.GetFileIcon(Path.GetExtension(openFileDialog.FileName)),
                    Tag = openFileDialog.FileName,
                    IsChecked = false
                });
            }
        }

        private void removeSelectedLibraryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (dllListBox.SelectedItem != null)
            {
                dllObjectCollection.Remove(dllListBox.SelectedItem as DllObjectViewModel);
            }
        }

        private void openContainingFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (dllListBox.SelectedItem != null)
            {
                string path = (dllListBox.SelectedItem as DllObjectViewModel).Tag;
                if (path != null && File.Exists(path))
                {
                    Process.Start("explorer.exe", Path.GetDirectoryName(path));
                }
            }
        }

        private void clearLibrariesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            dllObjectCollection.Clear();
        }

        private void refreshProcessListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<object> observableCollection = (ObservableCollection<object>)processListView.DataContext;

            int selectedId = -1;

            if (processListView.SelectedItem != null)
            {
                selectedId = (int)GetAnonymousObjectPropertyValue(processListView.SelectedItem, "Id");
            }

            observableCollection.Clear();

            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    observableCollection.Add(new
                    {
                        Name = process.MainModule.ModuleName,
                        Icon = Utilities.GetFileIcon(process.MainModule.FileName),
                        Tag = process.MainModule.FileName,
                        Id = process.Id
                    });
                }
                catch (Win32Exception)
                {
                    observableCollection.Add(new
                    {
                        Name = process.ProcessName,
                        Icon = Utilities.GetFileIcon(".exe"),
                        Tag = process.ProcessName,
                        Id = process.Id
                    });
                }
                catch (Exception) { }

                if (selectedId == process.Id)
                {
                    processListView.SelectedItem = observableCollection[observableCollection.Count - 1];
                }
            }
        }

        private void suspendProcessMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (processListView.SelectedItem != null)
            {
                int processId = (int)GetAnonymousObjectPropertyValue(processListView.SelectedItem, "Id");

                if (processId > 0)
                {
                    new Inject(Process.GetProcessById(processId)).Suspend();
                }
            }
        }

        private void resumeProcessMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (processListView.SelectedItem != null)
            {
                int processId = (int)GetAnonymousObjectPropertyValue(processListView.SelectedItem, "Id");

                if (processId > 0)
                {
                    new Inject(Process.GetProcessById(processId)).Resume();
                }
            }
        }

        private void terminateProcessMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (processListView.SelectedItem != null)
            {
                int processId = (int)GetAnonymousObjectPropertyValue(processListView.SelectedItem, "Id");

                if (processId > 0)
                {
                    Process.GetProcessById(processId).Kill();

                    refreshProcessListMenuItem_Click(sender, e);
                }
            }
        }

        private void injectDllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (processListView.SelectedItem != null)
            {
                int processId = (int)GetAnonymousObjectPropertyValue(processListView.SelectedItem, "Id");

                OpenFileDialog openFileDialog = new OpenFileDialog()
                {
                    Filter = "Dll files (*.dll)|*.dll|Executable files (*.exe)|*.exe"
                };

                if (processId > 0 && openFileDialog.ShowDialog() == true && File.Exists(openFileDialog.FileName))
                {
                    new Inject(Process.GetProcessById(processId), InjectionRoutine.LoadLibraryA, (bool)freezeProcessCheckBox.IsChecked).InjectLibrary(new List<string> { openFileDialog.FileName });
                }
            }
        }

        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void taskManagerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("Taskmgr.exe");
        }

        private void aboutXenbyteInjectorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Xenbyte Injector\t\t\t2.0.0.0\n\nCreated by Iciclez", "About Xenbyte Injector", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void processListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (processListView.SelectedIndex == -1)
            {
                injectButton.ToolTip = null;
            }
            else
            {
                int id = (int)GetAnonymousObjectPropertyValue(processListView.SelectedItem, "Id");
                injectButton.ToolTip = new ToolTip()
                {
                    Content = $"Target Process: {GetAnonymousObjectPropertyValue(processListView.SelectedItem, "Name")} {id:X8} ({id})",
                    Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#FF1C1E26"),
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFE1E1E1")
                };
            }
        }

        private void injectButton_Click(object sender, RoutedEventArgs e)
        {
            if (processListView.SelectedItem != null)
            {
                List<string> dllList = dllObjectCollection.Where(o => o.IsChecked).Select(o => o.Tag).ToList();
                int processId = (int)GetAnonymousObjectPropertyValue(processListView.SelectedItem, "Id");

                if (processId > 0 && dllList.Count > 0)
                {
                    new Inject(Process.GetProcessById(processId), InjectionRoutine.LoadLibraryA, (bool)freezeProcessCheckBox.IsChecked).InjectLibrary(dllList);
                }
            }
        }

        private void autoInjectionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            injectButton.Visibility = Visibility.Hidden;
            autoInjectionTextBox.IsEnabled = false;
            autoInjectionComboBox.IsEnabled = false;

            int autoInjectionDataType = autoInjectionComboBox.SelectedIndex;
            string autoInjectionData = autoInjectionDataType == 0 ? Path.GetFileNameWithoutExtension(autoInjectionTextBox.Text) : autoInjectionTextBox.Text;
            bool freezeProcess = (bool)freezeProcessCheckBox.IsChecked;
            AutoInjectionIsRunning = true;

            Task.Factory.StartNew(() =>
            {
                bool isRunning = false;

                while (AutoInjectionIsRunning)
                {
                    List<Process> processes = null;
                    switch (autoInjectionDataType)
                    {
                        case 0:
                            processes = Process.GetProcessesByName(autoInjectionData).ToList();
                            break;

                        case 1:
                            processes = new List<Process> { Utilities.GetProcessByWindowName(autoInjectionData) };
                            break;

                        case 2:
                            processes = new List<Process> { Utilities.GetProcessByClassName(autoInjectionData) };
                            break;
                    }

                    if (processes.Count > 0 && processes[0] != null)
                    {
                        if (!isRunning)
                        {
                            try
                            {
                                new Inject(processes, InjectionRoutine.LoadLibraryA, freezeProcess)
                                    .InjectLibrary(dllObjectCollection.Where(o => o.IsChecked).Select(o => o.Tag).ToList());
                            }
                            catch(Exception)
                            {

                            }

                            isRunning = true;
                            Task.Delay(300);
                        }
                    }
                    else
                    {
                        if (isRunning)
                        {
                            isRunning = false;
                        }
                    }
                }
            });
        }
        private void autoInjectionCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            injectButton.Visibility = Visibility.Visible;
            autoInjectionTextBox.IsEnabled = true;
            autoInjectionComboBox.IsEnabled = true;
            AutoInjectionIsRunning = false;
        }

        private void autoRefreshCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            autoRefreshTimer.Enabled = true;
        }

        private void autoRefreshCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            autoRefreshTimer.Enabled = false;
        }

        private void suspenderTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suspenderTabControl.SelectedIndex == suspenderTabControl.Items.Count - 1)
            {
                (suspenderTabControl.Items[suspenderTabControl.SelectedIndex] as TabItem).Header = int.Parse((suspenderTabControl.Items[suspenderTabControl.SelectedIndex - 1] as TabItem).Header.ToString()) + 1;
                suspenderTabControl.Items.Add(new TabItem
                {
                    Header = "",
                    Content = new Frame
                    {
                        Source = new Uri("Pages/AutomaticSuspenderPage.xaml", UriKind.Relative)
                    }
                });
            }
        }

        private void mainWindow_Closing(object sender, CancelEventArgs e)
        {
            SaveConfiguration();
        }
    }
}
