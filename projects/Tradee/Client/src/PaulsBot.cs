using System;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Text;
using System.Xml.Linq;
using cAlgo.API;
using PaulsBot;
using pr.extn;
using pr.util;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PaulsBot : Robot
    {
        #region Robot Implementation
        protected override void OnStart()
        {
            Pipe = new NamedPipeClientStream(".", "PaulsBotPipe", PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);
        }
        protected override void OnTick()
        {
            UpdateRisk();
        }
        protected override void OnStop()
        {
            Pipe = null;
        }
        #endregion

        #region Pipe

        /// <summary>IPC from cAlgo bots</summary>
        public NamedPipeClientStream Pipe
        {
            get
            {
                if (m_pipe != null && !m_pipe.IsConnected)
                {
                    try
                    {
                        m_pipe.Connect(100);
                    } catch (Exception)
                    {
                    }
                    Print(m_pipe.IsConnected ? "Connected to PaulsBotPipe" : "Failed to connect to PaulsBotPipe");
                }
                return m_pipe;
            }
            private set
            {
                if (m_pipe == value)
                    return;
                if (m_pipe != null)
                    Util.Dispose(ref m_pipe);
                m_pipe = value;
            }
        }
        private NamedPipeClientStream m_pipe;

        /// <summary>Send a message over the pipe</summary>
        private void Post<T>(T msg) where T : IMsg
        {
            if (Pipe == null || !Pipe.IsConnected)
                return;

			var bf = new BinaryFormatter();
			bf.Serialize(Pipe, msg);
            //var xml = msg.ToXml(new XElement("msg"), type_attr: true);
            //var data = Encoding.UTF8.GetBytes(xml.ToString(SaveOptions.DisableFormatting));
            //Pipe.Write(data, 0, data.Length);
        }

        #endregion

        /// <summary>Update total risk</summary>
        private void UpdateRisk()
        {
            var risk = new TotalRisk();
            risk.CurrencySymbol = Account.Currency;

            // Add up all the potential losses from current positions
            foreach (var position in Positions)
            {
                var symbol = MarketData.GetSymbol(position.SymbolCode);
                if (position.StopLoss == null)
                    continue;

                var pips = (position.EntryPrice - position.StopLoss.Value) * position.Volume;
                risk.PositionRisk += pips * symbol.PipValue / symbol.PipSize;
            }

            // Add up all potential losses from orders
            foreach (var order in PendingOrders)
            {
                var symbol = MarketData.GetSymbol(order.SymbolCode);
                if (order.StopLoss == null)
                    continue;

                var pips = order.TradeType == TradeType.Buy ? (order.TargetPrice - order.StopLoss.Value) * order.Volume : (order.StopLoss.Value - order.TargetPrice) * order.Volume;
                risk.OrderRisk += pips * symbol.PipValue / symbol.PipSize;
            }

            Post(risk);
        }
    }
}
