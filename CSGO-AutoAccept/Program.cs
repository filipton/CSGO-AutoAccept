using ConsoleHotKey;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSGOAutoAccept
{
    class Program
    {
        static bool ServiceRunning = false;
        static bool HassNotifyService = false;
        static string ip = "http://127.0.0.1:8123";
        static string auth = "null";

        static void Main(string[] args)
        {
            Console.Title = $"Service is: {(ServiceRunning ? "Running" : "Disabled")}";
            if (File.Exists("config.json"))
            {
                ConfigJson cj = JsonConvert.DeserializeObject<ConfigJson>(File.ReadAllText("config.json"));
                HassNotifyService = cj.hassnotify;
                ip = cj.ip;
                auth = cj.authkey;
                SendNotification("CSGO AUTO CONNECT", "Test message!");
            }

            new Thread(() =>
            {
                while (true)
                {
                    if (ServiceRunning)
                    {
                        try
                        {
                            string[] data = UseImageSearch(@"Resources\acc.png", "50");
                            string[] data2 = UseImageSearch(@"Resources\acc2.png", "50");
                            if (data != null && int.TryParse(data[1], out int x) && int.TryParse(data[2], out int y))
                            {
                                Console.WriteLine($"IMG1: {x} {y}");
                                LeftMouseClick(x, y);
                                if (HassNotifyService)
                                {
                                    SendNotification("CSGO AUTO CONNECT", "Match found!");
                                }
                            }
                            else if (data2 != null && int.TryParse(data2[1], out int x2) && int.TryParse(data2[2], out int y2))
                            {
                                Console.WriteLine($"IMG2: {x2} {y2}");
                                LeftMouseClick(x2, y2);
                                if (HassNotifyService)
                                {
                                    SendNotification("CSGO AUTO CONNECT", "Match found!");
                                }
                            }
                        }
                        catch { }
                    }
                    Thread.Sleep(2500);
                }
            }).Start();

            HotKeyManager.RegisterHotKey(Keys.PageUp, KeyModifiers.Control);
            HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_HotKeyPressed);
            Console.ReadLine();
        }

        static void SendNotification(string title, string message)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"{ip}/api/services/notify/mobile_app_sm_j710f"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {auth}");

                    request.Content = new StringContent($"{{\"message\": \"{message}\", \"title\": \"{title}\"}}");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    var response = httpClient.SendAsync(request).Result;
                }
            }
        }

        static void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e)
        {
            ServiceRunning = !ServiceRunning;
            Console.Title = $"Service is: {(ServiceRunning ? "Running" : "Disabled")}";
            Console.WriteLine($"Service toggled! State: {ServiceRunning}");
        }

        #region ImageSearch
        [DllImport(@"Resources\ImageSearchDLL.dll")]
        public static extern IntPtr ImageSearch(int x, int y, int right, int bottom, [MarshalAs(UnmanagedType.LPStr)] string imagePath);

        public static string[] UseImageSearch(string imgPath, string tolerance)
        {
            imgPath = "*" + tolerance + " " + imgPath;

            IntPtr result = ImageSearch(0, 0, 1920, 1080, imgPath);
            string res = Marshal.PtrToStringAnsi(result);

            if (res[0] == '0') return null;

            string[] data = res.Split('|');

            int x; int y;
            int.TryParse(data[1], out x);
            int.TryParse(data[2], out y);

            return data;
        }
        #endregion

        #region MouseClick
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        //This simulates a left mouse click
        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }
        #endregion
    }


    public class ConfigJson
    {
        public bool hassnotify { get; set; }
        public string authkey { get; set; }
        public string ip { get; set; }
    }
}