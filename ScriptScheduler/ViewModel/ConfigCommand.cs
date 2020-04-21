using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ScriptScheduler
{
    public class ConfigCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public static ConfigWindow Config { get; } = new ConfigWindow();

        bool ICommand.CanExecute(object parameter)
        {
            return (Config == null || !Config.ShowFlag);
        }

        void ICommand.Execute(object parameter)
        {
            Log.Logger.Info("コンフィグウィンドウを表示します");
            Config.Window_Show();
        }
    }
}
