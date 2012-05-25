namespace FarPointer
{
	public enum EMsg :byte
	{
		None       = 0,
		Name       = 1,
		MouseDown  = 2,
		MouseUp    = 3,
		MouseMove  = 4,
		MouseClick = 5,
		MouseWheel = 6,
		KeyDown    = 7,
		KeyUp      = 8,
		KeyPress   = 9
	}

	public struct Host
	{
		public const int DefaultPort = 0x1337;
		public string m_hostname;
		public int m_port;
		public Host(string hostname, int port) { m_hostname = hostname; m_port = port; }
	}

	public delegate void VoidFunc();
}
