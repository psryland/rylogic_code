using System;
using System.Collections.Generic;
using System.Linq;

namespace Rylogic.Common
{
	/// <summary>A wrapper for a mime string</summary>
	public class Mime
	{
		/// <summary>A constant for the "*/*" mime string</summary>
		public static readonly Mime Unknown = new Mime();

		public Mime()
			: this("*", "*", false)
		{}
		public Mime(string mime_string, bool validate = true)
		{
			var parts = mime_string.ToLowerInvariant().Split('/');
			if (parts.Length != 2)
				throw new ArgumentException("Invalid mime string");

			Type = parts[0];
			SubType = parts[1];

			if (validate && Validate() is Exception err)
				throw err;
		}
		public Mime(string type, string subtype, bool validate = true)
		{
			Type = type;
			SubType = subtype;

			if (validate && Validate() is Exception err)
				throw err;
		}

		/// <summary>The media type part of the mime string (first part)</summary>
		public readonly string Type;

		/// <summary>The media format part of the mime string (second part)</summary>
		public readonly string SubType;

		/// <summary>Media type name</summary>
		public override string ToString() => $"{Type}/{SubType}";

		/// <summary>Implicit conversion to/from string</summary>
		public static implicit operator string(Mime mime)
		{
			return mime.ToString();
		}
		public static implicit operator Mime(string mime)
		{
			return new Mime(mime);
		}

		/// <summary>Parse a mime string</summary>
		public static Mime Parse(string mime_string)
		{
			return new Mime(mime_string);
		}

		/// <summary>Debugging helper for validating the mime values</summary>
		public Exception? Validate()
		{
			if (Type != "*" && !Types.Known.Contains(Type))
				return new Exception("MIME: unknown type");
			if (SubType != "*" && !SubTypes.Known.Contains(SubType))
				return new Exception("MIME: unknown subtype");
			return null;
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
					yield return f.GetRawConstantValue()!.ToString()!;
			}
		}

		/// <summary>The known mime sub-type values (extend as necessary)</summary>
		public static class SubTypes
		{
			public const string unknown = "*";

			// Text
			public const string plain = "plain";
			public const string rtf = "rtf";
			public const string html = "html";

			// Image
			public const string jpeg = "jpeg";
			public const string png = "png";
			public const string gif = "gif";
			public const string tiff = "tiff";

			// Video
			public const string mp4 = "mp4";
			public const string mpeg = "mpeg";
			public const string quicktime = "quicktime";
			public const string x_msvideo = "x-msvideo";
			public const string x_ms_asf = "x-ms-asf";
			public const string x_ms_wmv = "x-ms-wmv";
			public const string x_m4v = "x-m4v";
			public const string _3gpp = "3gpp";
			public const string mj2 = "mj2";

			// Audio
			public const string aac = "aac";
			public const string x_wav = "x-wav";
			public const string x_ms_wma = "x-ms-wma";
			public const string mp4a_latm = "mp4a-latm";
			public const string midi = "midi";

			// Application
			public const string json = "json";
			public const string url_encoded = "x-www-form-urlencoded";
			public const string msword = "msword";
			public const string pdf = "pdf";
			public const string postscript = "postscript";
			public const string vnd_ms_powerpoint = "vnd.ms-powerpoint";
			public const string x_shockwave_flash = "x-shockwave-flash";
			public const string vnd_ms_excel = "vnd.ms-excel";
			public const string xml = "xml";
			public const string zip = "zip";

			/// <summary>A set of the known subtypes</summary>
			public static readonly HashSet<string> Known = new HashSet<string>(Enumerate());
			private static IEnumerable<string> Enumerate()
			{
				foreach (var f in typeof(Types).GetFields().Where(x => x.FieldType == typeof(string)))
					yield return f.GetRawConstantValue()!.ToString()!;
			}
		}

		/// <summary>Helper method for converting a filename extension to a mime type (not complete!)</summary>
		public static Mime FromExtn(string extn)
		{
			return m_ExtnToMimeTypeMap.TryGetValue(extn.TrimStart('.'), out var mime) ? mime : new Mime();
		}

		/// <summary></summary>
		#region Equals
		public bool Equals(Mime mime)
		{
			return mime.Type == Type && mime.SubType == SubType;
		}
		public bool Equals(string mime)
		{
			return ToString() == mime;
		}
		public override bool Equals(object? obj)
		{
			if (obj is Mime mime) return Equals(mime);
			if (obj is string str) return Equals(str);
			return false;
		}
		public static bool operator ==(Mime lhs, Mime rhs)
		{
			return Equals(lhs, rhs);
		}
		public static bool operator !=(Mime lhs, Mime rhs)
		{
			return !Equals(lhs, rhs);
		}
		public override int GetHashCode()
		{
			return new { Type, SubType }.GetHashCode();
		}
		#endregion

