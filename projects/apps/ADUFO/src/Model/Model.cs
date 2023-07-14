using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ADUFO.DomainObjects;
using Microsoft.VisualStudio.Services.Common;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace ADUFO;

public class Model :IDisposable, INotifyPropertyChanged
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

		// Download work streams
		var workstreams = await Ado.WorkStreams(cancel.Token);
		foreach (var workstream in workstreams)
		{
			if (workstream.Id == null) continue;
			var ws = Items.WorkStreams.GetOrAddValue(workstream.Id.Value, () => new WorkStream(workstream));
			ws.Item = workstream;
			ws.Chart = chart;
		}

		// Download epics
		var epics = await Ado.Epics(cancel.Token);
		foreach (var epic in epics)
		{
			if (epic.Id == null) continue;
			var ws = Items.Epics.GetOrAddValue(epic.Id.Value, () => new Epic(epic));
			ws.Item = epic;
			ws.Chart = chart;
		}

		//// Persist the layout to storage
		//Items.Save();
	}

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

}
