using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using pr.stream;
using pr.extn;

namespace pr.gfx
{
	public static class Exif
	{
		// ReSharper disable UnusedMember.Local
		/// <summary>A file marker used to identify jpg files</summary>
		private const ushort JpgIdentifierTag    = 0xFFD8;
		private const byte   JpgIdentifierTag_b0 = 0xFF;  // jpg start byte
		private const byte   JpgIdentifierTag_b1 = 0xD8;  // jpg start+1 byte
		private const byte   JpgIdentifierTag_e0 = 0xFF;  // jpg end-1 byte
		private const byte   JpgIdentifierTag_e1 = 0xD9;  // jpg end byte
		private const ushort TiffDataIdentifier  = 0x002A;
		private const string ExifIdentifier      = "Exif";
		private const string IntelIdentifier     = "II";
		private const string MotorolaIdentifier  = "MM";
		private const byte ExifStartMarker       = 0xFF;
		private const byte ExifCountMarker       = 0xE1;
		// ReSharper restore UnusedMember.Local

		/// <summary>All Exif tags as per the Exif standard 2.2, JEITA CP-2451</summary>
		public enum Tag :ushort
		{
			// IFD0 items
			ImageWidth                  = 0x100,
			ImageLength                 = 0x101,
			BitsPerSample               = 0x102,
			Compression                 = 0x103,
			PhotometricInterpretation   = 0x106,
			ImageDescription            = 0x10E,
			Make                        = 0x10F,
			Model                       = 0x110,
			StripOffsets                = 0x111,
			Orientation                 = 0x112,
			SamplesPerPixel             = 0x115,
			RowsPerStrip                = 0x116,
			StripByteCounts             = 0x117,
			XResolution                 = 0x11A,
			YResolution                 = 0x11B,
			PlanarConfiguration         = 0x11C,
			ResolutionUnit              = 0x128,
			TransferFunction            = 0x12D,
			Software                    = 0x131,
			DateTime                    = 0x132,
			Artist                      = 0x13B,
			WhitePoint                  = 0x13E,
			PrimaryChromaticities       = 0x13F,
			JPEGInterchangeFormat       = 0x201,
			JPEGInterchangeFormatLength = 0x202,
			YCbCrCoefficients           = 0x211,
			YCbCrSubSampling            = 0x212,
			YCbCrPositioning            = 0x213,
			ReferenceBlackWhite         = 0x214,
			Copyright                   = 0x8298,
			
			SubIFDOffset             = 0x8769,
			GPSIFDOffset             = 0x8825,
			
			// SubIFD items
			ExposureTime             = 0x829A,
			FNumber                  = 0x829D,
			ExposureProgram          = 0x8822,
			SpectralSensitivity      = 0x8824,
			ISOSpeedRatings          = 0x8827,
			OECF                     = 0x8828,
			ExifVersion              = 0x9000,
			DateTimeOriginal         = 0x9003,
			DateTimeDigitized        = 0x9004,
			ComponentsConfiguration  = 0x9101,
			CompressedBitsPerPixel   = 0x9102,
			ShutterSpeedValue        = 0x9201,
			ApertureValue            = 0x9202,
			BrightnessValue          = 0x9203,
			ExposureBiasValue        = 0x9204,
			MaxApertureValue         = 0x9205,
			SubjectDistance          = 0x9206,
			MeteringMode             = 0x9207,
			LightSource              = 0x9208,
			Flash                    = 0x9209,
			FocalLength              = 0x920A,
			SubjectArea              = 0x9214,
			MakerNote                = 0x927C,
			UserComment              = 0x9286,
			SubsecTime               = 0x9290,
			SubsecTimeOriginal       = 0x9291,
			SubsecTimeDigitized      = 0x9292,
			FlashpixVersion          = 0xA000,
			ColorSpace               = 0xA001,
			PixelXDimension          = 0xA002,
			PixelYDimension          = 0xA003,
			RelatedSoundFile         = 0xA004,
			FlashEnergy              = 0xA20B,
			SpatialFrequencyResponse = 0xA20C,
			FocalPlaneXResolution    = 0xA20E,
			FocalPlaneYResolution    = 0xA20F,
			FocalPlaneResolutionUnit = 0xA210,
			SubjectLocation          = 0xA214,
			ExposureIndex            = 0xA215,
			SensingMethod            = 0xA217,
			FileSource               = 0xA300,
			SceneType                = 0xA301,
			CFAPattern               = 0xA302,
			CustomRendered           = 0xA401,
			ExposureMode             = 0xA402,
			WhiteBalance             = 0xA403,
			DigitalZoomRatio         = 0xA404,
			FocalLengthIn35mmFilm    = 0xA405,
			SceneCaptureType         = 0xA406,
			GainControl              = 0xA407,
			Contrast                 = 0xA408,
			Saturation               = 0xA409,
			Sharpness                = 0xA40A,
			DeviceSettingDescription = 0xA40B,
			SubjectDistanceRange     = 0xA40C,
			ImageUniqueID            = 0xA420,

