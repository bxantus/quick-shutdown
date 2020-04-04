using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using KbdListener;
using System.Diagnostics;
using System.Management;


namespace ShutdownTest
{
    public partial class TimerForm : Form
    {
        public TimerForm()
        {
            InitializeComponent();
            KeyboardListener.s_KeyEventHandler += new KeyboardListener.KeyboardHandler(OnKeyEvent);
            hideTimer.Elapsed += new System.Timers.ElapsedEventHandler(hideTimer_Elapsed);
            hideTimer.SynchronizingObject = this;
            
            checkEllapsedTime.Elapsed += new System.Timers.ElapsedEventHandler(checkEllapsedTime_Elapsed);
            checkEllapsedTime.SynchronizingObject = this;
            checkEllapsedTime.AutoReset = true;

            UpdateTimeDisplay();
            // start hiding timer 
            hideTimer.AutoReset = false;
            hideTimer.Interval = 3000;
            hideTimer.Enabled = true;
        }

        void checkEllapsedTime_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!stopWatch.IsRunning) return;

            long remaining = targetTime - stopWatch.ElapsedMilliseconds;
            timeLeft = remaining / 1000;
            

            if (timeLeft <= 0)
            {
                checkEllapsedTime.Enabled = false;
                timeLeft = 0;
                InitiateShutdown();
            } else if (remaining < SHOW_REMAINING_TIME)
            {
                if (!Visible)
                {
                    Visible = true;
                    TopMost = true;
                    checkEllapsedTime.Interval = 100; 
                }
            }
            else if (remaining <= START_FREQUENT_TIMER)
            {
                checkEllapsedTime.Interval = 1000; // check each second 
            }

            if (Visible) UpdateTimeDisplay();
        }

        void OnKeyEvent(EventArgs e, Dictionary<ushort, KeyState> keyStates)
        {
            if (keyStates[TIMER_KEY] == KeyState.DOWN && keyStates[CONTROL] == KeyState.DOWN)
            {
                hideTimer.Enabled = false;
                if (stopWatch.IsRunning) // have to update time left
                {
                    timeLeft = (targetTime - stopWatch.ElapsedMilliseconds) / 1000;
                }

                if (Visible)
                {
                    StopTiming();
                    IncTimeleft();
                }

                UpdateTimeDisplay();
                if (!Visible)
                {
                    Visible = true;
                    TopMost = true;
                    checkEllapsedTime.Interval = 100; // have to update counter
                }

                // start hiding timer 
                hideTimer.AutoReset = false;
                hideTimer.Interval = 3000;
                hideTimer.Enabled = true;
            }
        }

        void hideTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Visible = false;
            StartTiming();
        }

        void InitiateShutdown()
        {
            ManagementBaseObject mboShutdown = null;
            ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
            mcWin32.Get();

            // You can't shutdown without security privileges
            mcWin32.Scope.Options.EnablePrivileges = true;
            ManagementBaseObject mboShutdownParams =
                     mcWin32.GetMethodParameters("Win32Shutdown");

            // Flag 1 means we want to shut down the system. Use "2" to reboot.
            mboShutdownParams["Flags"] = "1";
            mboShutdownParams["Reserved"] = "0";
            foreach (ManagementObject manObj in mcWin32.GetInstances())
            {
                mboShutdown = manObj.InvokeMethod("Win32Shutdown",
                                               mboShutdownParams, null);
            }
        }

        // Util methods ----------------------------------------------------
        private void UpdateTimeDisplay()
        {
            if (timeLeft == 0 && !stopWatch.IsRunning)
            {
                lblTimeLeft.Text = "OFF";
            }   
            else
            {// TODO: change to red in last minute
                lblTimeLeft.Text = String.Format("{0:D}:{1:D2}", timeLeft / 60, timeLeft % 60);
            }
        }

        private void IncTimeleft()
        {
            const int MIN_15 = 15 * 60;
            timeLeft /= MIN_15;
            timeLeft++;
            timeLeft *= MIN_15;

            if (timeLeft > MAX_TIME)
            {
                timeLeft = 0; // should stop
            }
        }

        private void StartTiming()
        {
            if (timeLeft > 0 && !stopWatch.IsRunning)
            {
                targetTime = timeLeft * 1000; // in millisecs
                stopWatch.Start();

                // start counting timer   
                checkEllapsedTime.Enabled = true;
                checkEllapsedTime.Interval = 60 * 1000; // check each second 
            }
            else if (timeLeft * 1000 <= START_FREQUENT_TIMER)
            {
                checkEllapsedTime.Interval = 1000; // check each second 
            }
        }

        private void StopTiming()
        {
            stopWatch.Reset();
            checkEllapsedTime.Enabled = false;
        }

        const ushort TIMER_KEY = 0x73; // F4 key
        const ushort CONTROL = 0x11;

        const long MAX_TIME = 180 * 60;
        const long START_TIME = 0;
        
        const long START_FREQUENT_TIMER = 2 * 60 * 1000; // 2 mins
        const long SHOW_REMAINING_TIME = 1 * 60 * 1000;

        private long timeLeft = 0;    // time left in seconds
        private long targetTime = 0;  // targetTime in millisecs
        private Stopwatch stopWatch = new Stopwatch();
        private System.Timers.Timer hideTimer = new System.Timers.Timer();
        private System.Timers.Timer checkEllapsedTime = new System.Timers.Timer();
    }
}
