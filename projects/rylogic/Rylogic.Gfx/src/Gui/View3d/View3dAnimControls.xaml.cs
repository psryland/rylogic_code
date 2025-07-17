using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Rylogic.Gfx;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class View3dAnimControls :Grid, INotifyPropertyChanged
	{
		public View3dAnimControls()
		{
			InitializeComponent();
			Reset = Command.Create(this, ResetInternal);
			Play = Command.Create(this, PlayInternal);
			Pause = Command.Create(this, PauseInternal);
			StepBack = Command.Create(this, StepBackInternal);
			StepForward = Command.Create(this, StepForwardInternal);
			DataContext = this;
		}

		/// <summary>The window in which to control animations</summary>
		public View3d.Window? ViewWindow
		{
			get => (View3d.Window)GetValue(ViewWindowProperty);
			set => SetValue(ViewWindowProperty, value);
		}
		private void ViewWindow_Changed(View3d.Window? new_value, View3d.Window? old_value)
		{
			if (old_value != null)
			{
				old_value.OnAnimationEvent -= HandleAnimationEvent;
			}
			if (new_value != null)
			{
				new_value.OnAnimationEvent += HandleAnimationEvent;
			}
			NotifyPropertyChanged(nameof(ViewWindow));

			// Handlers
			void HandleAnimationEvent(object? sender, View3d.AnimationEventArgs e)
			{
				Util.BreakIf(!Util.IsMainThread);
				NotifyPropertyChanged(nameof(Animating));
				NotifyPropertyChanged(nameof(AnimClock));
				NotifyPropertyChanged(nameof(Frame));
			}
		}
		public static readonly DependencyProperty ViewWindowProperty = Gui_.DPRegister<View3dAnimControls>(nameof(ViewWindow), null, Gui_.EDPFlags.None);

		/// <summary>Animation time</summary>
		public double AnimClock
		{
			get => ViewWindow?.AnimTime ?? 0.0;
			set
			{
				if (AnimClock == value || ViewWindow == null) return;
				ViewWindow.AnimTime = value;
				ViewWindow.AnimControl(View3d.EAnimCommand.Step);
				NotifyPropertyChanged(nameof(AnimClock));
				NotifyPropertyChanged(nameof(Frame));
			}
		}

		/// <summary>The size of each step per second. Set to 0 for real time</summary>
		public double StepSize
		{
			get => m_step_size;
			set
			{
				if (StepSize == value) return;
				m_step_size = value;
				if (Animating) Play.Execute();
				NotifyPropertyChanged(nameof(StepSize));
			}
		}
		private double m_step_size;

		/// <summary>The assumed frame rate of all animation</summary>
		public double FrameRate
		{
			get => m_frame_rate;
			set
			{
				if (FrameRate == value) return;
				m_frame_rate = value;
				NotifyPropertyChanged(nameof(FrameRate));
				NotifyPropertyChanged(nameof(Frame));
			}
		}
		private double m_frame_rate = 24.0;

		/// <summary>True if the animation is running</summary>
		public bool Animating => ViewWindow?.Animating ?? false;

		/// <summary>If true, animations play relative to their start frame</summary>
		public bool RelativeTime
		{
			get => m_relative_time;
			set
			{
				if (RelativeTime == value) return;
				m_relative_time = value;
				NotifyPropertyChanged(nameof(RelativeTime));
			}
		}
		private bool m_relative_time;

		/// <summary>The start frame</summary>
		public int Frame0
		{
			get => m_frame0;
			set
			{
				if (Frame0 == value) return;
				m_frame0 = value;
				NotifyPropertyChanged(nameof(Frame0));
			}
		}
		private int m_frame0 = 0;

		/// <summary>The current frame</summary>
		public int Frame
		{
			get => (int)Math.Floor(AnimClock * FrameRate);
			set
			{
				if (Frame == value) return;
				AnimClock = value / FrameRate;
			}
		}

		/// <summary>The end frame</summary>
		public int FrameN
		{
			get => m_frameN;
			set
			{
				if (FrameN == value) return;
				m_frameN = value;
				NotifyPropertyChanged(nameof(FrameN));
			}
		}
		private int m_frameN = 100;

		/// <summary>Reset the anim clock to 0</summary>
		public Command Reset { get; }
		private void ResetInternal()
		{
			if (ViewWindow == null) return;
			ViewWindow.AnimControl(View3d.EAnimCommand.Reset);
		}

		/// <summary>Start the animation</summary>
		public Command Play { get; }
		private void PlayInternal()
		{
			if (ViewWindow == null) return;
			ViewWindow.AnimControl(View3d.EAnimCommand.Play, StepSize);
		}

		/// <summary>Stop the animation</summary>
		public Command Pause { get; }
		private void PauseInternal()
		{
			if (ViewWindow == null) return;
			ViewWindow.AnimControl(View3d.EAnimCommand.Stop);
		}

		/// <summary>Step the animation one frame backwards</summary>
		public Command StepBack { get; }
		private void StepBackInternal()
		{
			if (ViewWindow == null) return;
			ViewWindow.AnimControl(View3d.EAnimCommand.Step, -StepSize);
		}

		/// <summary>Step the animation one frame forwards</summary>
		public Command StepForward { get; }
		private void StepForwardInternal()
		{
			if (ViewWindow == null) return;
			ViewWindow.AnimControl(View3d.EAnimCommand.Step, +StepSize);
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
