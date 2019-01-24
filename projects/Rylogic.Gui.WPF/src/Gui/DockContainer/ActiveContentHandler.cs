using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>Implementation of active panes and active content for dock containers, floating windows, and auto hide windows</summary>
	internal class ActiveContentImpl
	{
		public ActiveContentImpl(ITreeHost tree_host)
		{
			TreeHost = tree_host;
		}

		/// <summary>The root of the tree in this dock container</summary>
		private ITreeHost TreeHost { [DebuggerStepThrough] get; set; }

		/// <summary>
		/// Get/Set the active content. This will cause the pane that the content is on to also become active.
		/// To change the active content in a pane without making the pane active, assign to the pane's ActiveContent property</summary>
		public DockControl ActiveContent
		{
			get { return ActivePane?.VisibleContent; }
			set
			{
				if (ActiveContent == value) return;

				// Ensure 'value' is the active content on its pane
				if (value != null)
					value.DockPane.VisibleContent = value;

				// Set value's pane as the active one.
				// If value's pane was the active one before, then setting 'value' as the active content
				// on the pane will also have caused an OnActiveContentChanged to be called. If not, then
				// changing the active pane here will result in OnActiveContentChanged being called.
				ActivePane = value?.DockPane;
			}
		}

		/// <summary>Get/Set the active pane. Note, this pane may be within the dock container, a floating window, or an auto hide window</summary>
		public DockPane ActivePane
		{
			get { return m_active_pane; }
			set
			{
				if (m_active_pane == value) return;
				var old_pane = m_active_pane;
				var old_content = ActiveContent;

				// Change the pane
				if (m_active_pane != null)
				{
					m_active_pane.VisibleContentChanged -= HandleActiveContentChanged;
					m_active_pane.VisibleContent?.SaveFocus();
				}
				m_prev_pane = new WeakReference<DockPane>(m_active_pane);
				m_active_pane = value;
				if (m_active_pane != null)
				{
					m_active_pane.VisibleContent?.RestoreFocus();
					m_active_pane.VisibleContentChanged += HandleActiveContentChanged;
				}

				// Notify observers of each pane about activation changed
				old_pane?.OnActivatedChanged();
				value?.OnActivatedChanged();

				// Notify that the active pane has changed and therefore the active content too
				if (old_pane != value)
					RaiseActivePaneChanged(new ActivePaneChangedEventArgs(old_pane, value));
				if (old_content?.Dockable != ActiveContent?.Dockable)
					RaiseActiveContentChanged(new ActiveContentChangedEventArgs(old_content?.Dockable, ActiveContent?.Dockable));

				/// <summary>Watch for the content in the active pane changing</summary>
				void HandleActiveContentChanged(object sender, ActiveContentChangedEventArgs e)
				{
					RaiseActiveContentChanged(e);
				}
			}
		}
		private DockPane m_active_pane;
		private WeakReference<DockPane> m_prev_pane;

		/// <summary>Make the previously active dock pane the active pane</summary>
		public void ActivatePrevious()
		{
			var pane = m_prev_pane.TryGetTarget(out var p) ? p : null;
			if (pane != null && pane.DockContainer != null)
				ActivePane = pane;
		}

		/// <summary>Raised whenever the active pane changes in the dock container</summary>
		public event EventHandler<ActivePaneChangedEventArgs> ActivePaneChanged;
		internal void RaiseActivePaneChanged(ActivePaneChangedEventArgs args)
		{
			ActivePaneChanged?.Invoke(TreeHost, args);
		}

		/// <summary>Raised whenever the active content for the dock container changes</summary>
		public event EventHandler<ActiveContentChangedEventArgs> ActiveContentChanged;
		internal void RaiseActiveContentChanged(ActiveContentChangedEventArgs args)
		{
			ActiveContentChanged?.Invoke(TreeHost, args);
		}
	}

}
