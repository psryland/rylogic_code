using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>A floating window that hosts a tree of dock panes</summary>
	[DebuggerDisplay("FloatingWindow")]
	public partial class FloatingWindow : Window, ITreeHost, IDisposable
	{
		private Panel m_content;
		public FloatingWindow(DockContainer dc)
		{
			InitializeComponent();

			ShowInTaskbar = true;
			ResizeMode = ResizeMode.CanResizeWithGrip;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			//HideOnClose = true;
			//Text = string.Empty;
			//ShowIcon = false;
			Owner = GetWindow(dc);
			Content = m_content = new DockPanel { LastChildFill = true };

			DockContainer = dc;
			Root = new Branch(dc, DockSizeData.Quarters);
		}
		public virtual void Dispose()
		{
			Root = null;
			DockContainer = null;
		}

		/// <summary>An identifier for a floating window</summary>
		public int Id { get; set; }

		/// <summary>The dock container that owns this floating window</summary>
		public DockContainer DockContainer
		{
			get { return m_impl_dc; }
			private set
			{
				if (m_impl_dc == value) return;
				if (m_impl_dc != null)
				{
					m_impl_dc.ActiveContentChanged -= HandleActiveContentChanged;
				}
				m_impl_dc = value;
				if (m_impl_dc != null)
				{
					m_impl_dc.ActiveContentChanged += HandleActiveContentChanged;
				}

				/// <summary>Handler for when the active content changes</summary>
				void HandleActiveContentChanged(object sender, ActiveContentChangedEventArgs e)
				{
					//						InvalidateVisual();
				}
			}
		}
		DockContainer ITreeHost.DockContainer
		{
			get { return DockContainer; }
		}
		private DockContainer m_impl_dc;

		/// <summary>The root level branch of the tree in this floating window</summary>
		internal Branch Root
		{
			[DebuggerStepThrough]
			get { return m_root; }
			set
			{
				if (m_root == value) return;
				if (m_root != null)
				{
					m_root.TreeChanged -= HandleTreeChanged;
					m_content.Children.Remove(m_root);
					Util.Dispose(ref m_root);
				}
				m_root = value;
				if (m_root != null)
				{
					m_content.Children.Add(m_root);
					m_root.TreeChanged += HandleTreeChanged;
				}

				/// <summary>Handler for when panes are added/removed from the tree</summary>
				void HandleTreeChanged(object sender, TreeChangedEventArgs args)
				{
					switch (args.Action)
					{
					case TreeChangedEventArgs.EAction.ActiveContent:
						{
							UpdateUI();
							break;
						}
					case TreeChangedEventArgs.EAction.Added:
						{
							// When the first pane is added to the window, update the title
							//								InvalidateArrange();
							if (Root.AllContent.CountAtMost(2) == 1)
							{
								ActivePane = Root.AllPanes.First();
								UpdateUI();
							}
							break;
						}
					case TreeChangedEventArgs.EAction.Removed:
						{
							// Whenever content is removed, check to see if the floating
							// window is empty, if so, then hide the floating window.
							//								InvalidateArrange();
							if (!Root.AllContent.Any())
							{
								Hide();
								UpdateUI();
							}
							break;
						}
					}

					/// <summary>Update the floating window</summary>
					void UpdateUI()
					{
#if false
							var content = ActiveContent;
							if (content != null)
							{
								Text = content.TabText;
								var bm = content.TabIcon as Bitmap;
								if (bm != null)
								{
									// Can destroy the icon created from the bitmap because
									// the form creates it's own copy of the icon.
									var hicon = bm.GetHicon();
									Icon = Icon.FromHandle(hicon); // Copy assignment
									Win32.DestroyIcon(hicon);
								}
							}
							else
							{
								Text = string.Empty;
								Icon = null;
							}
#endif
					}
				}
			}
		}
		Branch ITreeHost.Root
		{
			get { return Root; }
		}
		private Branch m_root;

		/// <summary>
		/// Get/Set the active content on this floating window. This will cause the pane that the content is on to also become active.
		/// To change the active content in a pane without making the pane active, assign to the pane's ActiveContent property</summary>
		public DockControl ActiveContent
		{
			get { return DockContainer.ActiveContent; }
			set { DockContainer.ActiveContent = value; }
		}

		/// <summary>Get/Set the active pane. Note, this pane may be within the dock container, a floating window, or an auto hide window</summary>
		public DockPane ActivePane
		{
			get { return DockContainer.ActivePane; }
			set { DockContainer.ActivePane = value; }
		}

		/// <summary>The current screen location and size of this window</summary>
		public Rect Bounds
		{
			get { return new Rect(Left, Top, Width, Height); }
			set
			{
				Left = value.Left;
				Top = value.Top;
				Width = value.Width;
				Height = value.Height;
			}
		}

		/// <summary>Add a dockable instance to this branch at the position described by 'location'.</summary>
		internal DockPane Add(DockControl dc, int index, params EDockSite[] location)
		{
			if (dc == null)
				throw new ArgumentNullException(nameof(dc), "'dockable' or 'dockable.DockControl' cannot be 'null'");

			return Root.Add(dc, index, location);
		}
		public DockPane Add(IDockable dockable, int index, params EDockSite[] location)
		{
			return Add(dockable?.DockControl, index, location);
		}
		public DockPane Add(IDockable dockable, params EDockSite[] location)
		{
			var addr = location.Length != 0 ? location : new[] { EDockSite.Centre };
			return Add(dockable, int.MaxValue, addr);
		}
#if false
			/// <summary>Customise floating window behaviour</summary>
			protected override void WndProc(ref Message m)
			{
				switch (m.Msg)
				{
				default:
					{
						break;
					}
				case Win32.WM_NCLBUTTONDBLCLK:
					{
						// Double clicking the title bar returns the contents to the dock container
						if (DockContainer.Options.DoubleClickTitleBarToDock &&
							(int)Win32.SendMessage(Handle, Win32.WM_NCHITTEST, IntPtr.Zero, m.LParam) == (int)Win32.HitTest.HTCAPTION)
						{
							// Move all content back to the dock container
							using (this.SuspendLayout(layout_on_resume: false))
							{
								var content = Root.AllPanes.SelectMany(x => x.Content).ToArray();
								foreach (var c in content)
									c.IsFloating = false;

								Root?.TriggerLayout();
							}

							Hide();
							return;
						}
						break;
					}
				}
				base.WndProc(ref m);
			}
#endif
		/// <summary>Save state to XML</summary>
		public XElement ToXml(XElement node)
		{
			// Save the ID assigned to this window
			node.Add2(XmlTag.Id, Id, false);

			//				// Save whether the floating window is pinned to the dock container
			//				node.Add2(XmlTag.Pinned, PinWindow, false);

			// Save the screen-space location of the floating window. If pinned, save the offset bounds
			var bnds = Bounds;
			//				if (PinWindow) bnds = bnds.Shifted(-TargetFrame.Left, -TargetFrame.Top);
			node.Add2(XmlTag.Bounds, bnds, false);

			// Save whether the floating window is shown or now
			node.Add2(XmlTag.Visible, IsVisible, false);

			// Save the tree structure of the floating window
			node.Add2(XmlTag.Tree, Root, false);
			return node;
		}

		/// <summary>Apply state to this floating window</summary>
		public void ApplyState(XElement node)
		{
			//				// Restore the pinned state
			//				var pinned = node.Element(XmlTag.Pinned)?.As<bool>();
			//				if (pinned != null)
			//					PinWindow = pinned.Value;

			// Move the floating window to the saved position (clamped by the virtual screen)
			var bounds = node.Element(XmlTag.Bounds)?.As<Rect>();
			if (bounds != null)
			{
				// If 'PinWindow' is set, then the bounds are relative to the parent window
				var bnds = bounds.Value;
				//					if (PinWindow) bnds = bnds.Shifted(TargetFrame.Left, TargetFrame.Top);
				Bounds = Gui_.OnScreen(bnds);
			}

			// Update the tree layout
			var tree_node = node.Element(XmlTag.Tree);
			if (tree_node != null)
				Root.ApplyState(tree_node);

			// Restore visibility
			var visible = node.Element(XmlTag.Visible)?.As<bool>();
			if (visible != null)
				Visibility = visible.Value ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
