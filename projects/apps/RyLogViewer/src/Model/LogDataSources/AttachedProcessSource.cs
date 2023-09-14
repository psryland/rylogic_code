using System;
using System.Diagnostics;
using System.IO;
using RyLogViewer.Options;

namespace RyLogViewer
{
	public class AttachedProcessSource : ILogDataSource
	{
		private readonly Process m_process;
		private readonly Settings m_settings;
		public AttachedProcessSource(Process proc, Settings settings)
		{
			m_process = proc;
			m_settings = settings;
			proc.OutputDataReceived += new DataReceivedEventHandler((s, e) => Console.WriteLine(e.Data));
			proc.EnableRaisingEvents = true;
		}
		public void Dispose()
		{}

		/// <inheritdoc />
		public string Path => m_process.ProcessName;

		/// <inheritdoc />
		public Stream OpenStream()
		{
			return m_process.StandardOutput.BaseStream;
		}
	}
}
