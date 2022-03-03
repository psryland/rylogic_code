using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Rylogic.Gui.WPF;
using Rylogic.Maths;

namespace TestWPF
{
	public partial class RadialProgressUI :Window, INotifyPropertyChanged
	{
		public RadialProgressUI()
		{
			InitializeComponent();
			Minimum = 0.0;
			Maximum = 10.0;
			ToggleGo = Command.Create(this, () => Go = !Go);
			DataContext = this;
		}

		/// <summary></summary>
		public double Minimum
		{
			get => m_minimum;
			set => SetProp(ref m_minimum, value, nameof(Minimum));
		}
		private double m_minimum;

		/// <summary></summary>
		public double Maximum
		{
			get => m_maximum;
			set => SetProp(ref m_maximum, value, nameof(Maximum));
		}
		private double m_maximum;

		/// <summary></summary>
		public double Value
		{
			get => m_value;
			set => SetProp(ref m_value, value, nameof(Value));
		}
		private double m_value;

		/// <summary></summary>
		public bool Go
		{
			get => m_timer != null;
			set
			{
				if (Go == value) return;
				if (m_timer != null)
				{
					m_timer.Stop();
				}
				m_timer = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Background, HandleTick, Dispatcher.CurrentDispatcher) : null;
				if (m_timer != null)
				{
					m_start = DateTimeOffset.Now;
					m_timer.Start();
				}
				NotifyPropertyChanged(nameof(Go));

				// Handler
				void HandleTick(object? sender, EventArgs e)
				{
					var dt = (DateTimeOffset.Now - m_start).TotalSeconds;
					Value = Math_.Clamp(dt, Minimum, Maximum);
					Go = dt <= Maximum;
				}
			}
		}
		private DispatcherTimer? m_timer; 
		private DateTimeOffset m_start;

		/// <summary></summary>
		public ICommand ToggleGo { get; }

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		private void SetProp<T>(ref T prop, T value, string prop_name)
		{
			if (Equals(prop, value)) return;
			prop = value;
			NotifyPropertyChanged(prop_name);
		}
	}
}
