using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//Windows API
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Collections.Specialized;
//Window Focus changed hooks
//using System.Windows.Automation;
using System.Diagnostics;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        IntPtr clientHandle = IntPtr.Zero;
        double xRatio = 0.4246;
        double yRatio = 0.5638;
        Boolean isReady = false;

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        private const int HSHELL_WINDOWCREATED = 1;
        public delegate bool WindowEnumCallback(int hwnd, int lparam);
        private int uMsgNotify;
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(WindowEnumCallback lpEnumFunc, int lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr h, StringBuilder s, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(int h);
        [DllImport("user32.dll")]
        private static extern int RegisterWindowMessage(string lpString);
        [DllImport("user32.dll")]
        private static extern int RegisterShellHookWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern void FlashWindow(IntPtr a, bool b);
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool CloseWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        //Window Focus Change
        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        //Get Window Bounds
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        public static Size GetWindowSize(IntPtr hWnd)
        {
            RECT pRect;
            Size cSize = new Size();
            // get coordinates relative to window
            GetWindowRect(hWnd, out pRect);

            cSize.Width = pRect.Right - pRect.Left;
            cSize.Height = pRect.Bottom - pRect.Top;

            return cSize;
        }


        public Form1()
        {
            Console.WriteLine("Form initialized");
            InitializeComponent();
            WinEventDelegate dele = new WinEventDelegate(WinEventProc);
            IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Console.Write("Accept button clicked for");
            Console.WriteLine(clientHandle.ToString());
            if (clientHandle != IntPtr.Zero)
            {
                acceptQueue(clientHandle);
                isReady = false;
                button1.Enabled = false;
                stat.Text = "Idle";
                stat.ForeColor = SystemColors.Info;
            }
            else
                MessageBox.Show("Queue not ready!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!isReady)
            {
                Console.WriteLine("Ready to detect queue pop");
                isReady = true;
                stat.Text = "Waiting for queue";
                stat.ForeColor = System.Drawing.Color.Red;
                button2.Text = "Cancel";
            }
            else
            {
                Console.WriteLine("Ready cancelled");
                isReady = false;
                button2.Text = "Ready";
                stat.Text = "Idle";
                stat.ForeColor = SystemColors.Info;
            }
        }

        private void acceptQueue(IntPtr handle)
        {
            //Bring window to the top
            BringWindowToTop(handle);
            //Get Window bounds and calculate button coords
            Point button = getButtonCoordinates(handle);
            ClientToScreen(handle,ref button);
            //Click button
            ClickOnPoint(button);
            //Reset state
            Console.WriteLine("---QUEUE ACCEPTED---");
        }

        private Point getButtonCoordinates(IntPtr handle)
        {
            //Rectangle resolution = Screen.PrimaryScreen.Bounds;
            //return new Point(BUTTON_X * resolution.Width / 1920, BUTTON_Y * resolution.Height / 1080);
            Size bounds = GetWindowSize(handle);
            int buttonX = (int)(xRatio * bounds.Width);
            int buttonY = (int)(yRatio * bounds.Height);
            Console.WriteLine("X: " + buttonX.ToString() + " Y: " + buttonY.ToString());
            return new Point(buttonX, buttonY);
        }

        private void ClickOnPoint(Point clientPoint)
        {
            Point oldPoint;
            GetCursorPos(out oldPoint);

            /// set cursor on coords, and press mouse
            SetCursorPos(clientPoint.X, clientPoint.Y);
            mouse_event(0x00000002, 0, 0, 0, UIntPtr.Zero); /// left mouse button down
            mouse_event(0x00000004, 0, 0, 0, UIntPtr.Zero); /// left mouse button up

            /// return mouse 
            SetCursorPos(oldPoint.X, oldPoint.Y);
        }

        private IntPtr detectQueuePop()
        {
            const int nChars = 256;
            IntPtr handle = IntPtr.Zero;
            StringBuilder Buff = new StringBuilder(nChars);
            handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0 && Buff.ToString() == "PVP.net Client")
            {
                return handle;
            }
            return IntPtr.Zero;
        }

        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            IntPtr tempClientHandle = detectQueuePop();
            if (tempClientHandle != IntPtr.Zero && isReady)
            {
                clientHandle = tempClientHandle;
                Console.WriteLine("QUEUE DETECTED!!!");
                Console.WriteLine(clientHandle.ToString());
                stat.Text = "Queue Ready";
                button1.Enabled = true;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            return;
        } 


        
    }
}
