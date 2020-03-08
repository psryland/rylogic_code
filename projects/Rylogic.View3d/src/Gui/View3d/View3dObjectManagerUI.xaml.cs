using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class View3dObjectManagerUI : Window, INotifyPropertyChanged
	{
		public View3dObjectManagerUI(Window owner, View3d.Window window, IEnumerable<Guid>? exclude = null)
		{
			InitializeComponent();
			Owner = owner;
			Icon = Owner?.Icon;

			PinState = new PinData(this, EPin.Centre);
			ObjectManager = new View3d.ObjectManager(window, exclude ?? Array.Empty<Guid>());
			ObjectsView = new ListCollectionView(ObjectManager.Objects);

			ExpandAll = Command.Create(this, ExpandAllInternal);
			CollapseAll = Command.Create(this, CollapseAllInternal);
			ApplyFilter = Command.Create(this, ApplyFilterInternal);
			SetVisible = Command.Create(this, SetVisibleInternal);
			SetWireframe = Command.Create(this, SetWireframeInternal);
			UpdateSelected = Command.Create(this, UpdateSelectedInternal);
			InvertSelection = Command.Create(this, InvertSelectionInternal);
			ToggleShowNormals = Command.Create(this, InternalToggleShowNormals);

			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			ObjectManager = null!;
			PinState = null;
			base.OnClosed(e);
		}

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

		/// <summary>The view model for the object manager behaviour</summary>
		public View3d.ObjectManager ObjectManager
		{
			get => m_object_manager;
			private set
			{
				if (m_object_manager == value) return;
				if (m_object_manager != null)
				{
					m_object_manager.PropertyChanged -= HandlePropertyChanged;
					View3d.Object.ObjectChanged -= HandleObjectPropertyChanged;
					Util.Dispose(ref m_object_manager!);
				}
				m_object_manager = value;
				if (m_object_manager != null)
				{
					View3d.Object.ObjectChanged += HandleObjectPropertyChanged;
					m_object_manager.PropertyChanged += HandlePropertyChanged;
				}

				// Handler
				void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
					case nameof(View3d.ObjectManager.Objects):
						{
							ObjectsView.Refresh();
							NotifyPropertyChanged(nameof(Objects));
							break;
						}
					}
				}
				void HandleObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
					case nameof(View3d.Object.Flags):
						{
							NotifyPropertyChanged(nameof(FirstSelected));
							break;
						}
					}
				}
			}
		}
		private View3d.ObjectManager m_object_manager = null!;

		/// <summary>Access to the object container</summary>
		public IList<View3d.Object> Objects => ObjectManager.Objects;
		public IEnumerable<View3d.Object> SelectedObjects => Objects.Where(x => Bit.AllSet(x.Flags, View3d.EFlags.Selected));

		/// <summary>True if one or more objects is selected</summary>
		public View3d.Object FirstSelected => SelectedObjects.FirstOrDefault();

		/// <summary>A view of the top-level objects in the scene</summary>
		public ICollectionView ObjectsView { get; private set; }

		/// <summary>Refresh</summary>
		private void Invalidate() => ObjectManager.Window.Invalidate();

		/// <summary></summary>
		public Command ExpandAll { get; }
		private void ExpandAllInternal()
		{
		}

		/// <summary></summary>
		public Command CollapseAll { get; }
		private void CollapseAllInternal()
		{
		}

		/// <summary></summary>
		public Command ApplyFilter { get; }
		private void ApplyFilterInternal(object? parameter)
		{
			if (parameter is Pattern pattern && pattern.Expr.Length != 0 && pattern.IsValid)
				ObjectsView.Filter = x => pattern.IsMatch(((View3d.Object)x).Name);
			else
				ObjectsView.Filter = null;
		}

		/// <summary>Show/Hide objects</summary>
		public Command SetVisible { get; }
		private void SetVisibleInternal(object? parameter)
		{
			if (parameter is ESetVisibleCmd vis)
			{
				foreach (var x in Objects)
				{
					var selected = x.Flags.HasFlag(View3d.EFlags.Selected);
					var visible = x.Flags.HasFlag(View3d.EFlags.Hidden) == false;
					var show = vis switch
					{
						ESetVisibleCmd.ShowAll => true,
						ESetVisibleCmd.HideAll => false,
						ESetVisibleCmd.ShowSelected => selected ? true : visible,
						ESetVisibleCmd.HideSelected => selected ? false : visible,
						ESetVisibleCmd.ToggleSelected => selected ? !visible : visible,
						ESetVisibleCmd.ShowOthers => selected ? visible : true,
						ESetVisibleCmd.HideOthers => selected ? visible : false,
						ESetVisibleCmd.ToggleOthers => selected ? visible : !visible,
						_ => throw new Exception($"Unknown visibility command {vis}"),
					};
					x.FlagsSet(View3d.EFlags.Hidden, !show, string.Empty);
				}
				Invalidate();
				return;
			}
			throw new Exception($"SetVisible parameter '{parameter}' is invalid. Expected +1, 0, or -1");
		}

		/// <summary>Switch between wireframe and solid</summary>
		public Command SetWireframe { get; }
		private void SetWireframeInternal(object? parameter)
		{
			if (parameter is string wf && int.TryParse(wf, out var wireframe))
			{
				foreach (var x in SelectedObjects)
				{
					var wire = wireframe == +1 || (wireframe == 0 && !Bit.AllSet(x.Flags, View3d.EFlags.Wireframe));
					x.FlagsSet(View3d.EFlags.Wireframe, wire, string.Empty);
				}
				Invalidate();
				return;
			}
			throw new Exception($"SetWireframe parameter '{parameter}' is invalid. Expected +1, 0, or -1");
		}

		/// <summary>Set the 'selected' state for the objects</summary>
		public Command UpdateSelected { get; }
		private void UpdateSelectedInternal(object? parameter)
		{
			if (parameter is SelectionChangedEventArgs args)
			{
				foreach (var x in args.RemovedItems.Cast<View3d.Object>())
					x.Flags = Bit.SetBits(x.Flags, View3d.EFlags.Selected, false);
				foreach (var x in args.AddedItems.Cast<View3d.Object>())
					x.Flags = Bit.SetBits(x.Flags, View3d.EFlags.Selected, true);

				Invalidate();
			}
		}

		/// <summary>Invert the selection status of each object</summary>
		public Command InvertSelection { get; }
		private void InvertSelectionInternal()
		{
			foreach (var obj in Objects)
				obj.Flags = Bit.SetBits(obj.Flags, View3d.EFlags.Selected, !obj.Flags.HasFlag(View3d.EFlags.Selected));
		}
		
		/// <summary>Toggle show normals mode on selected objects</summary>
		public Command ToggleShowNormals { get; }
		private void InternalToggleShowNormals()
		{
			bool? show = null;
			foreach (var obj in SelectedObjects)
			{
				show = show ?? !obj.ShowNormals;
				obj.ShowNormals = show.Value;
			}
			Invalidate();
			return;
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary></summary>
		public enum ESetVisibleCmd
		{
			ShowAll,
			HideAll,
			ShowSelected,
			HideSelected,
			ToggleSelected,
			ShowOthers,
			HideOthers,
			ToggleOthers,
		}
	}
}
