using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using cAlgo.API;
using pr.extn;

namespace Rylobot
{
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class Rylobot :Robot
	{
		private RylobotUI m_ui;
		private Thread m_thread;

		protected override void OnStart()
		{
			try
			{
				// Create a thread and launch the Rylobot UI in it
				m_thread = new Thread(Main) { Name = "Rylobot" };
				m_thread.SetApartmentState(ApartmentState.STA);
				m_thread.Start();
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				m_thread = null;
				Stop();
			}
		}
		protected override void OnStop()
		{
			base.OnStop();
			try
			{
				if (m_ui != null && !m_ui.IsDisposed && m_ui.IsHandleCreated)
					m_ui.BeginInvoke(() => m_ui.Close());
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
			}
		}
		protected override void OnTick()
		{
			m_ui.BeginInvoke(() => m_ui.OnTick());
		}

		/// <summary>Thread entry point</summary>
		[STAThread] private void Main()
		{
			try
			{
				Trace.WriteLine("RylobotUI Created");

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				using (m_ui = new RylobotUI(this))
					Application.Run(m_ui);

				Stop();
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
			}
		}
	}
}
