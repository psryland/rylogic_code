using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using cAlgo.API;
using pr.extn;

namespace Tradee
{
	/// <summary>This bot streams market data to 'Tradee'</summary>
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class Tradee_StreamData : Robot
	{
		private TradeeBotUI m_ui;
		private Thread m_thread;

		protected override void OnStart()
		{
			try
			{
				// Create a thread and launch the TradeeBot UI in it
				m_thread = new Thread(Main) { Name = "TradeeBot" };
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

		/// <summary>Thread entry point</summary>
		[STAThread] private void Main()
		{
			try
			{
				Trace.WriteLine("TradeeBotUI Created");

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				using (m_ui = new TradeeBotUI(this))
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
