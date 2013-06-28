using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using pr.extn;
using pr.stream;
using pr.util;

namespace RyLogViewer
{
	public interface IFileSource :IDisposable
	{
		/// <summary>A string name describing the file</summary>
		string Name { get; }

		/// <summary>A full filepath that represents the associated files for use when generating output temp/export files</summary>
		string PsuedoFilepath { get; }

		/// <summary>Returns the full filepath for the file that contains byte offset 'offset'</summary>
		string FilepathAt(long offset);

		/// <summary>The filepaths associated with this file source</summary>
		IEnumerable<string> Filepaths { get; }

		/// <summary>The file stream to read from</summary>
		Stream Stream { get; }

		/// <summary>Open the associated files making 'Stream' valid</summary>
		IFileSource Open();

		/// <summary>Empty the contents of the log file. Returns null if successful</summary>
		Exception Clear();

		/// <summary>Returns a new instance of this file source, with Position set to 0</summary>
		IFileSource NewInstance();
	}

	public class SingleFile :IFileSource
	{
		/// <summary>The filepath that this file source represents</summary>
		private readonly string m_filepath;

		/// <summary>A string name describing the file</summary>
		public string Name { get { return Path.GetFileName(m_filepath); } }

		/// <summary>A full filepath that represents the associated files for use when generating output temp/export files</summary>
		public string PsuedoFilepath { get { return m_filepath; } }

		/// <summary>Returns the full filepath for the file that contains byte offset 'offset'</summary>
		public string FilepathAt(long offset) { return m_filepath; }

		/// <summary>The filepaths associated with this file source</summary>
		public IEnumerable<string> Filepaths { get { return Enumerable.Repeat(m_filepath,1); } }

		/// <summary>The file stream to read from</summary>
		public Stream Stream { get; private set; }

		/// <summary>Open the associated files making 'Stream' valid</summary>
		public IFileSource Open()
		{
			Dispose();
			Stream = new FileStream(m_filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite|FileShare.Delete, 0x1000, FileOptions.RandomAccess);
			return this;
		}

		/// <summary>Close and release files</summary>
		public void Dispose()
		{
			if (Stream != null) Stream.Dispose();
			Stream = null;
		}

		/// <summary>Empty the contents of the log file. Returns null if successful</summary>
		public Exception Clear()
		{
			Dispose();

			Exception err = null;

			// Try to open the file with write access and set its length to 0
			try
			{
				using (var f = new FileStream(m_filepath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite|FileShare.Delete))
					f.SetLength(0);
			}
			catch (Exception ex)
			{
				Log.Warn(this, "Failed to set log file '{0}'s length to zero.\nReason: {1}".Fmt(m_filepath, ex.Message));
				err = ex;
			}

			// Try swapping the file with an empty one
			if (err != null) try
			{
				// Create an empty file
				using (new FileStream(m_filepath+".tmp", FileMode.Create, FileAccess.Write)) {}

				// Try to 'hot-swap' the files
				File.Replace(m_filepath+".tmp", m_filepath, null);
			}
			catch (Exception ex)
			{
				Log.Warn(this, "Failed to replace file {0} with an empty file.\nReason: {1}".Fmt(m_filepath, ex.Message));
				err = ex;
			}

			Open();
			return err;
		}

		/// <summary>Returns a new instance of this file source, with Position set to 0</summary>
		public IFileSource NewInstance()
		{
			return new SingleFile(m_filepath);
		}

		public SingleFile(string filepath)
		{
			m_filepath = filepath;
		}
	}

	public class AggregateFile :IFileSource
	{
		/// <summary>The filepath that this file source represents</summary>
		private readonly List<string> m_filepaths;

		/// <summary>A string name describing the file</summary>
		public string Name { get; private set; }

		/// <summary>A full filepath that represents the associated files for use when generating output temp/export files</summary>
		public string PsuedoFilepath { get; private set; }

		/// <summary>Returns the full filepath for the file that contains byte offset 'offset'</summary>
		public string FilepathAt(long offset) 
		{
			Debug.Assert(Stream != null, "FileSource must be opened first");
			var fidx = Stream.FileIndexAtOffset(offset);
			return Stream.Files[fidx].FullName;
		}

		/// <summary>The filepaths associated with this file source</summary>
		public IEnumerable<string> Filepaths { get { return m_filepaths; } }

		/// <summary>The file stream to read from</summary>
		private AggregateFileStream Stream { get; set; }
		Stream IFileSource.Stream { get { return Stream; } }
		
		/// <summary>Open the associated files making 'Stream' valid</summary>
		public IFileSource Open()
		{
			Stream = new AggregateFileStream(m_filepaths, FileShare.ReadWrite|FileShare.Delete, file_options:FileOptions.RandomAccess);
			return this;
		}

		/// <summary>Empty the contents of the log file. Returns null if successful</summary>
		public Exception Clear()
		{
			return new NotSupportedException("Clearing aggregate files is not supported");
		}

		/// <summary>Returns a new instance of this file source, with Position set to 0</summary>
		public IFileSource NewInstance()
		{
			return new AggregateFile(m_filepaths);
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			if (Stream != null) Stream.Dispose();
			Stream = null;
		}

		public AggregateFile(IEnumerable<string> filepaths)
		{
			m_filepaths = new List<string>(filepaths);
			if (m_filepaths.Count == 0) throw new ArgumentException("Aggregate file sources cannot contain no files");

			// Determine a psuedo filepath and name
			var dir = Path.GetDirectoryName(m_filepaths[0]) ?? string.Empty;
			var name = new StringBuilder(Path.GetFileNameWithoutExtension(m_filepaths[0]));
			foreach (var f in m_filepaths.Skip(1))
			{
				var n = Path.GetFileNameWithoutExtension(f) ?? "logfile";
				name.Length = Math.Min(name.Length, n.Length);
				for (int i = 0; i < name.Length; ++i)
				{
					if (n[i] == name[i]) continue;
					name.Length = i;
				}
			}
			if (name.Length == 0) name.Append("log");
			Name = name.Append(".aggregated").Append(Path.GetExtension(m_filepaths[0])).ToString();
			PsuedoFilepath = Path.Combine(dir, Name);
		}
	}
}
