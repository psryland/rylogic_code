using System;
using RyLogViewer;

namespace RexBionics
{
	[CustomDataSource]
	public class RexLog :ICustomLogDataSource
	{
		private string m_log_entry_types_file;

		public LogDataSourceRunData ShowConfigUI(LogDataSourceConfig config)
		{
			var dg = new RexLogUI();
			dg.
			return new LogDataSourceRunData();
		}
		public void Dispose()
		{}

		public string ShortName { get { return "Rex Log"; } }

		public string MenuText { get { return "Rex Log"; } }

		public bool IsConnected { get { return true; } }

		public void Start()
		{
			throw new NotImplementedException();
		}

		public IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
		}

		public int EndRead(IAsyncResult async_result)
		{
		}
	}

	//[DataContract]
	//internal class RexLogSettings
	//{
	//	[DataMember]
	//}
}
