using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using pr.util;

namespace RyLogViewer
{
	/// <summary>An action that occurs in responce to a click on a row that matches a pattern</summary>
	public class ClkAction :Pattern
	{
		/// <summary>The program to launch when activated</summary>
		public string Executable { get; set; }
		
		/// <summary>Arguments passed to the program to launch</summary>
		public string Arguments { get; set; }
		
		/// <summary>The working directory of the launched program</summary>
		public string WorkingDirectory { get; set; }
		
		/// <summary>Return a string description of the acton</summary>
		public string ActionString { get { return Executable + " " + Arguments; } }
		
		public ClkAction()
		{
			Executable       = "";
			Arguments        = "";
			WorkingDirectory = "";
		}
		public ClkAction(ClkAction rhs) :base(rhs)
		{
			Executable       = rhs.Executable;
			Arguments        = rhs.Arguments;
			WorkingDirectory = rhs.WorkingDirectory;
		}
		
		/// <summary>Construct from xml description</summary>
		public ClkAction(XElement node) :base(node)
		{
			// ReSharper disable PossibleNullReferenceException
			Executable       = node.Element(XmlTag.Executable).Value;
			Arguments        = node.Element(XmlTag.Arguments ).Value;
			WorkingDirectory = node.Element(XmlTag.WorkingDir).Value;
			// ReSharper restore PossibleNullReferenceException
		}

		/// <summary>Performs the click action using values 'text'</summary>
		public void Execute(string text)
		{
			// Substitute the capture groups into the arguments string
			var grps = CaptureGroups(text);
			string args = Arguments;
			foreach (var c in grps)
				args = args.Replace("{"+c.Key+"}", c.Value);

			// Create the process
			ProcessStartInfo info = new ProcessStartInfo
			{
				UseShellExecute        = false,
				FileName               = Executable,
				Arguments              = args,
				WorkingDirectory       = WorkingDirectory
			};
			Process.Start(info);
		}

		/// <summary>Export this ClkAction as xml</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add
			(
				new XElement(XmlTag.Executable, Executable),
				new XElement(XmlTag.Arguments , Arguments ),
				new XElement(XmlTag.WorkingDir, WorkingDirectory)
			);
			return node;
		}
		
		/// <summary>Reads an xml description of the ClkAction expressions</summary>
		public static List<ClkAction> Import(string actions)
		{
			var list = new List<ClkAction>();
			
			XDocument doc;
			try { doc = XDocument.Parse(actions); } catch { return list; }
			if (doc.Root == null) return list;
			foreach (XElement n in doc.Root.Elements(XmlTag.ClkAction))
				try { list.Add(new ClkAction(n)); } catch {} // Ignore those that fail
			
			return list;
		}
		
		/// <summary>Serialise the ClkAction patterns to xml</summary>
		public static string Export(List<ClkAction> ClkActions)
		{
			XDocument doc = new XDocument(new XElement(XmlTag.Root));
			if (doc.Root == null) return "";
			
			foreach (var ac in ClkActions)
				doc.Root.Add(ac.ToXml(new XElement(XmlTag.ClkAction)));
			
			return doc.ToString(SaveOptions.None);
		}
		
		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		public override object Clone()
		{
			return MemberwiseClone();
		}

		/// <summary>Value equality test</summary>
		public override bool Equals(object obj)
		{
			ClkAction rhs = obj as ClkAction;
			return 
				rhs != null &&
				base.Equals(obj) &&
				Executable      .Equals(rhs.Executable      ) &&
				Arguments       .Equals(rhs.Arguments       ) &&
				WorkingDirectory.Equals(rhs.WorkingDirectory);
		}

		/// <summary>Value hash code</summary>
		public override int GetHashCode()
		{
			return
				base.GetHashCode()^
				Executable      .GetHashCode()^
				Arguments       .GetHashCode()^
				WorkingDirectory.GetHashCode();
		}
	}
}
