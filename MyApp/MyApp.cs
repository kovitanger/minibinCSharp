﻿using System;
using System.Text;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace RecycleBinApp
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SHQUERYRBINFO
    {
        public uint cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    class Program
    {
        [DllImport("shell32.dll")]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        [DllImport("shell32.dll")]
        static extern int SHGetFolderPath(IntPtr hwnd, int csidl, IntPtr hToken, uint dwFlags, StringBuilder pszPath);

        [DllImport("shell32.dll")]
        static extern int SHQueryRecycleBin(IntPtr hwnd, ref SHQUERYRBINFO pSHQueryRBInfo);

        static NotifyIcon trayIcon;
        static Icon emptyIcon;
        static Icon fullIcon;
        static bool lastRecycleBinState = false; 

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ApplicationContext context = new ApplicationContext();

            trayIcon = new NotifyIcon();
            Bitmap emptyBitmap = new Bitmap("icons/minibin-kt-empty.ico");
            emptyIcon = Icon.FromHandle(emptyBitmap.GetHicon());

            Bitmap fullBitmap = new Bitmap("icons/minibin-kt-full.ico");
            fullIcon = Icon.FromHandle(fullBitmap.GetHicon());

            trayIcon.Icon = emptyIcon;
            trayIcon.Visible = true;

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Открыть корзину");
            ToolStripMenuItem emptyMenuItem = new ToolStripMenuItem("Очистить корзину");
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Выход");

            openMenuItem.Click += OpenRecycleBin;
            emptyMenuItem.Click += EmptyRecycleBin;
            exitMenuItem.Click += ExitProgram;

            trayMenu.Items.Add(openMenuItem);
            trayMenu.Items.Add(emptyMenuItem);
            trayMenu.Items.Add(exitMenuItem);

            trayIcon.ContextMenuStrip = trayMenu;

            Thread updateThread = new Thread(PeriodicUpdate);
            updateThread.IsBackground = true;
            updateThread.Start();

            Application.Run(context);
        }

        static void OpenRecycleBin(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("shell:RecycleBinFolder");
        }

        static void EmptyRecycleBin(object sender, EventArgs e)
        {
            uint flags = 0x01; // SHERB_NOCONFIRMATION
            StringBuilder binPath = new StringBuilder(260);
            SHGetFolderPath(IntPtr.Zero, 0x0005, IntPtr.Zero, 0, binPath);
            int result = SHEmptyRecycleBin(IntPtr.Zero, binPath.ToString(), flags);

            if (result == 0 || result == -2147418113)
            {
                ShowNotification("Trash", "The trash has been successfully cleared.");
            }
            else
            {
                ShowNotification("Trash", "An error occurred while emptying the trash. Error code: " + result.ToString());
            }

            UpdateIcon();
        }

        static void ExitProgram(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            emptyIcon.Dispose();
            fullIcon.Dispose();
            Application.Exit();
        }

        static void UpdateIcon()
        {
            bool isEmpty = IsRecycleBinEmpty();
            if (isEmpty != lastRecycleBinState)
            {
                trayIcon.Icon = isEmpty ? emptyIcon : fullIcon;
                lastRecycleBinState = isEmpty;
            }
        }

        static bool IsRecycleBinEmpty()
        {
            SHQUERYRBINFO rbInfo = new SHQUERYRBINFO();
            rbInfo.cbSize = (uint)Marshal.SizeOf(typeof(SHQUERYRBINFO));
            int result = SHQueryRecycleBin(IntPtr.Zero, ref rbInfo);

            if (result != 0)
            {
                Console.WriteLine("An error occurred when requesting the status of the bucket.");
                return false;
            }

            return rbInfo.i64NumItems == 0;
        }

        static void PeriodicUpdate()
        {
            while (true)
            {
                UpdateIcon();
                Thread.Sleep(3000); 
            }
        }

        static void ShowNotification(string title, string message)
        {
            trayIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.None);
        }
    }
}
