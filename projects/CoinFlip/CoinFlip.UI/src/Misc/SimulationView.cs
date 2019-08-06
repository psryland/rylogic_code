using System;
using System.ComponentModel;
using Rylogic.Common;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI
{
	public class SimulationView :IDisposable, INotifyPropertyChanged
	{
		public SimulationView(Model model)
		{
			Model = model;

			Reset = Command.Create(this, ResetInternal);
			StepOne = Command.Create(this, StepOneInternal);
			RunToTrade = Command.Create(this, RunToTradeInternal);
			Run = Command.Create(this, RunInternal);
			Pause = Command.Create(this, PauseInternal);
			Toggle = Command.Create(this, ToggleInternal);
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
