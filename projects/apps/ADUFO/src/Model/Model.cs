using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ADUFO.DomainObjects;
using Microsoft.VisualStudio.Services.Common;
using Rylogic.Gui.WPF;
using Rylogic.Gui.WPF.ChartDiagram;
using Rylogic.Utility;

namespace ADUFO;

public class Model : IDisposable, INotifyPropertyChanged
{
	public Model(Settings settings)
	{
		Settings = settings;
		Items = new WorkItemDB(settings);
		Ado = null!;
	}
	public void Dispose()
	{
		Util.Dispose(Items);
		Ado = null!;
	}

	/// <summary>Application settings</summary>
	private Settings Settings { get; }

	/// <summary>Database of </summary>
	public WorkItemDB Items { get; }

	/// <summary>Access to ADO</summary>
	public AdoInterface? Ado
	{
		get => m_ado;
		set
		{
			if (m_ado == value) return;
			m_ado = value;
			NotifyPropertyChanged(nameof(Ado));
		}
	}
	private AdoInterface? m_ado;

	/// <summary>Reload the data from ADO</summary>
	public async Task Refresh(ChartControl chart)
	{
		if (Ado is not AdoInterface)
			return;

		using var cancel = new CancellationTokenSource();
		/*
		// Download work streams
		var task0 = Ado.WorkStreams(cancel.Token)
			.ContinueWith(workstreams =>
			{
				if (workstreams.IsFaulted) return;
				foreach (var workstream in workstreams.Result)
				{
					if (workstream.Id == null) continue;
					var ws = Items.WorkStreams.GetOrAddValue(workstream.Id.Value, () => new WorkStream(workstream));
					ws.Item = workstream;
					ws.Chart = chart;
				}
			});

		// Download epics
		var task1 = Ado.Epics(cancel.Token)
			.ContinueWith(epics =>
			{
				if (epics.IsFaulted) return;
				foreach (var epic in epics.Result)
				{
					if (epic.Id == null) continue;
					var ws = Items.Epics.GetOrAddValue(epic.Id.Value, () => new Epic(epic));
					ws.Item = epic;
					ws.Chart = chart;
				}
			});

		await Task.WhenAll(task0, task1);
*/
		var workstreams = Ado.WorkStreams(cancel.Token);
		var epics = Ado.Epics(cancel.Token);

		// Download work streams
		foreach (var workstream in await workstreams)
		{
			if (workstream.Id == null) continue;
			var ws = Items.WorkStreams.GetOrAddValue(workstream.Id.Value, () => new WorkStream(workstream));
			ws.Item = workstream;
			ws.Chart = chart;
		}

		// Download epics
		foreach (var epic in await epics)
		{
			if (epic.Id == null) continue;
			if (epic.Fields.TryGetValue("System.Parent", out var _) == false) continue;
			var ws = Items.Epics.GetOrAddValue(epic.Id.Value, () => new Epic(epic));
			ws.Item = epic;
			ws.Chart = chart;
		}

		// Create connections
		foreach (var epic in Items.Epics.Values)
		{
			if (epic.ParentItem is not long parent_item_id) continue;
			if (!Items.WorkStreams.TryGetValue((int)parent_item_id, out var ws)) continue;

			// Create a connection
			static long MakeKey(int id0, int id1) => ((long)id0 << 32) | (uint)id1;
			var link = Items.Links.GetOrAddValue(MakeKey(ws.ItemId, epic.ItemId), () => new Link(ws, epic, Connector.EType.Line));
			link.Chart = chart;
		}

		//// Persist the layout to storage
		//Items.Save();
	}

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
}
