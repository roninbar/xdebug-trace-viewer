using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace xdebug_trace_viewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = 0 < args.Length ? new Form1(args[0]) : new Form1();
            Application.Run(form);
        }
    }
}
