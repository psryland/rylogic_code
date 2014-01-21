using System.Windows.Forms;

namespace pr.common
{
	/// <summary>Type-safe version of BindingSource</summary>
	public class BindingSource<T> :BindingSource
	{
		public new T Current
		{
			get { return (T)base.Current; }
		}
	}
}