			// GPS subifd items
			GPSVersionID        = 0x0,
			GPSLatitudeRef      = 0x1,
			GPSLatitude         = 0x2,
			GPSLongitudeRef     = 0x3,
			GPSLongitude        = 0x4,
			GPSAltitudeRef      = 0x5,
			GPSAltitude         = 0x6,
			GPSTimestamp        = 0x7,
			GPSSatellites       = 0x8,
			GPSStatus           = 0x9,
			GPSMeasureMode      = 0xA,
			GPSDOP              = 0xB,
			GPSSpeedRef         = 0xC,
			GPSSpeed            = 0xD,
			GPSTrackRef         = 0xE,
			GPSTrack            = 0xF,
			GPSImgDirectionRef  = 0x10,
			GPSImgDirection     = 0x11,
			GPSMapDatum         = 0x12,
			GPSDestLatitudeRef  = 0x13,
			GPSDestLatitude     = 0x14,
			GPSDestLongitudeRef = 0x15,
			GPSDestLongitude    = 0x16,
			GPSDestBearingRef   = 0x17,
			GPSDestBearing      = 0x18,
			GPSDestDistanceRef  = 0x19,
			GPSDestDistance     = 0x1A,
			GPSProcessingMethod = 0x1B,
			GPSAreaInformation  = 0x1C,
			GPSDateStamp        = 0x1D,
			GPSDifferential     = 0x1E
		}

		/// <summary>Data types used in the Exif tag records</summary>
		public enum TiffDataType :byte
		{
			UByte        = 1,
			AsciiStrings = 2,
			UShort       = 3,
			ULong        = 4,
			URational    = 5,
			SByte        = 6,
			Undefined    = 7,
			SShort       = 8,
			SLong        = 9,
			SRational    = 10,
			Single       = 11,
			Double       = 12,
		}

		/// <summary>Image compression types</summary>
		public enum CompressiongType
		{
			Uncompressed                   = 1    ,
			CCITT_1D                       = 2    ,
			T4_Group3_Fax                  = 3    ,
			T6_Group4_Fax                  = 4    ,
			LZW                            = 5    ,
			JPEG_Old                       = 6    ,
			JPEG1                          = 7    ,
			AdobeDeflate                   = 8    ,
			JBIG_BW                        = 9    ,
			JBIGColor                      = 10   ,
			JPEG2                          = 99   ,
			Kodak262                       = 262  ,
			Next                           = 32766,
			SonyARWCompressed              = 32767,
			PackedRAW                      = 32769,
			SamsungSRWCompressed           = 32770,
			CCIRLEW                        = 32771,
			PackBits                       = 32773,
			Thunderscan                    = 32809,
			KodakKDCCompressed             = 32867,
			IT8CTPAD                       = 32895,
			IT8LW                          = 32896,
			IT8MP                          = 32897,
			IT8BL                          = 32898,
			PixarFilm                      = 32908,
			PixarLog                       = 32909,
			Deflate                        = 32946,
			DCS                            = 32947,
			JBIG                           = 34661,
			SGILog                         = 34676,
			SGILog24                       = 34677,
			JPEG2000                       = 34712,
			NikonNEFCompressed             = 34713,
			JBIG2_TIFF_FX                  = 34715,
			MDI_BinaryLevelCodec           = 34718,
			MDI_ProgressiveTransformCodec  = 34719,
			MDI_Vector                     = 34720,
			LossyJPEG                      = 34892,
			KodakDCRCompressed             = 65000,
			PentaxPEFCompressed            = 65535,
		}

