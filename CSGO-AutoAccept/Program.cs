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
using System.Net;
using System.Drawing.Imaging;

namespace CSGOAutoAccept
{
	class Program
	{
		static bool ServiceRunning = false;
		static ConfigJson config;

		static void Main(string[] args)
		{
			Console.Title = $"Service is: {(ServiceRunning ? "Running" : "Disabled")}";
			if (File.Exists("config.json"))
			{
				ConfigJson cj = JsonConvert.DeserializeObject<ConfigJson>(File.ReadAllText("config.json"));
				config = cj;
				if (config.testmsg)
				{
					SendNotification("CSGO AUTO CONNECT", "Test message!");
				}
			}

			AutoConnectChecker();

			if (config.hotkey != null)
			{
				HotKeyManager.RegisterHotKey((Keys)Enum.Parse(typeof(Keys), config.hotkey.key), (KeyModifiers)Enum.Parse(typeof(KeyModifiers), config.hotkey.modifiers));
				HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_HotKeyPressed);
			}
			else
			{
				HotKeyManager.RegisterHotKey(Keys.PageUp, KeyModifiers.Control);
				HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_HotKeyPressed);
			}
			Console.ReadLine();
		}

		public static async void AutoConnectChecker()
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
							string map = (config.ocr != null && config.ocr.enabled) ? await GetCurrentMap() : "Function Disabled!";
							LeftMouseClick(x, y);
							if ((config.telegram != null && config.telegram.enabled) || (config.hass != null && config.hass.enabled))
							{
								SendNotification("CSGO AUTO CONNECT", "Match found! Map: " + map);
							}
						}
						else if (data2 != null && int.TryParse(data2[1], out int x2) && int.TryParse(data2[2], out int y2))
						{
							Console.WriteLine($"IMG2: {x2} {y2}");
							string map = (config.ocr != null && config.ocr.enabled) ? await GetCurrentMap() : "Function Disabled!";
							LeftMouseClick(x2, y2);
							if ((config.telegram != null && config.telegram.enabled) || (config.hass != null && config.hass.enabled))
							{
								SendNotification("CSGO AUTO CONNECT", "Match found! Map: " + map);
							}
						}
					}
					catch { }
				}
				await Task.Delay(2500);
			}
		}

		static async Task<string> GetCurrentMap()
		{
			HttpClient httpClient = new HttpClient();
			httpClient.Timeout = new TimeSpan(1, 1, 1);


			MultipartFormDataContent form = new MultipartFormDataContent();
			form.Add(new StringContent(config.ocr.apikey), "apikey");
			form.Add(new StringContent("eng"), "language");

			form.Add(new StringContent("1"), "ocrengine");
			form.Add(new StringContent("true"), "scale");
			form.Add(new StringContent("true"), "istable");

			Bitmap screen = new Bitmap(1920, 1080);
			var size = new Size(Screen.PrimaryScreen.Bounds.Size.Width,
				  Screen.PrimaryScreen.Bounds.Size.Height);
			Graphics.FromImage(screen).CopyFromScreen(0, 0, 0, 0, size);

			Bitmap target = new Bitmap(295, 30);
			using (Graphics g = Graphics.FromImage(target))
			{
				g.DrawImage(screen, new Rectangle(0, 0, target.Width, target.Height),
								 new Rectangle() { X = 806, Width = 295, Y = 502, Height = 30 },
								 GraphicsUnit.Pixel);
			}
			MemoryStream ms = new MemoryStream();
			target.Save(ms, ImageFormat.Bmp);

			byte[] imageData = ms.GetBuffer();
			form.Add(new ByteArrayContent(imageData, 0, imageData.Length), "image", "image.png");

			HttpResponseMessage response = await httpClient.PostAsync("https://api.ocr.space/Parse/Image", form);

			string strContent = await response.Content.ReadAsStringAsync();
			OcrJson ocrResult = JsonConvert.DeserializeObject<OcrJson>(strContent);
			string output = "";

			if (ocrResult.OCRExitCode == 1)
			{
				for (int i = 0; i < ocrResult.ParsedResults.Count(); i++)
				{
					output += ocrResult.ParsedResults[i].ParsedText;
				}
			}
			return output;
		}

		static void SendNotification(string title, string message)
		{
			if (config.telegram != null && config.telegram.enabled)
			{
				new WebClient().DownloadString($"https://api.telegram.org/bot{config.telegram.bottoken}/sendMessage?chat_id={config.telegram.chatid}&text={message}");
			}
			if (config.hass != null && config.hass.enabled)
			{
				using (var httpClient = new HttpClient())
				{
					using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"{config.hass.ip}/api/services/notify/{config.hass.notifyservice}"))
					{
						request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {config.hass.authkey}");

						request.Content = new StringContent($"{{\"message\": \"{message}\", \"title\": \"{title}\"}}");
						request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

						var response = httpClient.SendAsync(request).Result;
					}
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

	#region ConfigJson
	public class ConfigJson
	{
		public bool testmsg { get; set; }
		public Hass hass { get; set; }
		public Telegram telegram { get; set; }
		public Ocr ocr { get; set; }
		public Hotkey hotkey { get; set; }
	}

	public class Hass
	{
		public bool enabled { get; set; }
		public string authkey { get; set; }
		public string ip { get; set; }
		public string notifyservice { get; set; }
	}

	public class Telegram
	{
		public bool enabled { get; set; }
		public string bottoken { get; set; }
		public string chatid { get; set; }
	}

	public class Ocr
	{
		public bool enabled { get; set; }
		public string apikey { get; set; }
	}

	public class Hotkey
	{
		public string key { get; set; }
		public string modifiers { get; set; }
	}
	#endregion

	#region OcrJson
	public class OcrJson
	{
		public Parsedresult[] ParsedResults { get; set; }
		public int OCRExitCode { get; set; }
		public bool IsErroredOnProcessing { get; set; }
		public string ProcessingTimeInMilliseconds { get; set; }
		public string SearchablePDFURL { get; set; }
	}

	public class Parsedresult
	{
		public Textoverlay TextOverlay { get; set; }
		public string TextOrientation { get; set; }
		public int FileParseExitCode { get; set; }
		public string ParsedText { get; set; }
		public string ErrorMessage { get; set; }
		public string ErrorDetails { get; set; }
	}

	public class Textoverlay
	{
		public Line[] Lines { get; set; }
		public bool HasOverlay { get; set; }
		public string Message { get; set; }
	}

	public class Line
	{
		public string LineText { get; set; }
		public Word[] Words { get; set; }
		public float MaxHeight { get; set; }
		public float MinTop { get; set; }
	}

	public class Word
	{
		public string WordText { get; set; }
		public float Left { get; set; }
		public float Top { get; set; }
		public float Height { get; set; }
		public float Width { get; set; }
	}
	#endregion
}