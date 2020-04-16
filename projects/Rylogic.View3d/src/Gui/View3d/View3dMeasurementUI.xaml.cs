using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class View3dMeasurementUI : Window, INotifyPropertyChanged
	{
		public View3dMeasurementUI(View3dControl owner)
		{
			InitializeComponent();
			Owner = GetWindow(owner);

			View3dCtrl = owner;
			Measurement = new Measurement(owner.Window);
			PinState = new PinData(this, EPin.TopRight);

			// Set up commands
			ChangeSpotColour = Command.Create(this, () =>
			{
				var dlg = new ColourPickerUI { Owner = this, Colour = Measurement.SpotColour };
				if (dlg.ShowDialog() == true)
					Measurement.SpotColour = dlg.Colour;
			});
			ReferenceFrames = new ListCollectionView(Enum<Measurement.EReferenceFrame>.ValuesArray);

			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			Measurement = null!;
			View3dCtrl = null!;
			PinState = null;
			base.OnClosed(e);
		}

		/// <summary>The View3d Control that contains the 3d scene</summary>
		public View3dControl View3dCtrl
		{
			get { return m_view3d_ctrl; }
			private set
			{
				if (m_view3d_ctrl == value) return;
				if (m_view3d_ctrl != null)
				{
					m_view3d_ctrl.MouseDown -= HandleMouseDown;
					m_view3d_ctrl.MouseMove -= HandleMouseMove;
					m_view3d_ctrl.MouseUp -= HandleMouseUp;
				}
				m_view3d_ctrl = value;
				if (m_view3d_ctrl != null)
				{
					m_view3d_ctrl.MouseUp += HandleMouseUp;
					m_view3d_ctrl.MouseMove += HandleMouseMove;
					m_view3d_ctrl.MouseDown += HandleMouseDown;
				}
				void HandleMouseDown(object sender, MouseButtonEventArgs e)
				{
					if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
						Measurement.MouseDown(e.GetPosition(m_view3d_ctrl).ToV2());
				}
				void HandleMouseMove(object sender ,MouseEventArgs e)
				{
					Measurement.MouseMove(e.GetPosition(m_view3d_ctrl).ToV2());
				}
				void HandleMouseUp(object sender, MouseButtonEventArgs e)
				{
					if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
						Measurement.MouseUp();
				}
			}
		}
		private View3dControl m_view3d_ctrl = null!;

		/// <summary>The view model for the measurement behaviour</summary>
		public Measurement Measurement
		{
			get => m_measurement;
			private set
			{
				if (m_measurement == value) return;
				if (m_measurement != null)
				{
					m_measurement.PropertyChanged -= HandlePropertyChanged;
					Util.Dispose(ref m_measurement!);
				}
				m_measurement = value;
				if (m_measurement != null)
				{
					m_measurement.PropertyChanged += HandlePropertyChanged;
				}

				// Handler
				void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
					case nameof(Measurement.Flags):
						{
							PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SnapToVerts)));
							PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SnapToEdges)));
							PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SnapToFaces)));
							break;
						}
					case nameof(Measurement.ActiveHit):
						{
							PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartActive)));
							PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EndActive)));
							break;
						}
					case nameof(Measurement.SpotColour):
						{
							PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpotColourBrush)));
							break;
						}
					case nameof(Measurement.SnapDistance):
						{
							PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SnapDistance)));
							break;
						}
					}
				}
			}
		}
		private Measurement m_measurement = null!;

		/// <summary>Pinned window support</summary>
		private PinData? PinState
		{
			get => m_pin_state;
			set
			{
				if (m_pin_state == value) return;
				Util.Dispose(ref m_pin_state);
				m_pin_state = value;
			}
		}
		private PinData? m_pin_state;

		/// <summary>Snap to distance</summary>
		public double SnapDistance
		{
			get { return Measurement.SnapDistance; }
			set { Measurement.SnapDistance = value; }
		}

		/// <summary>Snap bindings</summary>
		public bool SnapToVerts
		{
			get { return Measurement.Flags.HasFlag(View3d.EHitTestFlags.Verts); }
			set { Measurement.Flags = Bit.SetBits(Measurement.Flags, View3d.EHitTestFlags.Verts, value); }
		}
		public bool SnapToEdges
		{
			get { return Measurement.Flags.HasFlag(View3d.EHitTestFlags.Edges); }
			set { Measurement.Flags = Bit.SetBits(Measurement.Flags, View3d.EHitTestFlags.Edges, value); }
		}
		public bool SnapToFaces
		{
			get { return Measurement.Flags.HasFlag(View3d.EHitTestFlags.Faces); }
			set { Measurement.Flags = Bit.SetBits(Measurement.Flags, View3d.EHitTestFlags.Faces, value); }
		}

		/// <summary>Binding for which set point to use</summary>
		public bool StartActive
		{
			get { return Measurement.ActiveHit == Measurement.Hit0; }
			set { Measurement.ActiveHit = value ? Measurement.Hit0 : null; }
		}
		public bool EndActive
		{
			get { return Measurement.ActiveHit == Measurement.Hit1; }
			set { Measurement.ActiveHit = value ? Measurement.Hit1 : null; }
		}

		/// <summary>Binding for the spot colour</summary>
		public Brush SpotColourBrush => new SolidColorBrush(Measurement.SpotColour.ToMediaColor());

		/// <summary>Change the measurement spot colour</summary>
		public ICommand ChangeSpotColour { get; }

		/// <summary>The available measurement reference frames</summary>
		public ICollectionView ReferenceFrames
		{
			get => m_reference_frames;
			private set
			{
				if (m_reference_frames == value) return;
				if (m_reference_frames != null)
				{
					m_reference_frames.CurrentChanged -= HandleReferenceFrameChanged;
				}
				m_reference_frames = value;
				if (m_reference_frames != null)
				{
					ReferenceFrames.MoveCurrentTo(Measurement.ReferenceFrame);
					m_reference_frames.CurrentChanged += HandleReferenceFrameChanged;
				}

				// Handlers
				void HandleReferenceFrameChanged(object? sender, EventArgs e)
				{
					Measurement.ReferenceFrame = (Measurement.EReferenceFrame)ReferenceFrames.CurrentItem;
				}
			}
		}
		private ICollectionView m_reference_frames = null!;

		/// <summary>The measurement results</summary>
		public IBindingList Results => Measurement.Results;

		/// <summary>Display the colour picker</summary>
		private void HandleEditSpotColour(object? sender, RoutedEventArgs e)
		{
			MessageBox.Show("Not Done Yet");
		}

		/// <summary>Event handler that updates the bound source when enter is pressed</summary>
		private void UpdateBindingOnEnterPressed(object? sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter || !(sender is TextBox tb))
				return;

			var binding = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
			if (binding != null)
				binding.UpdateSource();
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>Change the spot colour</summary>
		private void HandleEditSpotColour(object sender, MouseButtonEventArgs e)
		{
			ChangeSpotColour.Execute(null);
		}
	}
}