		/// <summary>Return the size in bytes corresponding to a data type</summary>
		public static int SizeInBytes(TiffDataType dt)
		{
			switch (dt)
			{
			default: throw new ArgumentOutOfRangeException("dt",dt,"Unknown Tiff data type");
			case TiffDataType.UByte:
			case TiffDataType.AsciiStrings:
			case TiffDataType.SByte:
				return 1;
			case TiffDataType.UShort:
			case TiffDataType.SShort:
				return 2;
			case TiffDataType.ULong:
			case TiffDataType.SLong:
			case TiffDataType.Single:
			case TiffDataType.Undefined:
				return 4;
			
			case TiffDataType.URational:
			case TiffDataType.SRational:
			case TiffDataType.Double:
				return 8;
			}
		}

		/// <summary>Exceptions raised by this library</summary>
		public class Exception :System.Exception
		{
			public Exception() {}
			public Exception(string message) :base(message) {}
			public Exception(string message, Exception innerException) :base(message, innerException) {}
		}

		public interface IExifData
		{
			/// <summary>Returns true if 'tag' is in the exif data</summary>
			bool HasTag(Tag tag);

			/// <summary>Returns the raw exif data associated with a tag, or null if the tag is not available</summary>
			byte[] this[Tag tag] { get; }

			/// <summary>Jpg thumbnail data</summary>
			byte[] Thumbnail { get; }
		}

		private interface IExifDataInternal :IExifData
		{
			/// <summary>Read and store info about an exif record</summary>
			void ReadRecord(BinaryReaderEx br, long tiff_header_start);

			/// <summary>Read and store info about an image thumbnail</summary>
			void ReadThumbnail(BinaryReaderEx br, int length);
		}

		/// <summary>The Exif data read into memory from a jpg image</summary>
		private class Data :IExifData ,IExifDataInternal
		{
			/// <summary>The main table of exif data</summary>
			private readonly Dictionary<Tag, byte[]> m_IDF = new Dictionary<Tag, byte[]>();
			private readonly static byte[] m_NullTag = new byte[8];

			/// <summary>Returns true if 'tag' is in the exif data</summary>
			public bool HasTag(Tag tag) { return m_IDF.ContainsKey(tag); }

			/// <summary>Returns the raw exif data associated with a tag, or null if the tag is not available</summary>
			public byte[] this[Tag tag] { get { byte[] val; return m_IDF.TryGetValue(tag, out val) ? val : m_NullTag; } }

			/// <summary>Jpg thumbnail data</summary>
			public byte[] Thumbnail { get; private set; }

			/// <summary>Read and store info about an exif record</summary>
			public void ReadRecord(BinaryReaderEx br, long tiff_header_start)
			{
				var record = ParseExifRecord(br, tiff_header_start);
				m_IDF[record.Item1] = record.Item2;
			}

			/// <summary>Read and store info about an image thumbnail</summary>
			public void ReadThumbnail(BinaryReaderEx br, int length)
			{
				// Read the jpg thumbnail data
				byte[] thumb = new byte[length];
				br.BaseStream.Read(thumb, 0, thumb.Length);
				Thumbnail = thumb;
			}
		}

		/// <summary>An index for the exif data in a jpg image</summary>
		private class Index :IExifDataInternal
		{
			private readonly Dictionary<Tag, long> m_IDF = new Dictionary<Tag, long>();
			private readonly static byte[] m_NullTag = new byte[8];
			private readonly BinaryReaderEx m_br;
			private long m_tiff_header_start;
			private long m_thumbnail_offset;
			private int m_thumbnail_length;
			
			public Index(Stream stream) { m_br = new BinaryReaderEx(stream); }

			/// <summary>Returns true if 'tag' is in the exif data</summary>
			public bool HasTag(Tag tag) { return m_IDF.ContainsKey(tag); }

			/// <summary>Returns the raw exif data associated with a tag, or null if the tag is not available</summary>
			public byte[] this[Tag tag]
			{
				get
				{
					long ofs;
					if (!m_IDF.TryGetValue(tag, out ofs)) return m_NullTag;
					long pos = m_br.BaseStream.Position;
					try
					{
						m_br.BaseStream.Position = ofs;
						return ParseExifRecord(m_br, m_tiff_header_start).Item2;
					}
					finally { m_br.BaseStream.Position = pos; }
				}
			}

