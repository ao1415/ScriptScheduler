using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ScriptScheduler
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private System.Threading.Mutex mutex = new System.Threading.Mutex(false, "ScriptScheduler");

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!mutex.WaitOne(0, false))
            {
                Log.Logger.Warn("すでに起動しています");
                mutex.Close();
                mutex = null;
                Shutdown();
                Log.Logger.Warn("アプリケーションを終了させます");
            }
            ConfigCommand.Config.StartTimer();
            
            Log.Logger.Info("アプリケーションを起動します");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Close();
                Log.Logger.Info("多重起動を解除します");
            }
            Log.Logger.Info("アプリケーションを終了します");
        }
    }
}
