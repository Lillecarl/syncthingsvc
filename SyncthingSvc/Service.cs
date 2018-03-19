using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncthingSvc
{
    public class Service
    {
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Process syncthing;
        private object loglock = new object();

        public Service()
        {
        }

        public void Start()
        {
            if (!Environment.UserInteractive)
            {
                try { Environment.CurrentDirectory = (string)Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey(Program.ServiceName).GetValue("AppLocation"); }
                catch { }
            }

            var task = new Task(() => Run(cts.Token));
            task.Start();
        }

        public void Stop()
        {
            cts.Cancel();

            if (syncthing != null)
            {
                syncthing.CloseMainWindow();
                syncthing.Close();
            }
        }

        private void Run(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                syncthing = new Process();
                syncthing.StartInfo = new ProcessStartInfo()
                {
                    FileName = "syncthing.exe",
                    WorkingDirectory = Environment.CurrentDirectory,
                    Arguments = string.Format("-no-restart -no-browser -home=\"{0}\"", Environment.CurrentDirectory),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                syncthing.EnableRaisingEvents = true;
                syncthing.OutputDataReceived += Syncthing_DataReceived;
                syncthing.ErrorDataReceived += Syncthing_DataReceived;

                if (!Environment.UserInteractive)
                    syncthing.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                Console.WriteLine("Starting syncthing");
                syncthing.Start();
                syncthing.BeginOutputReadLine();
                syncthing.BeginErrorReadLine();
                syncthing.WaitForExit();
                Console.WriteLine("Sleeping for 10 seconds");
                Thread.Sleep(10000);
            }
        }

        private void Syncthing_DataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (loglock)
            {
                File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "syncthingsvc.log"), string.Format("{0}{1}", e.Data, Environment.NewLine));
                Console.WriteLine(e.Data);
            }
        }

        public static void Install(string ServiceName)
        {
            Registry.LocalMachine.OpenSubKey("SOFTWARE", true).CreateSubKey(ServiceName).SetValue("AppLocation", Environment.CurrentDirectory);
        }

        public static void Uninstall(string ServiceName)
        {
            var key = Registry.LocalMachine.OpenSubKey("SOFTWARE", true).CreateSubKey(ServiceName);
            key.DeleteValue("AppLocation");

            if (key.GetValueNames().Count() == 0 && key.GetSubKeyNames().Count() == 0)
                Registry.LocalMachine.OpenSubKey("SOFTWARE", true).DeleteSubKeyTree(ServiceName);
        }
    }
}
