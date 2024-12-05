using HidLibrary;

namespace HeadsetBatteryIcon;

class Arctis7
{
	private readonly HidDevice hidDevice;

	public Arctis7()
	{
		hidDevice = InitializeDevice();
	}

	private static HidDevice InitializeDevice()
	{
		foreach (var device in HidDevices.Enumerate(4152, 0x12ad))
		{
			if (device.Capabilities.Usage > 1)
			{
				return device;
			}
		}

		return null;
	}

	public int GetBatteryPercentage()
	{
		hidDevice?.Write([0x06, 0x18]);
		var result = hidDevice?.Read();

		if (result != null)
		{
			// 2nd byte contains the battery charge.
			return result.Data[2] > 100 ? 100 : result.Data[2];
		}

		return 0;
	}
}