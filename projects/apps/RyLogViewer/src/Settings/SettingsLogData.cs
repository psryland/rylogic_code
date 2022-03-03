using System;
using Rylogic.Common;

namespace RyLogViewer.Options
{
	public class LogData :SettingsSet<LogData>
	{
		public LogData()
		{
			LineCacheCount = Constants.LineCacheCountDefault;
			FileBufSize = Constants.FileBufSizeDefault;
			MaxLineLength = Constants.MaxLineLengthDefault;
			OpenAtEnd = true;
			FileChangesAdditive = true;
			IgnoreBlankLines = false;
		}

		/// <summary></summary>
		public int LineCacheCount
		{
			get => get<int>(nameof(LineCacheCount));
			set => set(nameof(LineCacheCount), value);
		}

		/// <summary></summary>
		public int FileBufSize
		{
			get => get<int>(nameof(FileBufSize));
			set => set(nameof(FileBufSize), value);
		}

		/// <summary></summary>
		public int MaxLineLength
		{
			get => get<int>(nameof(MaxLineLength));
			set => set(nameof(MaxLineLength), value);
		}

		/// <summary></summary>
		public bool OpenAtEnd
		{
			get => get<bool>(nameof(OpenAtEnd));
			set => set(nameof(OpenAtEnd), value);
		}

		/// <summary></summary>
		public bool FileChangesAdditive
		{
			get => get<bool>(nameof(FileChangesAdditive));
			set => set(nameof(FileChangesAdditive), value);
		}

		/// <summary></summary>
		public bool IgnoreBlankLines
		{
			get => get<bool>(nameof(IgnoreBlankLines));
			set => set(nameof(IgnoreBlankLines), value);
		}

		/// <summary>Validate settings</summary>
		public override Exception? Validate()
		{
			// File buffer size
			int file_buf_size = FileBufSize;
			if (file_buf_size < Constants.FileBufSizeMin || file_buf_size > Constants.FileBufSizeMax)
				FileBufSize = Constants.FileBufSizeDefault;

			// Max line length
			int max_line_length = MaxLineLength;
			if (max_line_length < Constants.MaxLineLengthMin || max_line_length > Constants.MaxLineLengthMax)
				MaxLineLength = Constants.MaxLineLengthDefault;

			// Line cache count
			int line_cache_count = LineCacheCount;
			if (line_cache_count < Constants.LineCacheCountMin || line_cache_count > Constants.LineCacheCountMax)
				LineCacheCount = Constants.LineCacheCountDefault;

			return null;
		}
	}
}
