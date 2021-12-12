﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace BNK_To_WwiseProject
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);
        private static readonly string mutexName = "SRTTbacon_WoTB_Voice_Mod_Creater_V1.4.9.2";
        private static readonly Mutex mutex = new Mutex(false, mutexName);
        private static bool hasHandle = false;
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                hasHandle = mutex.WaitOne(0, false);
                if (!hasHandle)
                {
                    MessageBoxResult result = MessageBox.Show("既にアプリが起動されています。\nソフトを強制終了させますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);
                    if (result == MessageBoxResult.Yes)
                    {
                        Process[] p = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                        foreach (Process e_Now in p)
                            e_Now.Kill();
                    }
                    else
                        Shutdown();
                    return;
                }
            }
            catch
            {

            }
            base.OnStartup(e);
        }
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            if (hasHandle)
                mutex.ReleaseMutex();
            mutex.Close();
        }
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            //dllの位置を変更
            string dllPath = System.IO.Path.Combine(System.IO.Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName, @"Resources");
            _ = SetDllDirectory(dllPath);
            MainCode Windows = new MainCode();
            Windows.Show();
        }
    }
}