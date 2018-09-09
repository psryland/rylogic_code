using Rylogic.Extn;

namespace Rylogic.Common
{
	public static class Common_
	{
		/// <summary>Add XML serialisation support for graphics types</summary>
		public static XmlConfig SupportRylogicCommonTypes(this XmlConfig cfg)
		{
			Xml_.ToMap[typeof(Range)] = (obj, node) =>
			{
				var r = (Range)obj;
				node.SetValue($"{r.Beg} {r.End}");
				return node;
			};
			Xml_.AsMap[typeof(Range)] = (elem, type, instance) =>
			{
				return Range.Parse(elem.Value);
			};

			Xml_.ToMap[typeof(RangeF)] = (obj, node) =>
			{
				var r = (RangeF)obj;
				node.SetValue($"{r.Beg} {r.End}");
				return node;
			};
			Xml_.AsMap[typeof(RangeF)] = (elem, type, instance) =>
			{
				return RangeF.Parse(elem.Value);
			};

			return cfg;
		}
	}
}