			/// <summary>Jpg thumbnail data</summary>
			public byte[] Thumbnail
			{
				get
				{
					if (m_thumbnail_length == 0) return null;
					return m_br.ReadBytes(m_thumbnail_offset, m_thumbnail_length);
				}
			}

			/// <summary>Read and store info about an exif record</summary>
			public void ReadRecord(BinaryReaderEx br, long tiff_header_start)
			{
				m_br.LittleEndian = br.LittleEndian;
				m_tiff_header_start = tiff_header_start;
				var tag = (Tag)br.ReadUShort();
				m_IDF[tag] = br.BaseStream.Position - 2;
				br.BaseStream.Position += 10;
			}

			/// <summary>Read and store info about an image thumbnail</summary>
			public void ReadThumbnail(BinaryReaderEx br, int length)
			{
				m_thumbnail_offset = br.BaseStream.Position;
				m_thumbnail_length = length;
				br.BaseStream.Position += length;
			}
		}

		/// <summary>Load the Exif data from a Jpg image into memory</summary>
		public static IExifData Load(string filepath, bool load_gps, bool load_thumbnail)
		{
			using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
				return Load(fs, load_gps, load_thumbnail);
		}

		/// <summary>Load the Exif data from a Jpg image into memory. Note this method does not close the stream</summary>
		public static IExifData Load(Stream stream, bool load_gps, bool load_thumbnail)
		{
			return Parse(stream, new Data(), load_gps, load_thumbnail);
		}

		/// <summary>Reads the exif data in a jpg creating an index of the tags and their byte offsets into the file</summary>
		public static IExifData Read(Stream stream, bool load_gps, bool load_thumbnail)
		{
			return Parse(stream, new Index(stream), load_gps, load_thumbnail);
		}

		/// <summary>Parse a jpg image for its exif data</summary>
		private static IExifDataInternal Parse(Stream stream, IExifDataInternal data, bool load_gps, bool load_thumbnail)
		{
			if (stream == null) throw new ArgumentNullException("stream", "Null Jpg stream");
			if (!stream.CanSeek) throw new Exception("Loading Exif data requires a seek-able stream");
			
			using (var br = new BinaryReaderEx(new UncloseableStream(stream)))
			{
				// JPEG encoding uses big endian (i.e. Motorola) byte aligns.
				// The TIFF encoding found later in the document will specify
				// the byte aligns used for the rest of the document.
				br.LittleEndian = false;
				
				// Check that this is a Jpg file
				if (br.ReadUShort() != JpgIdentifierTag)
					throw new Exception("Source is not a Jpg data");
				
				// The file has a number of blocks (Exif/JFIF), each of which has a tag number
				// followed by a length. We scan the document until the required tag (0xFFE1)
				// is found. All tags start with FF, so a non FF tag indicates an error.
				
				// Scan to the start of the Exif data
				byte s, c = 0;
				for (; (s = br.ReadByte()) == ExifStartMarker && (c = br.ReadByte()) != ExifCountMarker;)
					br.BaseStream.Seek(br.ReadUShort() - 2, SeekOrigin.Current);
				if (s != ExifStartMarker || c != ExifCountMarker)
					return data; // No Exif data found, return an empty data object
				
				// Populate the Exif.Data
				
				// The next 2 bytes are the size of the Exif data.
				var exif_data_size = br.ReadUShort();
				if (exif_data_size == 0) return data;
				
				// Next is the Exif data itself. It starts with the ASCII "Exif" followed by 2 zero bytes.
				if (br.ReadString(4) != ExifIdentifier) throw new Exception("Exif data not found");
				if (br.ReadByte() != 0 || br.ReadByte() != 0) throw new Exception("Malformed Exif data");
				
				// We're now into the TIFF format
				// Here the byte alignment may be different. II for Intel, MM for Motorola
				var tiff_hdr_start = br.BaseStream.Position;
				br.LittleEndian = br.ReadString(2) == IntelIdentifier;
				
				// Next 2 bytes are always the same.
				if (br.ReadUShort() != TiffDataIdentifier)
					throw new Exception("Error in TIFF data");
				
				// Get the offset to the main IFD (image file directory)
				uint ifd0_offset = br.ReadUInt();
				
				// Note that this offset is from the first byte of the TIFF header. Jump to the IFD.
				br.BaseStream.Position = tiff_hdr_start + ifd0_offset;
				
				// Parse this first IFD (there will be another IFD)
				ParseExifTable(br, data, tiff_hdr_start);
				
				// The address to the IFD1 (the thumbnail IFD) is located immediately after the main IFD. Save this for later
				var ifd1_offset = br.ReadUInt();
				
				// There's more data stored in the sub IFD, the offset to which is found in tag 0x8769.
				// As with all TIFF offsets, it will be relative to the first byte of the TIFF header.
				if (data.HasTag(Tag.SubIFDOffset))
				{
					// Jump to the exif SubIFD and add that to the data as well
					uint offset = data[Tag.SubIFDOffset].AsUInt32();
					br.BaseStream.Position = tiff_hdr_start + offset;
					ParseExifTable(br, data, tiff_hdr_start);
				}
				
				// Look for the optional GPS exif data
				if (load_gps && data.HasTag(Tag.GPSIFDOffset))
				{
					uint offset = data[Tag.GPSIFDOffset].AsUInt32();
					br.BaseStream.Position = tiff_hdr_start + offset;
					ParseExifTable(br, data, tiff_hdr_start);
				}
				
				// Finally, catalogue the thumbnail IFD if it's present
				if (load_thumbnail && ifd1_offset != 0)
				{
					var tmp = new Data();
					br.BaseStream.Position = tiff_hdr_start + ifd1_offset;
					ParseExifTable(br, tmp, tiff_hdr_start);
					ParseJpgThumbnail(br, data, tmp);
				}
			}
			
			return data;
		}

