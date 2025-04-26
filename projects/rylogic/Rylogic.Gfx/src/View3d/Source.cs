using System;
using System.ComponentModel;

namespace Rylogic.Gfx
{
	public sealed partial class View3d
	{
		/// <summary>Reference type wrapper of a view3d light</summary>
		public class Source :INotifyPropertyChanged
		{
			public Source(Guid context_id)
			{
				m_context_id = context_id;
			}

			/// <summary>The context Id associated with the source</summary>
			public Guid ContextId => m_context_id;
			private Guid m_context_id;

			/// <summary>The inner spot light cone angle (in degrees)</summary>
			public string Name
			{
				get => View3D_SourceNameGetBStr(ref m_context_id);
				set
				{
					if (Name == value) return;
					View3D_SourceNameSet(ref m_context_id, value);
					NotifyPropertyChanged(nameof(Name));
				}
			}

			/// <summary>The number of objects associated with this source</summary>
			public int ObjectCount
			{
				get => new SourceInfo(View3D_SourceInfo(ref m_context_id)).ObjectCount;
			}

			/// <summary>The filepath associated with this source (or null)</summary>
			public string? FilePath
			{
				get => new SourceInfo(View3D_SourceInfo(ref m_context_id)).FilePath;
			}

			/// <summary>Remove and clean up this data source</summary>
			public void Remove()
			{
				View3D_SourceDelete(ref m_context_id);
			}

			/// <summary>Notify property value changed</summary>
			public event PropertyChangedEventHandler? PropertyChanged;
			private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
