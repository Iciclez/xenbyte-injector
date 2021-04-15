using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Xenbyte_Injector.ViewModels
{
    class DllObject
    {
        public string Path { get; set; }
        public bool IsChecked { get; set; }
    }

    class DllObjectViewModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public ImageSource Icon { get; set; }
        public string Tag { get; set; }

        private bool isChecked;

        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
