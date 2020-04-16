﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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
			m_root.DataContext = this;
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
			}
		}
		public static readonly DependencyProperty ViewWindowProperty = Gui_.DPRegister<View3dAnimControls>(nameof(ViewWindow), flags:FrameworkPropertyMetadataOptions.None);

		/// <summary>Animation time</summary>
		public double AnimClock
		{
			get => ViewWindow?.AnimTime ?? 0.0;
			set
			{
				if (AnimClock == value || ViewWindow == null) return;
				ViewWindow.AnimTime = value;
				NotifyPropertyChanged(nameof(AnimClock));
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

		/// <summary>True if the animation is running</summary>
		public bool Animating => ViewWindow?.Animating ?? false;

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

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
