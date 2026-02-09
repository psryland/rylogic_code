using System;
using System.ComponentModel;
using System.IO;
using Rylogic.Common;

namespace LDraw
{
	public class ParsingProgressData :INotifyPropertyChanged
	{
		public ParsingProgressData(Guid context_id)
		{
			ContextId = context_id;
		}

		/// <summary>The context id associated with the parsing progress</summary>
		public Guid ContextId { get; }

		/// <summary>The name of the file/script currently being parsed</summary>
		public string DataSourceName
		{
			get => m_data_source_name ?? "Script";
			set
			{
				if (m_data_source_name == value) return;
				m_data_source_name = value;
				DataLength = Path_.FileExists(m_data_source_name) ? new FileInfo(m_data_source_name).Length : 0;
				NotifyPropertyChanged(nameof(DataSourceName));
			}
		}
		private string? m_data_source_name;

		/// <summary>The length of the data being parsed</summary>
		public long DataLength
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				NotifyPropertyChanged(nameof(Percentage));
				NotifyPropertyChanged(nameof(IsIndeterminate));
			}
		}

		/// <summary>Current position within the file</summary>
		public long DataOffset
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				NotifyPropertyChanged(nameof(Percentage));
			}
		}

		/// <summary>Percentage progress through the file</summary>
		public double Percentage => DataLength != 0 ? 100.0 * DataOffset / DataLength : 100.0 * (DataOffset % 0x1000) / 0x1000;

		/// <summary>True if we don't know the upper bound</summary>
		public bool IsIndeterminate => DataLength == 0;

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
