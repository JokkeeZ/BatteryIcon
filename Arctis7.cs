using HidLibrary;

namespace HeadsetBatteryIcon
{
	class Arctis7
	{
		private HidDevice? hidDevice;

		private Arctis7() { }

		public static Arctis7? CreateDevice()
		{
			var device = new Arctis7
			{
				hidDevice = InitializeDevice()
			};

			return device.hidDevice != null ? device : null;
		}

		static HidDevice? InitializeDevice()
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
			hidDevice?.Write(new byte[2] { 0x06, 0x18 });
			var result = hidDevice?.Read();

			if (result != null)
			{
				return result.Data[2] > 100 ? 100 : result.Data[2];
			}	
		
			return 0;
		}
	}
}