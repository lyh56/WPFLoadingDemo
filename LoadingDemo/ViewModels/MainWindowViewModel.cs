using Prism.Commands;
using Prism.Mvvm;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Windows;

namespace LoadingDemo.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public MainWindowViewModel()
        {
            LoadCommand = new DelegateCommand<object>(Load);
        }

        #region Commands
        public DelegateCommand<object> LoadCommand { get; private set; }

        private void Load(object parameter)
        {
            IsLoading = Convert.ToBoolean(parameter);
            if (IsLoading)
            {
                Countup = 0;
                Countdown = 5;
                Task.Run(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(1000);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Countup++;
                            Countdown--;
                        });
                        if (Countup > 5) break;
                    }

                    IsLoading = false;
                });
            }
        }
        #endregion

        #region Properties
        private bool _isLoading = false;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { this.SetProperty(ref _isLoading, value); }
        }

        private int _countdown = 0;
        public int Countdown
        {
            get { return _countdown; }
            set { this.SetProperty(ref _countdown, value); }
        }

        private int _countup = 0;
        public int Countup
        {
            get { return _countup; }
            set { this.SetProperty(ref _countup, value); }
        }

        #endregion
    }
}
