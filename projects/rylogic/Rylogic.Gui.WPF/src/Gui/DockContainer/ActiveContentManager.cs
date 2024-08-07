﻿using System;
using System.Windows;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	internal class ActiveContentManager
	{
		// Notes:
		//  - This class manages the active pane/content across all auto
		//    hide panels and floating windows.
		//  - ActiveXXXChanged events on tree hosts are all forwarded to
		//    the events on this class

		public ActiveContentManager(DockContainer dc)
		{
			DockContainer = dc;
		}

		/// <summary>The root of the tree in this dock container</summary>
		private DockContainer DockContainer { get; }

		/// <summary>
		/// Get/Set the active content. This will cause the pane that the content is on to also become active.
		/// To change the active content in a pane without making the pane active, assign to the pane's ActiveContent property</summary>
		public DockControl? ActiveContent
		{
			get => ActivePane?.VisibleContent;
			set
			{
				if (ActiveContent == value) return;

				// Ensure 'value' is the active content on its pane
				if (value?.DockPane is DockPane dp)
					dp.VisibleContent = value;

				// Set value's pane as the active one.
				// If value's pane was the active one before, then setting 'value' as the active content
				// on the pane will also have caused an OnActiveContentChanged to be called. If not, then
				// changing the active pane here will result in OnActiveContentChanged being called.
				ActivePane = value?.DockPane;
			}
		}

		/// <summary>Get/Set the active pane. Note, this pane may be within the dock container, a floating window, or an auto hide window</summary>
		public DockPane? ActivePane
		{
			get => m_active_pane;
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
				PrevPane = m_active_pane;
				m_active_pane = value;
				if (m_active_pane != null)
				{
					// Ensure the containing window is visible
					if (PresentationSource.FromVisual(m_active_pane)?.RootVisual is Window wnd)
						wnd.Visibility = Visibility.Visible;

					m_active_pane.VisibleContent?.RestoreFocus();
					m_active_pane.VisibleContentChanged += HandleActiveContentChanged;
				}

				// Notify observers of each pane about activation changed
				old_pane?.OnActivatedChanged();
				value?.OnActivatedChanged();

				// Notify that the active pane has changed and therefore the active content too
				if (old_pane != value)
					NotifyActivePaneChanged(new ActivePaneChangedEventArgs(old_pane, value));
				if (old_content?.Dockable != ActiveContent?.Dockable)
					NotifyActiveContentChanged(new ActiveContentChangedEventArgs(old_content?.Dockable, ActiveContent?.Dockable));

				/// <summary>Watch for the content in the active pane changing</summary>
				void HandleActiveContentChanged(object? sender, ActiveContentChangedEventArgs e)
				{
					NotifyActiveContentChanged(e);
				}
			}
		}
		private DockPane? m_active_pane;
		
		/// <summary>The previously active dock pane</summary>
		public DockPane? PrevPane
		{
			get => m_prev_pane2 != null ? (m_prev_pane2.TryGetTarget(out var p) ? p : null) : null;
			set => m_prev_pane2 = value != null ? new WeakReference<DockPane>(value) : null;
		}
		private WeakReference<DockPane>? m_prev_pane2;

		/// <summary>Make the previously active dock pane the active pane</summary>
		public void ActivatePrevious()
		{
			// If a previous pane is know, try to reactivate it
			if (PrevPane is DockPane p && p.DockContainer != null)
				ActivePane = p;
			
			// Otherwise, try to activate the centre pane on the main dock container
			else
				ActivePane = DockContainer.Root.DockPane(EDockSite.Centre);
		}

		/// <summary>Raised whenever the active pane changes in the dock container</summary>
		public event EventHandler<ActivePaneChangedEventArgs>? ActivePaneChanged;
		internal void NotifyActivePaneChanged(ActivePaneChangedEventArgs args)
		{
			ActivePaneChanged?.Invoke(DockContainer, args);
		}

		/// <summary>Raised whenever the active content for the dock container changes</summary>
		public event EventHandler<ActiveContentChangedEventArgs>? ActiveContentChanged;
		internal void NotifyActiveContentChanged(ActiveContentChangedEventArgs args)
		{
			ActiveContentChanged?.Invoke(DockContainer, args);
		}
	}
}
