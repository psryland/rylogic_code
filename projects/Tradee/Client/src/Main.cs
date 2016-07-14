using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Threading;
using pr.extn;
using pr.gui;
using pr.util;
using Tradee;

namespace TradeeClient
{
	public class TradeeClient :IDisposable
	{
		static void Main(string[] args)
		{
			using (var app = new TradeeClient())
				app.Run();
		}

		private BinaryFormatter m_bf;
		public TradeeClient()
		{
			m_bf = new BinaryFormatter();
		}
		public virtual void Dispose()
		{
		}
		public void Run()
		{
			for (;;)
			{
				UpdateRisk();
				Thread.Sleep(1000);
			}
		}

		/// <summary>Send a message over the pipe</summary>
		private void Post<T>(T msg)
		{
			try
			{
				using (var pipe = new NamedPipeClientStream(".", "TradeePipeIn", PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
				{
					pipe.Connect(100);
					m_bf.Serialize(pipe, msg);
					pipe.WaitForPipeDrain();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.MessageFull());
			}
		}

		/// <summary>Update total risk</summary>
		private void UpdateRisk()
		{
			Post(new HelloMsg { Msg = "Hello From Client2" });
			//var risk = new TotalRisk();
			//risk.CurrencySymbol = Account.Currency;

			//// Add up all the potential losses from current positions
			//foreach (var position in Positions)
			//{
			//	var symbol = MarketData.GetSymbol(position.SymbolCode);
			//	if (position.StopLoss == null)
			//		continue;

			//	var pips = (position.EntryPrice - position.StopLoss.Value) * position.Volume;
			//	risk.PositionRisk += pips * symbol.PipValue / symbol.PipSize;
			//}

			//// Add up all potential losses from orders
			//foreach (var order in PendingOrders)
			//{
			//	var symbol = MarketData.GetSymbol(order.SymbolCode);
			//	if (order.StopLoss == null)
			//		continue;

			//	var pips = order.TradeType == TradeType.Buy ? (order.TargetPrice - order.StopLoss.Value) * order.Volume : (order.StopLoss.Value - order.TargetPrice) * order.Volume;
			//	risk.OrderRisk += pips * symbol.PipValue / symbol.PipSize;
			//}

			//Post(risk);
		}
	}
}