		#region Mime type extension map

		/// <summary>These are the file extension types supported by magic bullet</summary>
		private static readonly Dictionary<string, Mime> m_ExtnToMimeTypeMap = new Dictionary<string, Mime>
		{
			// Text
			{"csv"  ,new Mime(Types.text, SubTypes.plain ,false)},
			{"log"  ,new Mime(Types.text, SubTypes.plain ,false)},
			{"txt"  ,new Mime(Types.text, SubTypes.plain ,false)},
			{"rtf"  ,new Mime(Types.text, SubTypes.rtf   ,false)},
			{"htm"  ,new Mime(Types.text, SubTypes.html  ,false)},
			{"html" ,new Mime(Types.text, SubTypes.html  ,false)},
			
			// Image
			{"jpg"  ,new Mime(Types.image, SubTypes.jpeg ,false)},
			{"jpeg" ,new Mime(Types.image, SubTypes.jpeg ,false)},
			{"png"  ,new Mime(Types.image, SubTypes.png  ,false)},
			{"gif"  ,new Mime(Types.image, SubTypes.gif  ,false)},
			{"tif"  ,new Mime(Types.image, SubTypes.tiff ,false)},
			{"tiff" ,new Mime(Types.image, SubTypes.tiff ,false)},
			
			// Video
			{"mp4"  ,new Mime(Types.video, SubTypes.mp4       ,false)},
			{"mpg"  ,new Mime(Types.video, SubTypes.mpeg      ,false)},
			{"mpeg" ,new Mime(Types.video, SubTypes.mpeg      ,false)},
			{"mov"  ,new Mime(Types.video, SubTypes.quicktime ,false)},
			{"avi"  ,new Mime(Types.video, SubTypes.x_msvideo ,false)},
			{"asf"  ,new Mime(Types.video, SubTypes.x_ms_asf  ,false)},
			{"wmv"  ,new Mime(Types.video, SubTypes.x_ms_wmv  ,false)},
			{"m4v"  ,new Mime(Types.video, SubTypes.x_m4v     ,false)},
			{"3gp"  ,new Mime(Types.video, SubTypes._3gpp     ,false)},
			{"3gp3" ,new Mime(Types.video, SubTypes._3gpp     ,false)},
			{"3gpp" ,new Mime(Types.video, SubTypes._3gpp     ,false)},
			{"3g2"  ,new Mime(Types.video, SubTypes._3gpp     ,false)},
			{"mj2"  ,new Mime(Types.video, SubTypes.mj2       ,false)},

			// Audio
			{"aac"  ,new Mime(Types.audio, SubTypes.aac       ,false)},
			{"mp3"  ,new Mime(Types.audio, SubTypes.mpeg      ,false)},
			{"wma"  ,new Mime(Types.audio, SubTypes.x_ms_wma  ,false)},
			{"mpa"  ,new Mime(Types.audio, SubTypes.mpeg      ,false)},
			{"m4a"  ,new Mime(Types.audio, SubTypes.mp4a_latm ,false)},
			{"wav"  ,new Mime(Types.audio, SubTypes.x_wav     ,false)},
			{"mid"  ,new Mime(Types.audio, SubTypes.midi      ,false)},
			{"midi" ,new Mime(Types.audio, SubTypes.midi      ,false)},
			
			// Application
			{"json" ,new Mime(Types.application, SubTypes.json              ,false)},
			{"doc"  ,new Mime(Types.application, SubTypes.msword            ,false)},
			{"docx" ,new Mime(Types.application, SubTypes.msword            ,false)},
			{"pdf"  ,new Mime(Types.application, SubTypes.pdf               ,false)},
			{"ps"   ,new Mime(Types.application, SubTypes.postscript        ,false)},
			{"ppt"  ,new Mime(Types.application, SubTypes.vnd_ms_powerpoint ,false)},
			{"pptx" ,new Mime(Types.application, SubTypes.vnd_ms_powerpoint ,false)},
			{"flv"  ,new Mime(Types.application, SubTypes.x_shockwave_flash ,false)},
			{"swf"  ,new Mime(Types.application, SubTypes.x_shockwave_flash ,false)},
			{"xls"  ,new Mime(Types.application, SubTypes.vnd_ms_excel      ,false)},
			{"xlsx" ,new Mime(Types.application, SubTypes.vnd_ms_excel      ,false)},
			{"xml"  ,new Mime(Types.application, SubTypes.xml               ,false)},
			{"zip"  ,new Mime(Types.application, SubTypes.zip               ,false)},
		};

		#endregion
	}
}