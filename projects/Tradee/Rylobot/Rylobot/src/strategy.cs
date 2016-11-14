using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Indicators;
using pr.common;
using pr.extn;
using pr.maths;
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
			Instrument = new Instrument(bot);
			Label = label;
			Suitability = new List<Vec2d>();
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
					m_bot.Positions.Opened -= HandlePositionOpened;
				}
				m_bot = value;
				if (m_bot != null)
				{
					m_bot.Positions.Opened += HandlePositionOpened;
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

		/// <summary>How well this strategy applies to the data</summary>
		public virtual double SuitabilityScore
		{
			get { return 0.0; }
		}
		private List<Vec2d> Suitability;

		/// <summary>
		/// Step the strategy.
		/// Note: Instruments are signed up to Bot.Tick, they will be updated before Strategy.Step() is called.</summary>
		public virtual void Step()
		{
			if (Instrument.NewCandle)
			{
				// Track suitability
				var x = Instrument.FractionalIndexAt(Bot.UtcNow) - (double)Instrument.FirstIdx;
				Suitability.Add(new Vec2d(x, SuitabilityScore));
			}
		}

		/// <summary>Called just before the bot stops</summary>
		protected virtual void OnBotStopping()
		{
			{// Output instrument
				var ldr = new pr.ldr.LdrBuilder();
				using (ldr.Group(string.Empty, Debugging.ScaleTxfm))
					Debugging.Dump(Instrument, emas:new[] {100,55}, ldr_:ldr);
				ldr.ToFile(Debugging.FP("{0}_{1}.ldr".Fmt(Label, Instrument.SymbolCode)));
			}
			{// Output suitability
				var range = RangeF.Invalid;
				foreach (var c in Instrument.CandleRange())
				{
					range.Encompass(c.Low);
					range.Encompass(c.High);
				}

				var ldr = new pr.ldr.LdrBuilder();
				using (ldr.Group(string.Empty, Debugging.ScaleTxfm))
				{
					var suitability = Suitability.Select(x => new v4((float)x.x, (float)(x.y * range.Size + range.Begin), 0.05f, 1f));
					ldr.Line("suitability", 0xFF8000FF, 1, suitability);
					ldr.Line("suitability_0", 0xFF8000A0, new v4(0f, (float)range.Begin, 0.05f, 1f), new v4(Instrument.Count, (float)range.Begin, 0.05f, 1f));
					ldr.Line("suitability_v", 0xFF8020F0, new v4(0f, (float)range.Mid  , 0.05f, 1f), new v4(Instrument.Count, (float)range.Mid  , 0.05f, 1f));
					ldr.Line("suitability_1", 0xFF8000A0, new v4(0f, (float)range.End  , 0.05f, 1f), new v4(Instrument.Count, (float)range.End  , 0.05f, 1f));
				}
				ldr.ToFile(Debugging.FP("{0}_suitability.ldr".Fmt(Label)));
			}
		}

		/// <summary>Return the position associated with 'id' or null</summary>
		protected Position FindLivePosition(int id)
		{
			return Bot.Positions.FirstOrDefault(x => x.Id == id);
		}
		protected Position FindLivePosition(string label)
		{
			return Bot.Positions.FirstOrDefault(x => x.Label == label);
		}
		protected Position FindLivePosition(Position pos)
		{
			if (pos == null) return null;
			return FindLivePosition(pos.Id);
		}

		/// <summary>Called when a position is opened</summary>
		private void HandlePositionOpened(PositionOpenedEventArgs args)
		{
			Debugging.LogTrade(args.Position);

			// Notify position closed
			OnPositionOpened(args.Position);
		}

		/// <summary>Called when a position is opened</summary>
		protected virtual void OnPositionOpened(Position position)
		{}

		/// <summary>Called when a position closes</summary>
		private void HandlePositionClosed(PositionClosedEventArgs args)
		{
			Debugging.LogTrade(args.Position);

			// Notify position closed
			OnPositionClosed(args.Position);
		}

		/// <summary>Called when the current position closes</summary>
		protected virtual void OnPositionClosed(Position position)
		{}

		/// <summary>Called just before the bot stops</summary>
		private void HandleBotStopping(object sender, EventArgs e)
		{
			// Notify stopping
			OnBotStopping();
		}
	}
}
