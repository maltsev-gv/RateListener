using System;
using System.Windows;

namespace RateListener.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        protected static void RunInMainThread(Action action) =>
            Application.Current.Dispatcher.BeginInvoke(action);
    }
}
