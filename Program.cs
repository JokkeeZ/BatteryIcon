using System.ComponentModel;
using System.Diagnostics;

namespace HeadsetBatteryIcon;

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
	private readonly NotifyIcon icon;
	private readonly Arctis7 headset;

	private readonly List<Icon> icons =
	[
		new("icons/testi_0.ico", new(128, 128)),
		new("icons/testi_25.ico", new(128, 128)),
		new("icons/testi_50.ico", new(128, 128)),
		new("icons/testi_75.ico", new(128, 128)),
		new("icons/testi_100.ico", new(128, 128))
	];

	public ArctisBatteryApplicationContext()
	{
		headset = new();

		if (headset == null)
		{
			MessageBox.Show("Error", "Arctis 7 couldn't be detected.");
			return;
		}

		var contextMenuStrip = new ContextMenuStrip(new Container());
		contextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) =>
		{
			icon.Visible = false;
			Application.Exit();
		}));

		icon = new NotifyIcon()
		{
			Icon = icons[0],
			Visible = true,
			ContextMenuStrip = contextMenuStrip
		};

		_ = new System.Threading.Timer(CheckBatteryPercentage, null, 0, 5000);
	}

	private void CheckBatteryPercentage(object state)
	{
		var batteryPercentage = headset.GetBatteryPercentage();

		// Headset disconnected, skip battery check.
		if (batteryPercentage == 0)
		{
			Debug.WriteLine($"Headset disconnected...");
			SetIcon(batteryPercentage);
			return;
		}

		SetIcon(batteryPercentage);
	}

	private void SetIcon(int batteryPercentage)
	{
		icon.Icon = batteryPercentage switch
		{
			> 0 and <= 25 => icons[1],
			> 25 and <= 50 => icons[2],
			> 50 and <= 75 => icons[3],
			> 75 and <= 100 => icons[4],
			_ => icons[0]
		};

		if (batteryPercentage == 0)
		{
			icon.Text = "Headset disconnected";
			return;
		}

		icon.Text = $"Headset battery remaining: {batteryPercentage}%";
	}
}