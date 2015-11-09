using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Diagnostics;
using System.Runtime.InteropServices;
using CSCore.CoreAudioAPI;
using System.Timers;

namespace AutoAccept
{
    public partial class Form1 : Form
    {
        IntPtr localhWnd;

        delegate void SetTextCallback(string text);

        Boolean scanning = false;

        System.Timers.Timer checkTimer;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);

        public Form1()
        {
            InitializeComponent();

            //Safe Windowhandle
            localhWnd = this.Handle;

            //Initialize the scanning timer
            checkTimer = new System.Timers.Timer(1000);
            checkTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent); //OnTimedEvent -> Gets called every interval.
            checkTimer.Enabled = false;
        }

        private static bool checkForSoundPeak()
        {
            using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnum = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnum)
                    {
                        using (var session2 = session.QueryInterface<AudioSessionControl2>())
                        using (var audioMeterInformation = session.QueryInterface<AudioMeterInformation>())
                        {
                            //See if Dota emits sound (If there are two Windows containing Dota in the title sound of either one will trigger!!
                            if (session2 == null) return false;
                            if (session2.Process.MainWindowTitle.Contains("Dota") && (audioMeterInformation.GetPeakValue() > 0.015))
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        private static AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {

                    var sessionManager = AudioSessionManager2.FromMMDevice(device);
                    return sessionManager;
                }
            }
        }

        private void accept_match()
        {
            String appStartPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            var programPath = System.IO.Path.Combine(appStartPath, "keystroke_simulator.exe");
            Debug.WriteLine(programPath);
            ProcessStartInfo kssi = new ProcessStartInfo(programPath);
            Process kss = Process.Start(kssi);
            kss.WaitForExit(3000);

            Debug.WriteLine("Keystokes sent");
        }


        private void SetText(string text)
        {
            if (this.lbl_status.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.lbl_status.Text = text;
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (checkForSoundPeak())
            {
                accept_match();
                SetText("Game is Ready!");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Search for the Dota 2 Process
            Process p = Process.GetProcessesByName("dota2").FirstOrDefault();
            if (p == null)
            {
                lbl_status.Text = "Dota 2 not running!";
                return;
            };
            //Implement Toggle behaviour of the button
            scanning = !scanning;

            checkTimer.Enabled = scanning;

            string btntxt = "Start";
            string lbltxt = "Press Start!";

            if (scanning)
            {
                lbltxt = "Waiting for the game to start!";
                btntxt = "Stop";
            }
            btn_toggleScan.Text = btntxt;
            lbl_status.Text = lbltxt;
        }
    }
}
