using System.Windows.Forms;
using pr.extn;

namespace pr.util
{
	/// <summary>A helper object for use with Control.Tag</summary>
	public class LoadSaveTag
	{
		public object OriginalTag;
		public bool Loading;
	}

	public static class LoadSave
	{
		/// <summary>
		/// Mark a control as 'loading' by assigning it's Tag property to an instance of the 'LoadSaveTag' class
		/// The original Tag value is preserved once the scope exits</summary>
		public static Scope MarkAsLoading(this Control ctrl)
		{
			return Scope.Create(
				() => ctrl.Tag = new LoadSaveTag{OriginalTag = ctrl.Tag, Loading = true},
				() => ctrl.Tag = ((LoadSaveTag)ctrl.Tag).OriginalTag);
		}

		/// <summary>
		/// Mark a control as 'saving' by assigning it's Tag property to an instance of the 'LoadSaveTag' class
		/// The original Tag value is preserved once the scope exits</summary>
		public static Scope MarkAsSaving(this Control ctrl)
		{
			return Scope.Create(
				() => ctrl.Tag = new LoadSaveTag{OriginalTag = ctrl.Tag, Loading = false},
				() => ctrl.Tag = ((LoadSaveTag)ctrl.Tag).OriginalTag);
		}

		/// <summary>True while this control is marked as loading</summary>
		public static bool IsLoading(this Control ctrl)
		{
			return ctrl.Tag is LoadSaveTag && ((LoadSaveTag)ctrl.Tag).Loading;
		}

		/// <summary>True while this control is marked as saving</summary>
		public static bool IsSaving(this Control ctrl)
		{
			return ctrl.Tag is LoadSaveTag && ((LoadSaveTag)ctrl.Tag).Loading == false;
		}
	}
}
