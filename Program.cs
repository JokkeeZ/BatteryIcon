using System.ComponentModel;
using System.Diagnostics;
using System.Text;

#nullable disable

namespace HeadsetBatteryIcon
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new ArctisBatteryApplicationContext());
		}
	}

	class ArctisBatteryApplicationContext : ApplicationContext
	{
		private readonly System.Threading.Timer timer;
		private readonly NotifyIcon icon;
		private readonly Arctis7 arctis;

		private int latestBatteryPercentage = -1;
		private DateTime latestTimestamp;

		private readonly List<Icon> icons = new()
		{
			new("icons/testi_0.ico", new(128, 128)),
			new("icons/testi_25.ico", new(128, 128)),
			new("icons/testi_50.ico", new(128, 128)),
			new("icons/testi_75.ico", new(128, 128)),
			new("icons/testi_100.ico", new(128, 128))
		};

		private readonly List<BatteryDischargeTime> dischargeTimes = new();

		public ArctisBatteryApplicationContext()
		{
			arctis = Arctis7.CreateDevice();
			if (arctis == null)
			{
				File.AppendAllText("asd.txt", $"Device not found");
				return;
			}

			latestTimestamp = DateTime.Now;

			var contextMenuStrip = new ContextMenuStrip(new Container());
			contextMenuStrip.Items.AddRange(new[]
			{
				new ToolStripMenuItem("Debug", null, OpenDebugPopup),
				new ToolStripMenuItem("Exit", null, Exit)
			});

			icon = new NotifyIcon()
			{
				Icon = icons[0],
				Visible = true,
				ContextMenuStrip = contextMenuStrip
			};

			timer = new(CheckBatteryPercentage, null, 0, 1000 * 5);
		}

		private void OpenDebugPopup(object sender, EventArgs e)
		{
			var sb = new StringBuilder();

			var batteryPercentage = arctis.GetBatteryPercentage();
			var estimatedDischargeTime = GetEstimatedDischargeTime();

			if (dischargeTimes.Count > 0)
			{
				sb.AppendLine("Discharge times: ");
				foreach (var time in dischargeTimes)
				{
					sb.AppendLine($"Timestamp: {time.Timestamp}, Interval: {time.Interval}");
				}

				sb.AppendLine("----------------------");
				sb.AppendLine($"Latest discharge: {dischargeTimes.OrderByDescending(x => x.Timestamp).First().Timestamp}");
				sb.AppendLine($"Avg time to lose 1%: {estimatedDischargeTime}");
			}
			sb.AppendLine();
			sb.AppendLine($"Current percentage: {batteryPercentage}%");

			if (dischargeTimes.Count > 0)
			{
				sb.AppendLine($"Estimated battery life left: {(estimatedDischargeTime == TimeSpan.Zero ? "Calculating" : estimatedDischargeTime * batteryPercentage)}");
			}	


			MessageBox.Show(sb.ToString(), "Debug stuff", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void Exit(object sender, EventArgs e)
		{
			icon.Visible = false;
			Application.Exit();
		}

		private void CheckBatteryPercentage(object state)
		{
			var currentTimestamp = DateTime.Now;
			var batteryPercentage = arctis.GetBatteryPercentage();

			// Headset disconnected, skip battery check.
			if (batteryPercentage == 0)
			{
				Debug.WriteLine($"Headset disconnected...");
				SetIcon(batteryPercentage);
				return;
			}

			// First check
			if (latestBatteryPercentage == -1)
			{
				latestBatteryPercentage = batteryPercentage;
				Debug.WriteLine($"First battery check done...");
			}

			// Battery has discharged 1%
			if (batteryPercentage < latestBatteryPercentage)
			{
				dischargeTimes.Add(new()
				{
					Interval = currentTimestamp - latestTimestamp,
					Timestamp = currentTimestamp,
				});
				
				Debug.WriteLine($"From {latestBatteryPercentage}% to {batteryPercentage}% it took {currentTimestamp - latestTimestamp}");
				File.AppendAllText("asd.txt", $"From {latestBatteryPercentage}% to {batteryPercentage}% it took {currentTimestamp - latestTimestamp}");
				
				// Set current time as latest one
				latestTimestamp = currentTimestamp;
				latestBatteryPercentage = batteryPercentage;
			}

			Debug.WriteLine($"5 seconds passed... {batteryPercentage}% left");
			SetIcon(batteryPercentage);
		}

		private void SetIcon(int batteryPercentage)
		{
			icon.Icon = GetIconForPercentage(batteryPercentage);

			if (batteryPercentage == 0)
			{
				icon.Text = "Headset disconnected";
				return;
			}

			var estimatedDischargeTime = GetEstimatedDischargeTime() * batteryPercentage;

			if (estimatedDischargeTime == TimeSpan.Zero)
			{
				icon.Text = $"Battery left: {batteryPercentage}%\r\n" +
					$"Estimate left: Calculating...";
				return;
			}

			icon.Text = $"Battery left: {batteryPercentage}%\r\n" +
				$"Estimate left: {estimatedDischargeTime.Days}d {estimatedDischargeTime.Hours}h {estimatedDischargeTime.Minutes}m";
		}

		private TimeSpan GetEstimatedDischargeTime()
		{
			var latestPercentages = dischargeTimes.OrderByDescending(x => x.Timestamp).Take(5);

			if (!latestPercentages.Any())
			{
				return TimeSpan.Zero;
			}

			var averageTime = new TimeSpan(Convert.ToInt64(latestPercentages.Average(x => x.Interval.Ticks)));
			return averageTime;
		}

		private Icon GetIconForPercentage(int percentage)
		{
			return percentage switch
			{
				> 0 and <= 25 => icons[1],
				> 25 and <= 50 => icons[2],
				> 50 and <= 75 => icons[3],
				> 75 and <= 100 => icons[4],
				_ => icons[0]
			};
		}
	}

	class BatteryDischargeTime
	{
		public TimeSpan Interval { get; set; }
		public DateTime Timestamp { get; set; }
	}
}