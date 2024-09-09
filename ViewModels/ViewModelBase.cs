using System;
using System.Windows;
using System.Windows.Input;


namespace RateListener.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        public ViewModelBase()
        {
        }

        public bool IsRefreshing
        {
            get => GetVal<bool>();
            set => SetVal(value);
        }

        public string Title
        {
            get => GetVal<string>();
            set => SetVal(value);
        }

        public bool IsBackButtonPresent
        {
            get => GetVal<bool>();
            set => SetVal(value);
        }

        public ICommand BackButtonPressedCommand { get; }

        protected virtual void OnBackButtonPressed()
        {
        }

        public virtual bool IsSameAs(ViewModelBase viewModel)
        {
            return false;
        }

        public void RunInMainThread(Action action)
        {
            Application.Current.Dispatcher.BeginInvoke(action);
        }
    }
}
