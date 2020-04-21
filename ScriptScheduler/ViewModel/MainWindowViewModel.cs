using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptScheduler.ViewModel
{
    public class MainWindowViewModel
    {
        public ConfigCommand ConfigCommand { get; private set; } = new ConfigCommand();
        public ExitCommand ExitCommand { get; private set; } = new ExitCommand();

    }
}
