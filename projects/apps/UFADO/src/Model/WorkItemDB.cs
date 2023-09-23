using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;
using UFADO.DomainObjects;
using UFADO.Gfx;
using UFADO.Utility;

namespace UFADO;

public sealed class WorkItemDB :IDisposable
{
	// Notes:
	//  - This is a repository of all the work items.

	public WorkItemDB()
	{
		WorkStreams = new();
		Epics = new();
		Links = new();
	}
	public void Dispose()
	{
		Util.DisposeRange(WorkStreams.Values);
		Util.DisposeRange(Epics.Values);
		GC.SuppressFinalize(this);
	}

	/// <summary>Return all work items</summary>
	public IEnumerable<WorkItemNode> All
	{
		get => Enumerable_.Concat(
			WorkStreams.Values.Cast<WorkItemNode>(),
			Epics.Values.Cast<WorkItemNode>());
	}

	/// <summary>Work Stream items</summary>
	public Dictionary<ItemId, WorkStream> WorkStreams { get; }

	/// <summary>Work Stream items</summary>
	public Dictionary<ItemId, Epic> Epics { get; }

	/// <summary>The connections between objects</summary>
	public Dictionary<long, ParentLink> Links { get; }

	/// <summary>Persist items to storage</summary>
	public void Save(string filepath)
	{
		// Write to a temp file
		var filepath_tmp = $"{filepath}.tmp";
		{
			using var fs = new FileStream(filepath_tmp, FileMode.Create);
			using var wr = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });

			// File
			wr.WriteStartObject();
			{
				// Layout
				wr.WriteStartObject(JsonEncodedText.Encode("Layout"));
				{
					// Work Streams
					wr.WriteStartArray(JsonEncodedText.Encode(nameof(WorkStreams)));
					foreach (var ws in WorkStreams.Values.OrderBy(x => x.Id))
					{
						wr.WriteStartObject();
						wr.WriteNumber(JsonEncodedText.Encode(nameof(ws.ItemId)), ws.ItemId);
						wr.WriteString(JsonEncodedText.Encode(nameof(ws.Title)), ws.Title);
						//wr.WriteString(JsonEncodedText.Encode(nameof(ws.O2W)), ws.O2W.ToString3x4());
						wr.WriteEndObject();
					}
					wr.WriteEndArray();

					// Epics
					wr.WriteStartArray(JsonEncodedText.Encode(nameof(Epics)));
					foreach (var ep in Epics.Values.OrderBy(x => x.Id))
					{
						wr.WriteStartObject();
						wr.WriteNumber(JsonEncodedText.Encode(nameof(ep.ItemId)), ep.ItemId);
						wr.WriteString(JsonEncodedText.Encode(nameof(ep.Title)), ep.Title);
						wr.WriteNumber(JsonEncodedText.Encode(nameof(ep.ParentItemId)), (long?)ep.ParentItemId ?? 0);
						//wr.WriteString(JsonEncodedText.Encode(nameof(ep.O2W)), ep.O2W.ToString3x4());
						wr.WriteEndObject();
					}
					wr.WriteEndArray();

					// Features

					// User Stories

					// Tasks
				}
				wr.WriteEndObject();
			}
			wr.WriteEndObject();
		}

		// Replace if successful
		if (Path_.FileExists(filepath))
			File.Replace(filepath_tmp, filepath, null);
		else
			File.Move(filepath_tmp, filepath);
	}

	/// <summary>Load meta data from storage</summary>
	public void Load(string filepath)
	{
		// Read the json data in 'Filepath' into an object
		using var json_file = new FileStream(filepath, FileMode.Open);
		using var json = JsonDocument.Parse(json_file, new JsonDocumentOptions
		{
			AllowTrailingCommas = true,
			CommentHandling = JsonCommentHandling.Skip
		});
		
		WorkStreams.Clear();
		Epics.Clear();

		if (json.RootElement.TryGetProperty("Layout", out var layout))
		{
			if (layout.TryGetProperty(nameof(WorkStreams), out var workstreams))
			{
				foreach (var ws in workstreams.EnumerateArray())
				{
					var id = ws.GetProperty(nameof(WorkStream.ItemId)).GetInt32();
					var title = ws.GetProperty(nameof(WorkStream.Title)).GetString() ?? string.Empty;
					//var o2w = m4x4.Parse3x4(ws.GetProperty(nameof(WorkStream.O2W)).GetString() ?? string.Empty);
					
					WorkStreams.Add(new(id), new WorkStream(new(id), title));
				}
			}
			if (layout.TryGetProperty(nameof(Epics), out var epics))
			{
				foreach (var ep in epics.EnumerateArray())
				{
					var id = ep.GetProperty(nameof(Epic.ItemId)).GetInt32();
					var title = ep.GetProperty(nameof(Epic.Title)).GetString() ?? string.Empty;
					var parent_item_id = ep.GetProperty(nameof(Epic.ParentItemId)).GetInt64();
					//var o2w = m4x4.Parse3x4(ep.GetProperty(nameof(Epic.O2W)).GetString() ?? string.Empty);
					
					Epics.Add(new(id), new Epic(new(id), title, parent_item_id != 0 ? new(parent_item_id) : null));
				}
			}
		}

		UpdateLinks();
	}

	/// <summary>Repopulate the Links between items</summary>
	public void UpdateLinks()
	{
		Links.Clear();

		var all = All.ToDictionary(x => x.ItemId, x => x);
		foreach (var item in All)
		{
			if (item.ParentItemId is not ItemId parent_item_id) continue;
			if (!all.TryGetValue(parent_item_id, out var parent_item)) continue;

			// Create a connection
			static long MakeKey(ItemId id0, ItemId id1) => ((long)id0 << 32) | (uint)id1;
			Links.Add(MakeKey(parent_item.ItemId, item.ItemId), new(parent_item, item));
		}
	}
}
