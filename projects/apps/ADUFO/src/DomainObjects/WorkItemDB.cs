using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ADUFO.DomainObjects;
using Rylogic.Common;
using Rylogic.Maths;
using Rylogic.Utility;

namespace ADUFO;

public class WorkItemDB : IDisposable
{
	public WorkItemDB(Settings settings)
	{
		Settings = settings;
		WorkStreams = new Dictionary<int, WorkStream>();
		Epics = new Dictionary<int, Epic>();
		Links = new Dictionary<long, Link>();
	}
	public void Dispose()
	{
		Util.DisposeRange(WorkStreams.Values);
		Util.DisposeRange(Epics.Values);
	}

	/// <summary>App settings</summary>
	private Settings Settings { get; }

	/// <summary>Location to store layout</summary>
	public string Filepath => Util.ResolveAppDataPath("Rylogic", Util.AppProductName, Path_.SanitiseFileName($"{Settings.Organization}-{Settings.Project}.json"));

	/// <summary>Load meta data from storage</summary>
	public void Load()
	{
		var json = new ReadOnlySpan<byte>(File.ReadAllBytes(Filepath));
		var rd = new Utf8JsonReader(json, new JsonReaderOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });

		try
		{
			rd.Expect(JsonTokenType.None);
			rd.Expect(JsonTokenType.StartObject);
			for (; rd.GetPropertyName(out var prop);)
			{
				switch (prop)
				{
					case nameof(WorkStreams):
					{
						LoadWorkStreams(rd);
						break;
					}
					default:
					{
						rd.Skip();
						break;
					}
				}
			}
			rd.Expect(JsonTokenType.EndObject);
		}
		catch (Exception)
		{
		}

		// Handlers
		void LoadWorkStreams(Utf8JsonReader rd)
		{
			rd.Expect(JsonTokenType.StartArray);
			for (; !rd.IsToken(JsonTokenType.EndArray);) LoadWorkStream(rd);
			rd.Expect(JsonTokenType.EndArray);
		}
		void LoadWorkStream(Utf8JsonReader rd)
		{
			int? id = null;
			m4x4? o2w = null;

			rd.Expect(JsonTokenType.StartObject);
			for (; rd.GetPropertyName(out var prop);)
			{
				switch (prop)
				{
					case nameof(WorkStream.Id):
					{
						id = rd.GetInt32();
						break;
					}
					case nameof(WorkStream.O2W):
					{
						o2w = m4x4.Parse3x4(rd.GetString() ?? string.Empty);
						break;
					}
				}
			}
			rd.Expect(JsonTokenType.EndObject);

			// Update the work stream
			if (id != null && WorkStreams.TryGetValue(id.Value, out var ws))
			{
				if (o2w != null)
					ws.O2W = o2w.Value;

			}
		}
	}

	/// <summary>Persist items to storage</summary>
	public void Save()
	{
		// Write to a temp file
		var filepath_tmp = $"{Filepath}.tmp";
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
					foreach (var kv in WorkStreams)
					{
						wr.WriteStartObject();
						wr.WriteNumber(JsonEncodedText.Encode(nameof(kv.Value.Id)), kv.Key);
						wr.WriteString(JsonEncodedText.Encode(nameof(kv.Value.O2W)), kv.Value.O2W.ToString3x4());
						wr.WriteEndObject();
					}
					wr.WriteEndArray();

					// Epics

					// Features

					// User Stories

					// Tasks
				}
				wr.WriteEndObject();
			}
			wr.WriteEndObject();
		}

		// Replace if successful
		if (Path_.FileExists(Filepath))
			File.Replace(filepath_tmp, Filepath, null);
		else
			File.Move(filepath_tmp, Filepath);
	}

	/// <summary>Work Stream items</summary>
	public Dictionary<int, WorkStream> WorkStreams { get; }

	/// <summary>Work Stream items</summary>
	public Dictionary<int, Epic> Epics { get; }

	/// <summary>The connections between objects</summary>
	public Dictionary<long, Link> Links { get; }
}
