//***************************************************
// Drag Drop
//  Copyright (c) Rylogic Ltd 2011
//***************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace pr.util
{
	// A DragDrop proxy object.
	// Allows the same drag drop handlers to be attached to multiple drop targets
	// e.g.
	//   Drop Receiver <--- DragDrop <-+- OnDragEnter
	//   Drop Receiver <--+            +- OnDragExit
	//   Drop Receiver <--+
	// Also unifies the dragdrop handler into a single function for all dd events

	/// <summary>Handles multiple drag/drop handlers for a single drop target</summary>
	public class DragDrop
	{
		public enum EDrop { Enter, Over, Leave, Drop }

		/// <summary>
		/// A single function that handles all dropping.
		/// Return true if this handler handles the dragged data</summary>
		public delegate bool DropHandler(object sender, DragEventArgs args, EDrop mode);

		/// <summary>
		/// Sign up to this with a unified drag drop handler.
		/// The first drop handler that returns true will receive the rest of the drop events</summary>
		public event DropHandler DoDrop
		{
			add    { m_handlers.Add(value); }
			remove { m_handlers.Remove(value); }
		}
		private readonly List<DropHandler> m_handlers = new List<DropHandler>();
		private DropHandler m_preferred;
		private DragEventArgs m_args;

		// Sign these up on the drop target
		// e.g.
		//   grid.OnDragEnter += dd.OnDragEnter
		//   grid.OnDragDrop  += dd.OnDragDrop
		//   etc
		// The Attach()/Detach() methods can do this by reflection if you want
		// Note: 'dd' can be attached to more than one drop target
		public DragEventHandler OnDragEnter { get; private set; }
		public DragEventHandler OnDragOver  { get; private set; }
		public EventHandler     OnDragLeave { get; private set; }
		public DragEventHandler OnDragDrop  { get; private set; }

		public DragDrop()
		{
			OnDragEnter = HandleDragEnter;
			OnDragOver  = HandleDragOver;
			OnDragLeave = HandleDragLeave;
			OnDragDrop  = HandleDragDrop;
		}
		public DragDrop(params object[] targets) :this()
		{
			foreach (var target in targets)
				Attach(target);
		}

		/// <summary>Sign up this mux to 'target'</summary>
		public void Attach(object target)
		{
			try
			{
				var type = target.GetType();

				var AllowDrop = type.GetProperty("AllowDrop", BindingFlags.Public|BindingFlags.Instance);
				if (AllowDrop != null)
					AllowDrop.GetSetMethod().Invoke(target, new object[]{true});
				else
					throw new ArgumentException(string.Format("Target type {0} does not have an 'AllowDrop' property",type.Name), "target");

				var events = type.GetEvents(BindingFlags.Instance|BindingFlags.Public);

				var DragEnter = events.FirstOrDefault(x => x.Name == "DragEnter");
				if (DragEnter != null)
					DragEnter.AddEventHandler(target, new DragEventHandler(OnDragEnter));

				var DragOver = events.FirstOrDefault(x => x.Name == "DragOver");
				if (DragOver != null)
					DragOver.AddEventHandler(target, new DragEventHandler(OnDragOver));

				var DragLeave = events.FirstOrDefault(x => x.Name == "DragLeave");
				if (DragLeave != null)
					DragLeave.AddEventHandler(target, new EventHandler(OnDragLeave));

				var DragDrop = events.FirstOrDefault(x => x.Name == "DragDrop");
				if (DragDrop != null)
					DragDrop.AddEventHandler(target, new DragEventHandler(OnDragDrop));

				if (DragEnter == null && DragOver == null && DragLeave == null && DragDrop == null)
					throw new ArgumentException("No appropriate Drag'n'Drop events were found on 'target'", "target");
			}
			catch
			{
				Detach(target);
				throw;
			}
		}

		/// <summary>Sign up this mux to 'target'</summary>
		public void Detach(object target)
		{
			var events = target.GetType().GetEvents(BindingFlags.Instance|BindingFlags.Public);

			var DragEnter = events.FirstOrDefault(x => x.Name == "DragEnter");
			if (DragEnter != null)
				DragEnter.RemoveEventHandler(target, new DragEventHandler(OnDragEnter));

			var DragOver = events.FirstOrDefault(x => x.Name == "DragOver");
			if (DragOver != null)
				DragOver.AddEventHandler(target, new DragEventHandler(OnDragOver));

			var DragLeave = events.FirstOrDefault(x => x.Name == "DragLeave");
			if (DragLeave != null)
				DragLeave.AddEventHandler(target, new EventHandler(OnDragLeave));

			var DragDrop = events.FirstOrDefault(x => x.Name == "DragDrop");
			if (DragDrop != null)
				DragDrop.AddEventHandler(target, new DragEventHandler(OnDragDrop));
		}

		/// <summary>Forwards the DragEnter event to the first handler that says it handles it</summary>
		private void HandleDragEnter(object sender, DragEventArgs args)
		{
			args.Effect = DragDropEffects.None;

			foreach (var handler in m_handlers)
			{
				if (!handler(sender, args, EDrop.Enter)) continue;
				m_preferred = handler;
				m_args = args;
				return;
			}
		}

		/// <summary>Forwards the DragOver event to the handler that handled DragEnter if it exists.
		/// Otherwise, forwards to the first handler that says it can handle it, which then becomes the preferred handler</summary>
		private void HandleDragOver(object sender, DragEventArgs args)
		{
			args.Effect = DragDropEffects.None;

			// If we have a preferred handler, let it handle it
			if (m_preferred != null)
			{
				m_preferred(sender, args, EDrop.Over);
				return;
			}

			// Otherwise, look for one that handles it
			foreach (var handler in m_handlers)
			{
				if (!handler(sender, args, EDrop.Over)) continue;
				m_preferred = handler;
				return;
			}
		}

		/// <summary>Forwards the OnDragLeave event to the preferred handler</summary>
		private void HandleDragLeave(object sender, EventArgs args)
		{
			if (m_preferred == null) return;
			m_preferred(sender, m_args, EDrop.Leave);
		}

		/// <summary>Forwards the OnDragDrop event to the preferred handler</summary>
		private void HandleDragDrop(object sender, DragEventArgs args)
		{
			if (m_preferred == null) return;
			m_preferred(sender, args, EDrop.Drop);
		}
	}
}
