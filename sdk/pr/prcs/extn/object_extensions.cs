using System.Linq;

namespace pr.extn
{
	public static class ObjectExtensions
	{
		/// <summary>Returns a string containing a description of this object and its member values</summary>
		public static string Dump(this object obj)
		{
			if (obj == null) return "null";
			var type = obj.GetType();
			return
				"{0}:\n".Fmt(type.Name) +
				string.Join("\n", type.GetProperties().Select(p => p.Name + ":  " + p.GetValue(obj, null)))+
				"\n";
		}
	}
}
