using System.Xml.Linq;

namespace PaulsBot
{
	public interface IMsg
	{
		XElement ToXml(XElement node);
	}
}
