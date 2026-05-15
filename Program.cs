using System;
using System.Windows.Forms;

namespace HotKeyHelper
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new HotKeyContext());
        }
    }
}