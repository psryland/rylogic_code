using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace FarPointer
{
	public class Settings
	{
		private string m_client_name = "unknown";
		private readonly List<Host> m_hosts = new List<Host>();

		public string ClientName { get { return m_client_name; } set { m_client_name = value; } }
		public List<Host> Hosts { get { return m_hosts; } }

		// Load/Save
		public static string GetFilepath()
		{
			return Path.Combine(Application.CommonAppDataPath, @"settings.xml");
		}
		public void Save()
		{
			using (StreamWriter sw = new StreamWriter(GetFilepath()))
				new XmlSerializer(typeof(Settings)).Serialize(sw, this);
		}
		public static Settings Load()
		{
			try
			{
				using (StreamReader sr = new StreamReader(GetFilepath()))
					return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(sr);
			}
			catch (IOException) { return new Settings(); }
		}
	}
}