		/// <summary>Parses an IDF table from 'br' into 'data'. Assumes 'br' is positioned at the start of the table</summary>
		private static void ParseExifTable(BinaryReaderEx br, IExifDataInternal exif_data, long tiff_header_start)
		{
			// First 2 bytes is the number of entries in this IFD
			for (ushort i = 0, iend = br.ReadUShort(); i < iend; i++)
				exif_data.ReadRecord(br, tiff_header_start);
		}

		/// <summary>
		/// Parse a single exif entry.
		/// Assumes 'br' is positioned at the start of the record.
		/// 'br' is moved to the start of the next record on return</summary>
		private static Tuple<Tag,byte[]> ParseExifRecord(BinaryReaderEx br, long tiff_header_start)
		{
			// Read the exif field data. Each entry is 12 bytes
			var tag   = (Tag)br.ReadUShort();
			var dt    = (TiffDataType)br.ReadUShort();
			var count = br.ReadUInt();
			byte[] data;
			
			// If the total space taken up by the field is longer than the
			// 4 bytes afforded by the tag data, the tag data will contain
			// an offset to the actual data.
			var data_size = (int)count * SizeInBytes(dt);
			if (data_size > 4)
			{
				var ofs = br.ReadUShort();
				data = br.ReadBytes(tiff_header_start + ofs, data_size);
				br.ReadByte();
				br.ReadByte(); // pad to 4 bytes
			}
			else
			{
				data  = br.ReadBytes(4);
			}
			return new Tuple<Tag, byte[]>(tag,data);
		}

