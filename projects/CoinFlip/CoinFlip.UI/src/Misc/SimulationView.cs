using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CoinFlip.UI.Dialogs;
using Rylogic.Common;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI
{
	public class SimulationView :IDisposable, INotifyPropertyChanged
	{
		public SimulationView(Window owner, Model model)
		{
			Owner = owner;
			Model = model;

			Reset = Command.Create(this, ResetInternal);
			StepOne = Command.Create(this, StepOneInternal);
			RunToTrade = Command.Create(this, RunToTradeInternal);
			Run = Command.Create(this, RunInternal);
			Pause = Command.Create(this, PauseInternal);
			Toggle = Command.Create(this, ToggleInternal);
			ShowBackTestingOptions = Command.Create(this, ShowBackTestingOptionsInternal);
		}
		public void Dispose()
		{
			Model = null;
		}

		/// <summary>App logic</summary>
		private Model Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					Model.BackTestingChange -= HandleBackTestingChange;
					m_model.SimPropertyChanged -= HandleSimEvent;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.SimPropertyChanged += HandleSimEvent;
					Model.BackTestingChange += HandleBackTestingChange;
				}

				// Handler
				void HandleSimEvent(object sender, EventArgs e)
				{
					NotifyPropertyChanged(nameof(Running));
					NotifyPropertyChanged(nameof(CanReset));
					NotifyPropertyChanged(nameof(CanStepOne));
					NotifyPropertyChanged(nameof(CanRunToTrade));
					NotifyPropertyChanged(nameof(CanRun));
					NotifyPropertyChanged(nameof(CanPause));
					NotifyPropertyChanged(nameof(PercentComplete));
					NotifyPropertyChanged(nameof(Clock));
					NotifyPropertyChanged(nameof(StepsPerCandle));
					NotifyPropertyChanged(nameof(StartTime));
					NotifyPropertyChanged(nameof(EndTime));
				}
				void HandleBackTestingChange(object sender, PrePostEventArgs e)
				{
					if (e.Before) return;
					NotifyPropertyChanged(nameof(Active));
					NotifyPropertyChanged(string.Empty);
				}
			}
		}
		private Model m_model;

		/// <summary>The owner window</summary>
		private Window Owner { get; }

		/// <summary>The simulation (or null)</summary>
		private Simulation Sim => Model.Simulation;

		/// <summary>True when the simulation is active</summary>
		public bool Active => Sim != null;

		/// <summary>True when the simulation is advancing</summary>
		public bool Running => Sim?.Running ?? false;

		/// <summary>Get/Set the simulation time frame</summary>
		public ETimeFrame TimeFrame
		{
			get { return Sim?.TimeFrame ?? ETimeFrame.None; }
			set { if (Sim != null) Sim.TimeFrame = value; }
		}

		/// <summary>Sub-candle simulated resolution</summary>
		public int StepsPerCandle
		{
			get => Sim?.StepsPerCandle ?? 1;
			set
			{
				if (Sim == null) return;
				Sim.StepsPerCandle = value;
			}
		}

		/// <summary>Simulation start time</summary>
		public DateTimeOffset? StartTime
		{
			get => Sim?.StartTime;
			set
			{
				if (Sim == null || value == null) return;
				Sim.StartTime = value.Value;
			}
		}

		/// <summary>Simulation end time</summary>
		public DateTimeOffset EndTime
		{
			get => Sim?.EndTime ?? Misc.CryptoCurrencyEpoch;
			set
			{
				if (Sim == null) return;
				Sim.EndTime = value;
			}
		}

		/// <summary>The limits for the simulation time range</summary>
		public DateTimeOffset MinSimTime => Misc.CryptoCurrencyEpoch;
		public DateTimeOffset MaxSimTime => DateTimeOffset.UtcNow;

		/// <summary></summary>
		public bool CanReset => Sim?.CanReset ?? false;

		/// <summary></summary>
		public bool CanStepOne => Sim?.CanStepOne ?? false;

		/// <summary></summary>
		public bool CanRunToTrade => Sim?.CanRunToTrade ?? false;

		/// <summary></summary>
		public bool CanRun => Sim?.CanRun ?? false;

		/// <summary></summary>
		public bool CanPause => Sim?.CanPause ?? false;

		/// <summary>Reset the simulation</summary>
		public Command Reset { get; }
		private void ResetInternal()
		{
			Sim?.Reset();
		}

		/// <summary>Advance the simulation by one step</summary>
		public Command StepOne { get; }
		private void StepOneInternal()
		{
			Sim?.StepOne();
		}

		/// <summary>Advance the simulation to the next trade</summary>
		public Command RunToTrade { get; }
		private void RunToTradeInternal()
		{
			Sim?.RunToTrade();
		}

		/// <summary>Run the simulation</summary>
		public Command Run { get; }
		private void RunInternal()
		{
			Sim?.Run();
		}

		/// <summary>Pause the simulation</summary>
		public Command Pause { get; }
		private void PauseInternal()
		{
			Sim?.Pause();
		}

		/// <summary>Toggle between run and pause</summary>
		public Command Toggle { get; }
		private void ToggleInternal()
		{
			if (Running == false)
				Run.Execute();
			else
				Pause.Execute();
		}

		/// <summary>Show the dialog containing the back testing options</summary>
		public Command ShowBackTestingOptions { get; }
		private void ShowBackTestingOptionsInternal()
		{
			// Toggle the dialog visibility
			if (m_back_testing_options_ui == null)
			{
				var pt = Owner.PointToScreen(Mouse.GetPosition(Owner));
				m_back_testing_options_ui = new BackTestingOptionsUI(Owner, this);
				m_back_testing_options_ui.Closed += delegate { m_back_testing_options_ui = null; };
				m_back_testing_options_ui.Show();
				m_back_testing_options_ui.SetLocation(pt.X, pt.Y + 20).OnScreen();
			}
			else
			{
				m_back_testing_options_ui.Close();
			}
		}
		private BackTestingOptionsUI m_back_testing_options_ui;

		/// <summary>Factional time through the simulation period</summary>
		public double PercentComplete
		{
			get { return Sim?.PercentComplete ?? 0.0; }
			set { if (Sim != null) Sim.PercentComplete = value; }
		}

		/// <summary>Simulation time</summary>
		public DateTimeOffset Clock => Model.SimClock;

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
