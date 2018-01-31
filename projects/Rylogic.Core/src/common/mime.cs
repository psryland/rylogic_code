using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rylogic.Common
{
	/// <summary>A wrapper for a mime string</summary>
	public class Mime
	{
		/// <summary>The media type part of the mime string (first part)</summary>
		public readonly string Type;

		/// <summary>The media format part of the mime string (second part)</summary>
		public readonly string SubType;

		public override string ToString() { return Type + '/' + SubType; }

		/// <summary>Implicit conversion to/from string</summary>
		public static implicit operator string(Mime mime) { return mime.ToString(); }
		public static implicit operator Mime(string mime) { return new Mime(mime); }

		/// <summary>A constant for the "*/*" mime string</summary>
		public static readonly Mime Unknown = new Mime();

		public Mime(string type = "*", string subtype = "*")
		{
			Type    = type;
			SubType = subtype;
			Validate();
		}

		public Mime(string mimeString)
		{
			var parts = mimeString.ToLowerInvariant().Split('/');
			if (parts.Length != 2) throw new ArgumentException("Invalid mime string");
			Type    = parts[0];
			SubType = parts[1];
			Validate();
		}

		/// <summary>A private constructor that doesn't do validation</summary>
		private Mime(string type, string subtype, int internalOnly)
		{
			Type = type;
			SubType = subtype;
		}

		/// <summary>Parse a mime string</summary>
		public static Mime Parse(string mimeString)
		{
			return new Mime(mimeString);
		}

		/// <summary>Debugging helper for validating the mime values</summary>
		[Conditional("DEBUG")] public void Validate()
		{
			Debug.Assert(Types.Known.Contains(Type), "unknown mime type");
			Debug.Assert(SubTypes.Known.Contains(SubType), "unknown mime subtype");
		}

		/// <summary>The known mime type values (extend as necessary)</summary>
		public static class Types
		{
			public const string unknown     = "*";
			public const string text        = "text";
			public const string image       = "image";
			public const string video       = "video";
			public const string audio       = "audio";
			public const string application = "application";

			/// <summary>A set of the known types</summary>
			public static readonly HashSet<string> Known = new HashSet<string>(Enumerate());
			private static IEnumerable<string> Enumerate()
			{
				foreach (var f in typeof(Types).GetFields().Where(x => x.FieldType == typeof(string)))
					yield return f.GetRawConstantValue().ToString();
			}
		}

		/// <summary>The known mime sub-type values (extend as necessary)</summary>
		public static class SubTypes
		{
			public const string unknown           = "*";
			public const string plain             = "plain";
			public const string rtf               = "rtf";
			public const string html              = "html";
			public const string jpeg              = "jpeg";
			public const string png               = "png";
			public const string gif               = "gif";
			public const string tiff              = "tiff";
			public const string x_wav             = "x-wav";
			public const string x_m4v             = "x-m4v";
			public const string x_ms_asf          = "x-ms-asf";
			public const string x_ms_wma          = "x-ms-wma";
			public const string x_ms_wmv          = "x-ms-wmv";
			public const string x_msvideo         = "x-msvideo";
			public const string x_shockwave_flash = "x-shockwave-flash";
			public const string quicktime         = "quicktime";
			public const string mp4               = "mp4";
			public const string _3gpp             = "3gpp";
			public const string mj2               = "mj2";
			public const string mpeg              = "mpeg";
			public const string aac               = "aac";
			public const string mp4a_latm         = "mp4a-latm";
			public const string midi              = "midi";
			public const string msword            = "msword";
			public const string pdf               = "pdf";
			public const string postscript        = "postscript";
			public const string vnd_ms_powerpoint = "vnd.ms-powerpoint";
			public const string vnd_ms_excel      = "vnd.ms-excel";
			public const string xml               = "xml";
			public const string zip               = "zip";

			/// <summary>A set of the known subtypes</summary>
			public static readonly HashSet<string> Known = new HashSet<string>(Enumerate());
			private static IEnumerable<string> Enumerate()
			{
				foreach (var f in typeof(Types).GetFields().Where(x => x.FieldType == typeof(string)))
					yield return f.GetRawConstantValue().ToString();
			}
		}

		/// <summary>Helper method for converting a filename extension to a mime type (not complete!)</summary>
		public static Mime FromExtn(string extn)
		{
			Mime mime;
			return m_ExtnToMimeTypeMap.TryGetValue(extn.TrimStart('.'), out mime) ? mime : new Mime();
		}

		#region Mime type extension map

		/// <summary>These are the file extension types supported by magic bullet</summary>
		private static readonly Dictionary<string, Mime> m_ExtnToMimeTypeMap = new Dictionary<string, Mime>
		{
			// Text
			{"csv"  ,new Mime(Types.text, SubTypes.plain ,0)},
			{"log"  ,new Mime(Types.text, SubTypes.plain ,0)},
			{"txt"  ,new Mime(Types.text, SubTypes.plain ,0)},
			{"rtf"  ,new Mime(Types.text, SubTypes.rtf   ,0)},
			{"htm"  ,new Mime(Types.text, SubTypes.html  ,0)},
			{"html" ,new Mime(Types.text, SubTypes.html  ,0)},
			
			// Image
			{"jpg"  ,new Mime(Types.image, SubTypes.jpeg ,0)},
			{"jpeg" ,new Mime(Types.image, SubTypes.jpeg ,0)},
			{"png"  ,new Mime(Types.image, SubTypes.png  ,0)},
			{"gif"  ,new Mime(Types.image, SubTypes.gif  ,0)},
			{"tif"  ,new Mime(Types.image, SubTypes.tiff ,0)},
			{"tiff" ,new Mime(Types.image, SubTypes.tiff ,0)},
			
			// Video
			{"avi"  ,new Mime(Types.video, SubTypes.x_msvideo ,0)},
			{"asf"  ,new Mime(Types.video, SubTypes.x_ms_asf  ,0)},
			{"m4v"  ,new Mime(Types.video, SubTypes.x_m4v     ,0)},
			{"mov"  ,new Mime(Types.video, SubTypes.quicktime ,0)},
			{"mp4"  ,new Mime(Types.video, SubTypes.mp4       ,0)},
			{"3gp"  ,new Mime(Types.video, SubTypes._3gpp     ,0)},
			{"3gp3" ,new Mime(Types.video, SubTypes._3gpp     ,0)},
			{"3gpp" ,new Mime(Types.video, SubTypes._3gpp     ,0)},
			{"3g2"  ,new Mime(Types.video, SubTypes._3gpp     ,0)},
			{"mj2"  ,new Mime(Types.video, SubTypes.mj2       ,0)},
			{"wmv"  ,new Mime(Types.video, SubTypes.x_ms_wmv  ,0)},
			{"mpg"  ,new Mime(Types.video, SubTypes.mpeg      ,0)},
			{"mpeg" ,new Mime(Types.video, SubTypes.mpeg      ,0)},

			// Audio
			{"mp3"  ,new Mime(Types.audio, SubTypes.mpeg      ,0)},
			{"wma"  ,new Mime(Types.audio, SubTypes.x_ms_wma  ,0)},
			{"mpa"  ,new Mime(Types.audio, SubTypes.mpeg      ,0)},
			{"aac"  ,new Mime(Types.audio, SubTypes.aac       ,0)},
			{"m4a"  ,new Mime(Types.audio, SubTypes.mp4a_latm ,0)},
			{"wav"  ,new Mime(Types.audio, SubTypes.x_wav     ,0)},
			{"mid"  ,new Mime(Types.audio, SubTypes.midi      ,0)},
			{"midi" ,new Mime(Types.audio, SubTypes.midi      ,0)},
			
			// Application
			{"doc"  ,new Mime(Types.application, SubTypes.msword            ,0)},
			{"docx" ,new Mime(Types.application, SubTypes.msword            ,0)},
			{"pdf"  ,new Mime(Types.application, SubTypes.pdf               ,0)},
			{"ps"   ,new Mime(Types.application, SubTypes.postscript        ,0)},
			{"ppt"  ,new Mime(Types.application, SubTypes.vnd_ms_powerpoint ,0)},
			{"pptx" ,new Mime(Types.application, SubTypes.vnd_ms_powerpoint ,0)},
			{"flv"  ,new Mime(Types.application, SubTypes.x_shockwave_flash ,0)},
			{"swf"  ,new Mime(Types.application, SubTypes.x_shockwave_flash ,0)},
			{"xls"  ,new Mime(Types.application, SubTypes.vnd_ms_excel      ,0)},
			{"xlsx" ,new Mime(Types.application, SubTypes.vnd_ms_excel      ,0)},
			{"xml"  ,new Mime(Types.application, SubTypes.xml               ,0)},
			{"zip"  ,new Mime(Types.application, SubTypes.zip               ,0)},
		};

		#endregion

	}
}