		/// <summary>
		/// Retrieves a JPEG thumbnail from the image if one is present. Note that this method cannot retrieve thumbnails encoded in other formats,
		/// but since the DCF specification specifies that thumbnails must be JPEG, this method will be sufficient for most purposes
		/// See http://gvsoft.homedns.org/exif/exif-explanation.html#TIFFThumbs or http://partners.adobe.com/public/developer/en/tiff/TIFF6.pdf for 
		/// details on the encoding of TIFF thumbnails</summary>
		private static void ParseJpgThumbnail(BinaryReaderEx br, IExifDataInternal exif_data, Data tags)
		{
			if (!tags.HasTag(Tag.Compression) ||
				!tags.HasTag(Tag.JPEGInterchangeFormat) ||
				!tags.HasTag(Tag.JPEGInterchangeFormatLength))
				return;
			
			// Get the thumbnail encoding. This method only handles JPEG thumbnails (compression type 6)
			var compression = (CompressiongType)tags[Tag.Compression].AsUInt16();
			if (compression != CompressiongType.JPEG_Old)
				return;
			
			// Get the location of the thumbnail
			uint offset = tags[Tag.JPEGInterchangeFormat      ].AsUInt32();
			uint length = tags[Tag.JPEGInterchangeFormatLength].AsUInt32();
			if (length < 2) return;
			
			// Seek to the thumbnail data
			br.BaseStream.Seek(offset, SeekOrigin.Begin);
			
			// The thumbnail may be padded, so we scan forward until we reach the JPEG header (0xFFD8) or the end of the file
			byte b0 = br.ReadByte(), b1 = br.ReadByte();
			for (;!br.EndOfStream && !(b0 == JpgIdentifierTag_b0 && b1 == JpgIdentifierTag_b1); b0 = b1, b1 = br.ReadByte()) {}
			if (br.EndOfStream) return;
			
			// Validate that it is a valid jpg thumbnail
			br.BaseStream.Position -= 2; // step back to point at b0
			br.BaseStream.Position += length - 2; // jump to the end of the thumbnail minus 2 bytes
			if (br.ReadByte() != JpgIdentifierTag_e0 || // A valid JPEG stream ends with 0xFFD9
				br.ReadByte() != JpgIdentifierTag_e1)
				return;
			
			// Read the jpg thumbnail data
			br.BaseStream.Position -= length;
			exif_data.ReadThumbnail(br, (int)length);
		}

		/// <summary>Helper class for reading binary data from a stream allowing for endian-ness</summary>
		private class BinaryReaderEx :IDisposable
		{
			private readonly BinaryReader m_br;

			// ReSharper disable MemberCanBePrivate.Local
			/// <summary>Set to true if the provided stream contains little endian data</summary>
			public bool LittleEndian { get; set; }
			// ReSharper restore MemberCanBePrivate.Local

			// ReSharper disable UnusedMember.Local
			public BinaryReaderEx(Stream input) { m_br = new BinaryReader(input); }
			public BinaryReaderEx(Stream input, Encoding encoding) { m_br = new BinaryReader(input, encoding); }
			public void Dispose() { m_br.Dispose(); }
			// ReSharper restore UnusedMember.Local

			/// <summary>Access to the underlying stream</summary>
			public Stream BaseStream { get { return m_br.BaseStream; } }

			/// <summary>True when the end of stream is reached</summary>
			public bool EndOfStream { get { return m_br.BaseStream.Position == m_br.BaseStream.Length; } }

			/// <summary>Reads 'count' bytes from the stream, and reverses them if necessary for endian-ness</summary>
			public byte[] ReadBytes(int count)
			{
				var bytes = m_br.ReadBytes(count);
				if (LittleEndian != BitConverter.IsLittleEndian) Array.Reverse(bytes);
				return bytes;
			}
			
			/// <summary>Reads 'count' bytes from the stream at a given offset, restoring the stream position afterwards</summary>
			public byte[] ReadBytes(long offset, int count)
			{
				var pos = m_br.BaseStream.Position;
				try
				{
					m_br.BaseStream.Seek(offset, SeekOrigin.Begin);
					return ReadBytes(count);
				}
				finally { m_br.BaseStream.Position = pos; }
			}
			public byte   ReadByte()           { return m_br.ReadByte(); }
			public ushort ReadUShort()         { return BitConverter.ToUInt16(ReadBytes(2), 0); }
			public uint   ReadUInt()           { return BitConverter.ToUInt32(ReadBytes(4), 0); }
			public string ReadString(int len)  { var b = m_br.ReadBytes(len); return Encoding.UTF8.GetString(b,0,b.Length); }
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using gfx;
	
	[TestFixture] internal static partial class UnitTests
	{
		private const string ExifImage = @"Q:\sdk\pr\prcs\unittest_resources\exif_image.jpg";
		
		[Test] public static void TestExif_LoadExif()
		{
			{
				var exif = Exif.Load(ExifImage, true, true);
				Assert.AreEqual(1, exif[Exif.Tag.Orientation].AsInt32());
			}
			{
				using (var fs = new FileStream(ExifImage, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					var exif = Exif.Read(fs, true, true);
					Assert.AreEqual(1, exif[Exif.Tag.Orientation].AsUInt32());
				}
			}
		}
	}
}
#endif
