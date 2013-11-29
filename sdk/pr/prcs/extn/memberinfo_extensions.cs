using System;
using System.Linq;
using System.Reflection;

namespace pr.extn
{
	public static class MemberInfoExtensions
	{
		/// <summary>Returns an attribute associated with a member or null</summary>
		public static T GetAttribute<T>(this MemberInfo mi, bool inherit = true) where T:Attribute
		{
			return (T)mi.GetCustomAttributes(typeof(T), inherit).FirstOrDefault();
		}
	}
}
