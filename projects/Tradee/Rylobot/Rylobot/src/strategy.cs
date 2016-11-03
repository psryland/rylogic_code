using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Indicators;
using pr.util;

namespace Rylobot
{
	public abstract class Strategy :IDisposable
	{
		// Notes:
		// - One trade per strategy.
		// - Strategies should be resume-able, i.e on start they should look for
		//   existing positions associated with the strategy and continue using them.
		//   This is so startup/shutdown have very little effect on the running of the bot.

		public Strategy(Rylobot bot, string label)
		{
			Bot = bot;
			Instrument = new Instrument(bot, bot.Symbol.Code);
			Label = label;

			// Look for any existing trade created by this strategy
			Position = Bot.Positions.FirstOrDefault(x => x.Label == Label);
		}
		public virtual void Dispose()
		{
			Instrument = null;
			Bot = null;
		}

		/// <summary>A label for positions created by this strategy</summary>
		public string Label
		{
			get;
			private set;
		}

		/// <summary>Application logic</summary>
		public Rylobot Bot
		{
			[DebuggerStepThrough] get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{
					m_bot.Stopping -= HandleBotStopping;
					m_bot.Positions.Closed -= HandlePositionClosed;
				}
				m_bot = value;
				if (m_bot != null)
				{
					m_bot.Positions.Closed += HandlePositionClosed;
					m_bot.Stopping += HandleBotStopping;
				}
			}
		}
		private Rylobot m_bot;

		/// <summary>The main instrument for this bot</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instr; }
			private set
			{
				if (m_instr == value) return;
				Util.Dispose(ref m_instr);
				m_instr = value;
			}
		}
		private Instrument m_instr;

		/// <summary>The position managed by this strategy</summary>
		public Position Position
		{
			get { return m_position; }
			protected set
			{
				if (m_position == value) return;
				if (m_position != null)
				{
				}
				m_position = value;
				if (m_position != null)
				{
				}
			}
		}
		private Position m_position;

		/// <summary>Step the strategy. Note: Instruments signed up to Bot.Tick while already be updated</summary>
		public abstract void Step();

		/// <summary>Called when the current position closes</summary>
		protected virtual void OnPositionClosed(Position position)
		{}

		/// <summary>Called just before the bot stops</summary>
		protected virtual void OnBotStopping()
		{
		}

		/// <summary>Called when a position closes</summary>
		private void HandlePositionClosed(PositionClosedEventArgs args)
		{
			// If not the position owned by this strategy, then ignore
			if (Position == null || args.Position.Id != Position.Id)
				return;

			// Save a reference to the outgoing position and set the current position to null
			var position = Position;
			Position = null;

			// Notify position closed
			OnPositionClosed(position);
		}

		/// <summary>Called just before the bot stops</summary>
		private void HandleBotStopping(object sender, EventArgs e)
		{
			// Notify stopping
			OnBotStopping();
		}
	}
}
