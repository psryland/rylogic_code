using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Text;
using pr.extn;

namespace cAlgo
{
	/// <summary>Represents the 'Tradee' application. The bot uses this to communicate with Tradee</summary>
	public class TradeeProxy :IDisposable
	{
		/// <summary>Binary serialiser</summary>
		private BinaryFormatter m_bf;
		
		public TradeeProxy()
		{

		}
		public void Dispose()
		{

		}

		/// <summary>Send a message over the pipe</summary>
		public void Post<T>(T msg)
		{
			try
			{
				using (var pipe = new NamedPipeClientStream(".", "TradeePipeIn", PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
				{
					pipe.Connect(10);
					m_bf.Serialize(pipe, msg);
					pipe.WaitForPipeDrain();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.MessageFull());
			}
		}
	}
}
