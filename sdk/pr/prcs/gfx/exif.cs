using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using pr.extn;
using pr.stream;

namespace pr.gfx
{
	/// <summary>Based on http://www.cipa.jp/english/hyoujunka/kikaku/pdf/DC-008-2010_E.pdf</summary>
	public static class Exif
	{
		// ReSharper disable UnusedMember.Local
		private const ushort TiffDataIdentifier           = 0x002A;
		private static readonly byte[] ExifIdentifier     = new[]{(byte)'E',(byte)'x',(byte)'i',(byte)'f',(byte)'\0',(byte)'\0'};
		private static readonly byte[] IntelIdentifier    = new[]{(byte)'I',(byte)'I'};
		private static readonly byte[] MotorolaIdentifier = new[]{(byte)'M',(byte)'M'};
		// ReSharper restore UnusedMember.Local

		/// <summary>Structure markers within a jpg file</summary>
		public static class JpgMarker
		{
			public const byte Start  = 0xFF;  // All markers are 2 bytes, beginning with 'Start' followed by one of these below
			public const byte SOI    = 0xD8;  // Start of Image
			public const byte APP1   = 0xE1;  // Application marker segment 1 (for Exif data)
			public const byte APP2   = 0xE2;  // Application marker segment 2 (for FlashPix Extension data)
			                                  // (Optional up to 0xEF)
			public const byte DQT    = 0xDB;  // Define Quantization Table
			public const byte DHT    = 0xC4;  // Define Huffman Table
			public const byte DRI    = 0xDD;  // Define Restart Interoperability
			public const byte SOF    = 0xC0;  // Start of Frame
			public const byte SOS    = 0xDA;  // Start of Scan
			                                  // Compressed data goes here
			public const byte EOI    = 0xD9;  // End of Image
		}

		// Exif table structure is:
		// "Exif" identifier    ___byte offsets are from here
		// Endian-ness (2bytes)
		// 0x002A (2bytes)
		// ByteOffsetToIFD0 (4bytes) (typically = 8)
		// IFD0: (main image exif data)
		//   Count (2bytes)
		//   Fields[Count] (N * 12bytes)
		//   ByteOffsetToNextIFD (IFD1)
		//   OversizedData (variable size)
		//   SubIFD: (sub exif data)
		//      Count
		//      Fields[Count]
		//      ByteOffsetToNextIFD (typically = 0 meaning no more)
		//      OversizedData
		// IFD1: (thumbnail exif data)
		//   Count
		//   Fields[Count]
		//   ByteOffsetToNextIFD (typically = 0 meaning no more)
		//   OversizedData
		
		/// <summary>Names for the various ifd tables within a jpg file</summary>
		public enum IFDTable
		{
			IFD0,
			IFD1,
			SubIFD,
			GpsIFD,
		}

		/// <summary>All Exif tags as per the Exif standard 2.2, JEITA CP-2451</summary>
		public enum Tag :ushort
		{
			Invalid = 0,
			
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
			SubIFDOffset                = 0x8769,
			GPSIFDOffset                = 0x8825,
			
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

		/// <summary>Data types used in the Exif tag fields</summary>
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
			case TiffDataType.Undefined:
				return 1;
			case TiffDataType.UShort:
			case TiffDataType.SShort:
				return 2;
			case TiffDataType.ULong:
			case TiffDataType.SLong:
			case TiffDataType.Single:
				return 4;
			
			case TiffDataType.URational:
			case TiffDataType.SRational:
			case TiffDataType.Double:
				return 8;
			}
		}

		/// <summary>Return the ifd table that 'tag' is typically found in</summary>
		public static IFDTable TableAffinity(Tag tag)
		{
			// IFD0 items
			if (tag >= Tag.ImageWidth && tag <= Tag.ReferenceBlackWhite) return IFDTable.IFD0;
			if (tag == Tag.Copyright || tag == Tag.SubIFDOffset || tag == Tag.GPSIFDOffset) return IFDTable.IFD0;
			
			// SubIFD items
			if (tag >= Tag.ExposureTime && tag <= Tag.ImageUniqueID)
				return IFDTable.SubIFD;
			
			// GPS subifd items
			if (tag >= Tag.GPSVersionID && tag <= Tag.GPSDifferential)
				return IFDTable.GpsIFD;
			
			// Dunno
			return IFDTable.IFD0;
		}

		/// <summary>Exceptions raised by this library</summary>
		public class Exception :System.Exception
		{
			public Exception() {}
			public Exception(string message) :base(message) {}
			public Exception(string message, Exception innerException) :base(message, innerException) {}
		}

		/// <summary>The value of an exif field</summary>
		public class Field
		{
			public const int FieldDataSizeInBytes = 4;
			public const int SizeInBytes = 12;
			public readonly static Field Null = new Field(Tag.Invalid, TiffDataType.Undefined, 0, new byte[FieldDataSizeInBytes]);

			public Tag          Tag;
			public TiffDataType DataType;
			public uint         Count;
			public byte[]       Data;

			public int AsInt
			{
				get
				{
					if (DataType == TiffDataType.SByte)  return Data[0];
					if (DataType == TiffDataType.UByte)  return Data[0];
					if (DataType == TiffDataType.SShort) return BitConverter.ToInt16(Data, 0);
					if (DataType == TiffDataType.UShort) return BitConverter.ToUInt16(Data, 0);
					if (DataType == TiffDataType.SLong)  return BitConverter.ToInt32(Data, 0);
					if (DataType == TiffDataType.ULong)  return (int)BitConverter.ToUInt32(Data, 0);
					throw new ArgumentException("Contained data type is not a signed integral type");
				}
			}
			public uint AsUInt
			{
				get
				{
					if (DataType == TiffDataType.UByte)  return Data[0];
					if (DataType == TiffDataType.SByte)  return Data[0];
					if (DataType == TiffDataType.UShort) return BitConverter.ToUInt16(Data, 0);
					if (DataType == TiffDataType.SShort) return (uint)BitConverter.ToInt16(Data, 0);
					if (DataType == TiffDataType.ULong)  return BitConverter.ToUInt32(Data, 0);
					if (DataType == TiffDataType.SLong)  return (uint)BitConverter.ToInt32(Data, 0);
					throw new ArgumentException("Contained data type is not an unsigned integral type");
				}
			}
			public double AsReal
			{
				get
				{
					if (DataType == TiffDataType.Single   ) return BitConverter.ToSingle(Data, 0);
					if (DataType == TiffDataType.Double   ) return BitConverter.ToDouble(Data, 0);
					if (DataType == TiffDataType.SRational) return (double)BitConverter.ToInt32 (Data, 0) / BitConverter.ToInt32 (Data, 4);
					if (DataType == TiffDataType.URational) return (double)BitConverter.ToUInt32(Data, 0) / BitConverter.ToUInt32(Data, 4);
					throw new ArgumentException("Contained data type is not floating point type");
				}
			}
			public Tuple<int,int> AsSRational
			{
				get
				{
					if (DataType == TiffDataType.SRational) return new Tuple<int, int>(BitConverter.ToInt32(Data, 0), BitConverter.ToInt32(Data, 4));
					throw new ArgumentException("Contained data type is not an signed rational type");
				}
			}
			public Tuple<uint,uint> AsURational
			{
				get
				{
					if (DataType == TiffDataType.URational) return new Tuple<uint, uint>(BitConverter.ToUInt32(Data, 0), BitConverter.ToUInt32(Data, 4));
					throw new ArgumentException("Contained data type is not an unsigned rational type");
				}
			}
			public string AsString
			{
				get { return Encoding.ASCII.GetString(Data); }
			}

			public Field() {}
			public Field(Tag tag, TiffDataType dt, uint count, byte[] data)
			{
				Tag      = tag;
				DataType = dt;
				Count    = count;
				Data     = data;
			}
		}

		/// <summary>
		/// Public interface for an Exif data accessor object.
		/// Presents all tags in the main ifd and sub ifd at the same level.</summary>
		public interface IExifData
		{
			/// <summary>Returns the number of contained exif fields</summary>
			int Count { get; }

			/// <summary>Enumerate the contained tags</summary>
			IEnumerable<Tag> Tags { get; }

			/// <summary>Returns true if 'tag' is in the contained data</summary>
			bool HasTag(Tag tag);

			/// <summary>Returns the Exif data associated with a tag, or 'byte[]{0,0,0,0}' if the tag is not in this data</summary>
			Field this[Tag tag] { get; }

			/// <summary>Jpg thumbnail data</summary>
			byte[] Thumbnail { get; }

			/// <summary>The Exif data associated with the thumbnail</summary>
			IExifData ThumbnailExif { get; }

			/// <summary>Add/Update an exif field</summary>
			void Set(Tag tag, Field field);
			void Set(Tag tag, byte[] b);
			void Set(Tag tag, byte b);
			void Set(Tag tag, sbyte b);
			void Set(Tag tag, ushort b);
			void Set(Tag tag, short b);
			void Set(Tag tag, uint b);
			void Set(Tag tag, int b);
			void Set(Tag tag, float b);
			void Set(Tag tag, double b);
			void Set(Tag tag, uint n, uint d);
			void Set(Tag tag, int n, int d);
			void Set(Tag tag, string s);

			/// <summary>Delete an exif field. Returns true if the field was deleted</summary>
			bool Delete(Tag tag);
		}

		/// <summary>Private interface for setting up an exif data accessor object</summary>
		private interface IExifDataInternal :IExifData
		{
			/// <summary>Returns access to the next IFD table</summary>
			IExifDataInternal NextIfd { get; }

			/// <summary>Returns access to the sub IFD table</summary>
			IExifDataInternal SubIfd { get; }

			/// <summary>Returns access to the gps IFD table</summary>
			IExifDataInternal GpsIfd { get; }

			/// <summary>Returns the number of fields contained in this ifd table. (Not recursive)</summary>
			int FieldCount { get; }

			/// <summary>Return the fields contained in this ifd table. (Not recursive)</summary>
			IEnumerable<Field> Fields { get; }

			/// <summary>Return the field associated with 'tag'</summary>
			Field Field(Tag tag, bool recursive);

			/// <summary>Read and store info about an exif field</summary>
			void ReadField(BinaryReaderEx br, long tiff_header_start);

			/// <summary>Read and store info about an image thumbnail</summary>
			void ReadThumbnail(BinaryReaderEx br, int length);

			/// <summary>Add/Update an exif field on this ifd table (not recursive)</summary>
			void AddOrUpdate(Tag tag, Field value);
		}

		/// <summary>Common implementation of Exif data access</summary>
		private abstract class ExifDataBase<Elem> :IExifData ,IExifDataInternal
		{
			protected readonly Dictionary<Tag, Elem> m_ifd = new Dictionary<Tag, Elem>();
			protected ExifDataBase<Elem> m_sub_ifd;
			protected ExifDataBase<Elem> m_gps_ifd;
			protected ExifDataBase<Elem> m_nxt_ifd;

			public override string ToString()
			{
				var sb = new StringBuilder();
				foreach (var f in Fields) sb.Append(f.Tag).Append(": ").Append(BitConverter.ToString(f.Data)).AppendLine(" ");
				return sb.ToString();
			}

			/// <summary>Returns the number of contained exif fields</summary>
			public int Count
			{
				get
				{
					return m_ifd.Count
						+ (m_sub_ifd != null ? m_sub_ifd.Count : 0)
						+ (m_gps_ifd != null ? m_gps_ifd.Count : 0)
						+ (m_nxt_ifd != null ? m_nxt_ifd.Count : 0);
				}
			}

			/// <summary>Enumerate the contained tags</summary>
			public IEnumerable<Tag> Tags
			{
				get
				{
					foreach (var k in m_ifd.Keys) yield return k;
					if (m_sub_ifd != null) foreach (var k in m_sub_ifd.Tags) yield return k;
					if (m_gps_ifd != null) foreach (var k in m_gps_ifd.Tags) yield return k;
					if (m_nxt_ifd != null) foreach (var k in m_nxt_ifd.Tags) yield return k;
				}
			}

			/// <summary>Returns true if 'tag' is in the exif data</summary>
			public bool HasTag(Tag tag)
			{
				if (m_ifd.ContainsKey(tag)) return true;
				if (m_sub_ifd != null && m_sub_ifd.HasTag(tag)) return true;
				if (m_gps_ifd != null && m_gps_ifd.HasTag(tag)) return true;
				if (m_nxt_ifd != null && m_nxt_ifd.HasTag(tag)) return true;
				return false;
			}

			/// <summary>Returns the raw exif data associated with a tag, or null if the tag is not available</summary>
			public Field this[Tag tag] { get { return Field(tag, true); } }

			/// <summary>Jpg thumbnail data</summary>
			public abstract byte[] Thumbnail { get; }

			/// <summary>The Exif data associated with the thumbnail</summary>
			public IExifData ThumbnailExif { get { return NextIfd; } }

			/// <summary>Add/Update an exif field</summary>
			public void Set(Tag tag, Field field)
			{
				switch (TableAffinity(tag))
				{
				default: throw new ArgumentOutOfRangeException();
				case IFDTable.IFD0:   AddOrUpdate(tag, field); break;
				case IFDTable.IFD1:   NextIfd.AddOrUpdate(tag, field); break;
				case IFDTable.SubIFD: SubIfd .AddOrUpdate(tag, field); break;
				case IFDTable.GpsIFD: GpsIfd .AddOrUpdate(tag, field); break;
				}
			}
			public void Set(Tag tag, byte[] b)       { Set(tag, new Field(tag, TiffDataType.Undefined, (uint)b.Length, b)); }
			public void Set(Tag tag, byte b)         { Set(tag, new Field(tag, TiffDataType.UByte, 1, new[]{b})); }
			public void Set(Tag tag, sbyte b)        { Set(tag, new Field(tag, TiffDataType.SByte, 1, new[]{(byte)b})); }
			public void Set(Tag tag, ushort b)       { Set(tag, new Field(tag, TiffDataType.UShort, 1, BitConverter.GetBytes(b))); }
			public void Set(Tag tag, short b)        { Set(tag, new Field(tag, TiffDataType.SShort, 1, BitConverter.GetBytes(b))); }
			public void Set(Tag tag, uint b)         { Set(tag, new Field(tag, TiffDataType.ULong, 1, BitConverter.GetBytes(b))); }
			public void Set(Tag tag, int b)          { Set(tag, new Field(tag, TiffDataType.SLong, 1, BitConverter.GetBytes(b))); }
			public void Set(Tag tag, float b)        { Set(tag, new Field(tag, TiffDataType.Single, 1, BitConverter.GetBytes(b))); }
			public void Set(Tag tag, double b)       { Set(tag, new Field(tag, TiffDataType.Double, 1, BitConverter.GetBytes(b))); }
			public void Set(Tag tag, uint n, uint d) { Set(tag, new Field(tag, TiffDataType.URational, 1, BitConverter.GetBytes(n).Concat(BitConverter.GetBytes(d)).ToArray())); }
			public void Set(Tag tag, int n, int d)   { Set(tag, new Field(tag, TiffDataType.SRational, 1, BitConverter.GetBytes(n).Concat(BitConverter.GetBytes(d)).ToArray())); }
			public void Set(Tag tag, string s)       { Set(tag, new Field(tag, TiffDataType.AsciiStrings, (uint)s.Length, Encoding.ASCII.GetBytes(s))); }

			/// <summary>Delete an exif field</summary>
			public bool Delete(Tag tag)
			{
				if (m_ifd.Remove(tag)) return true;
				if (m_sub_ifd != null && m_sub_ifd.Delete(tag)) return true;
				if (m_gps_ifd != null && m_gps_ifd.Delete(tag)) return true;
				if (m_nxt_ifd != null && m_nxt_ifd.Delete(tag)) return true;
				return false;
			}

			/// <summary>Add/Update an exif field on this ifd table (not recursive)</summary>
			public abstract void AddOrUpdate(Tag tag, Field field);

			/// <summary>Returns access to the next IFD table</summary>
			public abstract IExifDataInternal NextIfd { get; }

			/// <summary>Returns access to the sub IFD table</summary>
			public abstract IExifDataInternal SubIfd { get; }

			/// <summary>Returns access to the gps IFD table</summary>
			public abstract IExifDataInternal GpsIfd { get; }

			/// <summary>Returns the number of fields contained in this ifd table. (Not recursive)</summary>
			public int FieldCount { get { return m_ifd.Count; } }

			/// <summary>Return the fields contained in this ifd table. (Not recursive)</summary>
			public IEnumerable<Field> Fields { get { foreach (var k  in m_ifd.Keys) yield return Field(k,false); } }
 
			/// <summary>Return the field associated with 'tag'</summary>
			public abstract Field Field(Tag tag, bool recursive);

			/// <summary>Read and store info about an exif field</summary>
			public abstract void ReadField(BinaryReaderEx br, long tiff_header_start);

			/// <summary>Read and store info about an image thumbnail</summary>
			public abstract void ReadThumbnail(BinaryReaderEx br, int length);
		}

		/// <summary>The Exif data read into memory from a jpg image</summary>
		private class Data :ExifDataBase<Field>, IExifData ,IExifDataInternal
		{
			/// <summary>Jpg thumbnail data</summary>
			public override byte[] Thumbnail { get { return m_Thumbnail; } }
			private byte[] m_Thumbnail;

			/// <summary>Returns access to the sub IFD table</summary>
			public override IExifDataInternal SubIfd { get { return m_sub_ifd ?? (m_sub_ifd = new Data()); } }

			/// <summary>Returns access to the gps IFD table</summary>
			public override IExifDataInternal GpsIfd { get { return m_gps_ifd ?? (m_gps_ifd = new Data()); } }

			/// <summary>Returns access to the next IFD table</summary>
			public override IExifDataInternal NextIfd { get { return m_nxt_ifd ?? (m_nxt_ifd = new Data()); } }

			/// <summary>Return the field associated with 'tag'</summary>
			public override Field Field(Tag tag, bool recursive)
			{
				Field r;
				if (m_ifd.TryGetValue(tag, out r)) return r;
				if (!recursive) return Exif.Field.Null;
				if (m_sub_ifd != null && !ReferenceEquals(Exif.Field.Null, (r = m_sub_ifd.Field(tag, true)))) return r;
				if (m_gps_ifd != null && !ReferenceEquals(Exif.Field.Null, (r = m_gps_ifd.Field(tag, true)))) return r;
				if (m_nxt_ifd != null && !ReferenceEquals(Exif.Field.Null, (r = m_nxt_ifd.Field(tag, true)))) return r;
				return Exif.Field.Null;
			}

			/// <summary>Read and store info about an exif field</summary>
			public override void ReadField(BinaryReaderEx br, long tiff_header_start)
			{
				var field = ParseExifField(br, tiff_header_start);
				m_ifd[field.Tag] = field;
			}

			/// <summary>Read and store info about an image thumbnail</summary>
			public override void ReadThumbnail(BinaryReaderEx br, int length)
			{
				// Read the jpg thumbnail data
				byte[] thumb = new byte[length];
				br.BaseStream.Read(thumb, 0, thumb.Length);
				m_Thumbnail = thumb;
			}

			/// <summary>Add/Update an exif field</summary>
			public override void AddOrUpdate(Tag tag, Field field)
			{
				m_ifd[tag] = field;
			}
		}

		/// <summary>An index for the exif data in a jpg image</summary>
		private class Index :ExifDataBase<long>, IExifData ,IExifDataInternal ,IDisposable
		{
			private readonly BinaryReaderEx m_br;
			private long m_tiff_header_start;
			private long m_thumbnail_offset;
			private int m_thumbnail_length;
			
			public Index(Stream stream) { m_br = new BinaryReaderEx(stream); }
			public void Dispose() { m_br.Dispose(); }

			/// <summary>Jpg thumbnail data</summary>
			public override byte[] Thumbnail { get { return m_thumbnail_length != 0 ? m_br.ReadBytes(m_thumbnail_offset, m_thumbnail_length) : null; } }

			/// <summary>Returns access to the sub IFD table</summary>
			public override IExifDataInternal SubIfd { get { return m_sub_ifd ?? (m_sub_ifd = new Index(m_br.BaseStream)); } }

			/// <summary>Returns access to the gps IFD table</summary>
			public override IExifDataInternal GpsIfd { get { return m_gps_ifd ?? (m_gps_ifd = new Index(m_br.BaseStream)); } }

			/// <summary>Returns access to the next IFD table</summary>
			public override IExifDataInternal NextIfd { get { return m_nxt_ifd ?? (m_nxt_ifd = new Index(m_br.BaseStream)); } }

			/// <summary>Return the field associated with 'tag'</summary>
			public override Field Field(Tag tag, bool recursive)
			{
				Field r;
				for (long ofs; m_ifd.TryGetValue(tag, out ofs);)
				{
					long pos = m_br.BaseStream.Position;
					try
					{
						m_br.BaseStream.Position = ofs;
						r = ParseExifField(m_br, m_tiff_header_start);
						return r;
					}
					finally { m_br.BaseStream.Position = pos; }
				}
				if (!recursive) return Exif.Field.Null;
				if (m_sub_ifd != null && !ReferenceEquals(Exif.Field.Null, (r = m_sub_ifd.Field(tag, true)))) return r;
				if (m_gps_ifd != null && !ReferenceEquals(Exif.Field.Null, (r = m_gps_ifd.Field(tag, true)))) return r;
				if (m_nxt_ifd != null && !ReferenceEquals(Exif.Field.Null, (r = m_nxt_ifd.Field(tag, true)))) return r;
				return Exif.Field.Null;
			}

			/// <summary>Read and store info about an exif field</summary>
			public override void ReadField(BinaryReaderEx br, long tiff_header_start)
			{
				m_br.LittleEndian = br.LittleEndian;
				m_tiff_header_start = tiff_header_start;
				var tag = (Tag)br.ReadUShort();
				m_ifd[tag] = br.BaseStream.Position - 2;
				br.BaseStream.Position += Exif.Field.SizeInBytes - 2;
			}

			/// <summary>Read and store info about an image thumbnail</summary>
			public override void ReadThumbnail(BinaryReaderEx br, int length)
			{
				m_thumbnail_offset = br.BaseStream.Position;
				m_thumbnail_length = length;
				br.BaseStream.Position += length;
			}

			/// <summary>Add/Update an exif field</summary>
			public override void AddOrUpdate(Tag tag, Field field)
			{
				throw new NotSupportedException("Modifying IFD tables is not supported when exif is being read directly from a stream");
			}
		}

		/// <summary>Load the Exif data from a Jpg image into memory</summary>
		public static IExifData Load(string filepath, bool load_thumbnail = true)
		{
			using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
				return Load(fs, load_thumbnail);
		}

		/// <summary>Load the Exif data from a Jpg image into memory. Note this method does not close the stream</summary>
		public static IExifData Load(Stream src, bool load_thumbnail = true)
		{
			return Parse(src, new Data(), load_thumbnail);
		}

		/// <summary>Reads the exif data in a jpg creating an index of the tags and their byte offsets into the file</summary>
		public static IExifData Read(Stream src, bool load_thumbnail = true)
		{
			return Parse(src, new Index(src), load_thumbnail);
		}

		/// <summary>Saves the exif data in 'exif_data' into 'dst'. Any existing exif data in source is overwritten</summary>
		public static void Save(string src_file, IExifData exif_data, string dst_file)
		{
			using (var src = new FileStream(src_file, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (var dst = new FileStream(dst_file, FileMode.Create, FileAccess.Write, FileShare.Read))
				Write(src, exif_data as IExifDataInternal, dst);
		}

		/// <summary>Saves the exif data in 'exif_data' into 'dst'. Any existing exif data in source is overwritten</summary>
		public static void Save(Stream src, IExifData exif_data, Stream dst)
		{
			Write(src, exif_data as IExifDataInternal, dst);
		}

		/// <summary>Parse a jpg image for its exif data</summary>
		private static IExifDataInternal Parse(Stream src, IExifDataInternal data, bool load_thumbnail)
		{
			if (src == null) throw new ArgumentNullException("src", "Null Jpg stream");
			if (!src.CanSeek) throw new Exception("Loading Exif data requires a seek-able stream");
			
			using (var br = new BinaryReaderEx(new UncloseableStream(src)))
			{
				// JPEG encoding uses big endian (i.e. Motorola) byte aligns.
				// The TIFF encoding found later in the document will specify
				// the byte aligns used for the rest of the document.
				br.LittleEndian = false;
				
				// Check that this is a Jpg file
				if (br.ReadByte() != JpgMarker.Start || br.ReadByte() != JpgMarker.SOI)
					throw new Exception("Source is not a Jpg data");
				
				// The file has a number of blocks (Exif/JFIF), each of which has a tag number
				// followed by a length. We scan the document until the required tag (0xFFE1)
				// is found. All tags start with FF, so a non FF tag indicates an error.
				
				// Scan to the start of the Exif data
				for (;!br.EndOfStream && br.ReadByte() == JpgMarker.Start && br.ReadByte() != JpgMarker.APP1;)
					br.BaseStream.Seek(br.ReadUShort() - 2, SeekOrigin.Current);
				if (br.EndOfStream) return data; // No Exif data found, return an empty data object
				
				// Populate the Exif.Data
				
				// The next 2 bytes are the size of the Exif data.
				var exif_data_size = br.ReadUShort();
				if (exif_data_size == 0) return data;
				
				// Next is the Exif data itself. It starts with the ASCII string "Exif\0\0".
				if (!ExifIdentifier.SequenceEqual(br.ReadBytes(6)))
					throw new Exception("Malformed Exif data");
				
				// We're now into the TIFF format
				// Here the byte alignment may be different. II for Intel, MM for Motorola
				var tiff_hdr_start = br.BaseStream.Position;
				br.LittleEndian = IntelIdentifier.SequenceEqual(br.ReadBytes(2));
				
				// Next 2 bytes are always the same.
				if (br.ReadUShort() != TiffDataIdentifier)
					throw new Exception("Error in TIFF data");
				
				// Jump to the main IFD (image file directory)
				br.BaseStream.Position = tiff_hdr_start + br.ReadUInt(); // Note that this offset is from the first byte of the TIFF header.
				
				// Parse the IFD tables
				ParseExifTable(br, data, tiff_hdr_start);
				
				if (load_thumbnail)
					ParseJpgThumbnail(br, data, data.NextIfd);
			}
			
			return data;
		}

		/// <summary>Saves the exif data in 'exif_data' into 'dst'. Any existing exif data in source is overwritten</summary>
		private static void Write(Stream src, IExifDataInternal exif_data, Stream dst)
		{
			if (src == null) throw new Exception("Exif data must be an instance returned from this library");
			if (!src.CanSeek) throw new Exception("Loading Exif data requires a seek-able stream");
			if (ReferenceEquals(src,dst)) throw new Exception("Cannot read from and write to the same stream");
			
			using (var br = new BinaryReaderEx(new UncloseableStream(src)))
			using (var bw = new BinaryWriterEx(new UncloseableStream(dst)))
			{
				long section_start;
				
				// JPEG encoding uses big endian (i.e. Motorola) byte aligns.
				// The TIFF encoding found later in the document will specify
				// the byte aligns used for the rest of the document.
				bw.LittleEndian = false;
				
				// Copy the Jpg start of image marker
				bw.Write(br.ReadBytes(2));
				
				// Write the exif data section
				{
					section_start = br.BaseStream.Position;
					
					// Write the exif data marker
					bw.Write(JpgMarker.Start);
					bw.Write(JpgMarker.APP1);
						
					// Write the section size
					var section_size_offset = bw.BaseStream.Position;
					bw.Write((ushort)0); // Will update this at the end
						
					// Write the Exif identifier tag
					bw.Write(ExifIdentifier);
					var tiff_header_start = bw.BaseStream.Position;
					bw.LittleEndian = true; // Intel is little endian
						
					// Use Intel endian-ness
					bw.Write(IntelIdentifier);
						
					// Write the TIFF data id
					bw.Write(TiffDataIdentifier);
						
					// Write the offset to the main IFD table
					bw.Write((uint)(bw.BaseStream.Position + sizeof(uint) - tiff_header_start));
						
					// Write the ifd tables
					WriteExifTable(bw, exif_data, tiff_header_start);
					bw.LittleEndian = false; // Back to Motorola endian
						
					// Write the thumbnail image
					var thumb = exif_data.Thumbnail;
					if (thumb != null)
						bw.Write(thumb);
						
					// Update the section size
					bw.Write(section_size_offset, (ushort)(bw.BaseStream.Position - section_start - 2)); // section size doesn't include the marker
				}
				
				// Copy the sections of the source jpg, excluding the exif data section.
				// When we encounter an unmarked section, that should be the scan data.
				for (;!br.EndOfStream;)
				{
					section_start = br.BaseStream.Position;
					var b0 = br.ReadByte();
					var b1 = br.ReadByte();
					var size = br.ReadUShort() + 2; // +2 to include the section marker
					
					// Not a section
					if (b0 != JpgMarker.Start)
						break;
					
					// If this is the Exif data section, skip it
					if (b1 == JpgMarker.APP1)
					{
						br.BaseStream.Position = section_start + size;
					}
					// Otherwise, copy the section to the dst stream
					else
					{
						br.BaseStream.Position = section_start;
						var copied = br.BaseStream.CopyTo(size, bw.BaseStream);
						if (copied != size) throw new IOException("Error while writing to output stream");
					}
				}
				
				// Copy the scan data
				br.BaseStream.Position = section_start;
				var scan_data_size = br.BaseStream.Length - br.BaseStream.Position - 2;
				if (scan_data_size > 0)
					br.BaseStream.CopyTo(scan_data_size, bw.BaseStream);
				
				// Copy the Jpg end of image marker
				bw.Write(br.ReadBytes(2));
			}
		}

		/// <summary>Parses IFD tables from 'br' into 'exif_data'. Assumes 'br' is positioned at the start of the table</summary>
		private static void ParseExifTable(BinaryReaderEx br, IExifDataInternal exif_data, long tiff_header_start)
		{
			// First 2 bytes is the number of entries in this IFD
			for (ushort i = 0, iend = br.ReadUShort(); i != iend; i++)
				exif_data.ReadField(br, tiff_header_start);
			
			// Save the offset to the next ifd table in the linked list
			var next_idf_offset = br.ReadUInt();
			
			// Check for a sub IFD table
			var sub = exif_data.Field(Tag.SubIFDOffset, false);
			if (sub != Field.Null)
			{
				br.BaseStream.Position = tiff_header_start + sub.AsUInt;
				ParseExifTable(br, exif_data.SubIfd, tiff_header_start);
			}
			
			// Check for an optional GPS ifd table
			var gps = exif_data.Field(Tag.GPSIFDOffset, false);
			if (gps != Field.Null)
			{
				br.BaseStream.Position = tiff_header_start + gps.AsUInt;
				ParseExifTable(br, exif_data.GpsIfd, tiff_header_start);
			}
			
			// Move to the next table
			if (next_idf_offset != 0)
			{
				br.BaseStream.Position = tiff_header_start + next_idf_offset;
				ParseExifTable(br, exif_data.NextIfd, tiff_header_start);
			}
		}

		/// <summary>Writes IFD tables from 'exif_data' into 'bw'. Assumes 'bw' is positioned at the location to start writing</summary>
		private static void WriteExifTable(BinaryWriterEx bw, IExifDataInternal exif_data, long tiff_header_start)
		{
			// Write the number of fields in the table
			var count_offset = bw.BaseStream.Position; // Save the position to write the table field count
			bw.Write((ushort)0); // Will update this after writing the table data
			
			// The offset to the start of the oversized data for the table
			var oversized_data_offset = bw.BaseStream.Position + exif_data.FieldCount * Field.SizeInBytes + sizeof(uint);
			
			long subifd_offset = 0; // Saves the location of where to write the offset to the subifd table
			long gpsifd_offset = 0; // Saves the location of where to write the offset to the gpsifd table
			
			// Write each field
			int count = 0;
			foreach (var f in exif_data.Fields)
			{
				bw.Write((ushort)f.Tag);
				bw.Write((ushort)f.DataType);
				bw.Write(f.Count);
				if (f.Tag == Tag.SubIFDOffset)
				{
					subifd_offset = bw.BaseStream.Position;
					bw.Write((uint)0); // Will update this when we write the sub ifd table
				}
				else if (f.Tag == Tag.GPSIFDOffset)
				{
					gpsifd_offset = bw.BaseStream.Position;
					bw.Write((uint)0); // Will update this when we write the gps ifd table
				}
				else if (f.Data.Length <= Field.FieldDataSizeInBytes)
				{
					bw.Write(f.Data);
				}
				else
				{
					bw.Write(oversized_data_offset, f.Data);                     // write the data at the oversized data position
					bw.Write((uint)(oversized_data_offset - tiff_header_start)); // write the offset to it
					oversized_data_offset += f.Data.Length;
				}
				++count;
			}
			
			// Write the field count
			bw.Write(count_offset, (ushort)count);
			
			// Write the offset to the next ifd table
			var nextifd_offset = bw.BaseStream.Position;
			bw.Write((uint)0); // Will update this if we write a next ifd table
			
			// Move the stream position past the oversized data
			bw.BaseStream.Position = oversized_data_offset;
			
			// Write the sub ifd (if there is one)
			if (subifd_offset != 0)
			{
				bw.Write(subifd_offset, (ushort)(bw.BaseStream.Position - tiff_header_start));
				WriteExifTable(bw, exif_data.SubIfd, tiff_header_start);
			}
			
			// Write the gps ifd (if there is one)
			if (gpsifd_offset != 0)
			{
				bw.Write(gpsifd_offset, (ushort)(bw.BaseStream.Position - tiff_header_start));
				WriteExifTable(bw, exif_data.GpsIfd, tiff_header_start);
			}
			
			// Write the next next ifd (if there is one)
			if (exif_data.NextIfd.Count != 0)
			{
				bw.Write(nextifd_offset, (ushort)(bw.BaseStream.Position - tiff_header_start));
				WriteExifTable(bw, exif_data.NextIfd, tiff_header_start);
			}
		}

		/// <summary>
		/// Parse a single exif entry.
		/// Assumes 'br' is positioned at the start of the field.
		/// 'br' is moved to the start of the next field on return</summary>
		private static Field ParseExifField(BinaryReaderEx br, long tiff_header_start)
		{
			// Read the exif field data. Each entry is 12 bytes
			var field      = new Field();
			field.Tag      = (Tag)br.ReadUShort();
			field.DataType = (TiffDataType)br.ReadUShort();
			field.Count    = br.ReadUInt();
			
			// If the total space taken up by the field is longer than the
			// FieldSizeInBytes bytes afforded by the tag data, the tag data
			// will contain an offset to the actual data.
			var elem_size = SizeInBytes(field.DataType);
			var data_size = (int)field.Count * elem_size;
			if (data_size > Field.FieldDataSizeInBytes)
			{
				var ofs = br.ReadUInt();
				field.Data = br.ReadBytes(tiff_header_start + ofs, data_size);
			}
			else
			{
				field.Data  = br.ReadBytes(Field.FieldDataSizeInBytes);
			}
			
			// If the contained data is not bytes, endian swap each element
			if (br.LittleEndian != BitConverter.IsLittleEndian)
			{
				switch (field.DataType)
				{
				// For these types 'elem_size' is correct
				case TiffDataType.SShort:
				case TiffDataType.UShort:
				case TiffDataType.SLong:
				case TiffDataType.ULong:
				case TiffDataType.Single:
				case TiffDataType.Double:
					for (uint i = 0, iend = field.Count; i != iend; ++i)
						Array.Reverse(field.Data, (int)i * elem_size, elem_size);
					break;
				
				case TiffDataType.SRational:
				case TiffDataType.URational:
					int sz = elem_size / 2;
					for (uint i = 0, iend = 2*field.Count; i != iend; ++i)
						Array.Reverse(field.Data, (int)i*sz, sz);
					break;
				}
			}
			return field;
		}

		/// <summary>
		/// Retrieves a JPEG thumbnail from the image if one is present. Note that this method cannot retrieve thumbnails encoded in other formats,
		/// but since the DCF specification specifies that thumbnails must be JPEG, this method will be sufficient for most purposes
		/// See http://gvsoft.homedns.org/exif/exif-explanation.html#TIFFThumbs or http://partners.adobe.com/public/developer/en/tiff/TIFF6.pdf for 
		/// details on the encoding of TIFF thumbnails</summary>
		private static void ParseJpgThumbnail(BinaryReaderEx br, IExifDataInternal exif_data, IExifDataInternal tags)
		{
			if (!tags.HasTag(Tag.Compression) ||
				!tags.HasTag(Tag.JPEGInterchangeFormat) ||
				!tags.HasTag(Tag.JPEGInterchangeFormatLength))
				return;
			
			// Get the thumbnail encoding. This method only handles JPEG thumbnails (compression type 6)
			var compression = (CompressiongType)tags[Tag.Compression].AsInt;
			if (compression != CompressiongType.JPEG_Old)
				return;
			
			// Get the location of the thumbnail
			var offset = tags[Tag.JPEGInterchangeFormat      ].AsInt;
			var length = tags[Tag.JPEGInterchangeFormatLength].AsInt;
			if (length < 2) return;
			
			// Seek to the thumbnail data
			br.BaseStream.Seek(offset, SeekOrigin.Begin);
			
			// The thumbnail may be padded, so we scan forward until we reach the JPEG header (0xFFD8) or the end of the file
			byte b0 = br.ReadByte(), b1 = br.ReadByte();
			for (;!br.EndOfStream && !(b0 == JpgMarker.Start && b1 == JpgMarker.SOI); b0 = b1, b1 = br.ReadByte()) {}
			if (br.EndOfStream) return;
			
			// Validate that it is a valid jpg thumbnail
			br.BaseStream.Position -= 2; // step back to point at b0
			br.BaseStream.Position += length - 2; // jump to the end of the thumbnail minus 2 bytes
			if (br.ReadByte() != JpgMarker.Start || // A valid JPEG stream ends with 0xFFD9
				br.ReadByte() != JpgMarker.EOI)
				return;
			
			// Read the jpg thumbnail data
			br.BaseStream.Position -= length;
			exif_data.ReadThumbnail(br, length);
		}

		/// <summary>
		/// Helper class for reading binary data from a stream allowing for endian-ness.
		/// Does not close the stream.</summary>
		private class BinaryReaderEx :IDisposable
		{
			private readonly BinaryReader m_br;

			/// <summary>Set to true if the provided stream contains little endian data</summary>
			public bool LittleEndian { get; set; }

			// ReSharper disable UnusedMember.Local, MemberCanBePrivate.Local
			public BinaryReaderEx(Stream input) :this(input, Encoding.UTF8) {}
			public BinaryReaderEx(Stream input, Encoding encoding) { m_br = new BinaryReader(new UncloseableStream(input), encoding); }
			public void Dispose() { m_br.Dispose(); }
			// ReSharper restore UnusedMember.Local, MemberCanBePrivate.Local

			/// <summary>Access to the underlying stream</summary>
			public Stream BaseStream { get { return m_br.BaseStream; } }

			/// <summary>Returns true when the end of the stream is reached</summary>
			public bool EndOfStream { get { return m_br.BaseStream.Position == m_br.BaseStream.Length; } }

			/// <summary>Read a byte from the stream</summary>
			public byte ReadByte() { return m_br.ReadByte(); }

			/// <summary>Reads 'count' bytes from the stream</summary>
			public byte[] ReadBytes(int count) { return m_br.ReadBytes(count); }

			/// <summary>Reads 'count' bytes from the stream at 'offset' preserving the stream position</summary>
			public byte[] ReadBytes(long offset, int count)
			{
				var pos = m_br.BaseStream.Position;
				try
				{
					m_br.BaseStream.Seek(offset, SeekOrigin.Begin);
					return m_br.ReadBytes(count);
				}
				finally { m_br.BaseStream.Position = pos; }
			}

			/// <summary>Read a ushort from the stream</summary>
			public ushort ReadUShort()
			{
				var bytes = ReadBytes(2);
				if (LittleEndian != BitConverter.IsLittleEndian) Array.Reverse(bytes);
				return BitConverter.ToUInt16(bytes, 0);
			}

			/// <summary>Read a uint from the stream</summary>
			public uint ReadUInt()
			{
				var bytes = ReadBytes(4);
				if (LittleEndian != BitConverter.IsLittleEndian) Array.Reverse(bytes);
				return BitConverter.ToUInt32(bytes, 0);
			}
		}

		/// <summary>
		/// Helper class for writing binary data to a stream allowing for endian-ness.
		/// Does not close the stream</summary>
		private class BinaryWriterEx :IDisposable
		{
			private readonly BinaryWriter m_bw;

			/// <summary>Set to true if the provided stream contains little endian data</summary>
			public bool LittleEndian { private get; set; }

			// ReSharper disable UnusedMember.Local, MemberCanBePrivate.Local
			public BinaryWriterEx(Stream input) :this(input, Encoding.UTF8) {}
			public BinaryWriterEx(Stream input, Encoding encoding) { m_bw = new BinaryWriter(new UncloseableStream(input), encoding); }
			public void Dispose() { m_bw.Dispose(); }
			// ReSharper restore UnusedMember.Local, MemberCanBePrivate.Local

			/// <summary>Access to the underlying stream</summary>
			public Stream BaseStream { get { return m_bw.BaseStream; } }

			/// <summary>Write a byte to the stream</summary>
			public void Write(byte b) { m_bw.Write(b); m_bw.Flush(); }

			/// <summary>Write a byte array to the stream</summary>
			public void Write(byte[] bytes) { m_bw.Write(bytes); m_bw.Flush(); }

			/// <summary>Writes a byte[] starting at 'offset' preserving the stream position</summary>
			public void Write(long offset, byte[] bytes)
			{
				var pos = m_bw.BaseStream.Position;
				try
				{
					m_bw.BaseStream.Seek(offset, SeekOrigin.Begin);
					Write(bytes);
				}
				finally { m_bw.BaseStream.Position = pos; }
			}

			/// <summary>Write a uint to the stream. (endian swapping if needed)</summary>
			public void Write(uint ui)
			{
				var bytes = BitConverter.GetBytes(ui);
				if (LittleEndian != BitConverter.IsLittleEndian) Array.Reverse(bytes);
				Write(bytes);
			}

			/// <summary>Write a ushort to the stream. (endian swapping if needed)</summary>
			public void Write(ushort us)
			{
				var bytes = BitConverter.GetBytes(us);
				if (LittleEndian != BitConverter.IsLittleEndian) Array.Reverse(bytes);
				Write(bytes);
			}

			/// <summary>Write a ushort to the stream at 'offset' preserving the stream position. (endian swapping if needed)</summary>
			public void Write(long offset, ushort us)
			{
				var pos = m_bw.BaseStream.Position;
				try
				{
					var bytes = BitConverter.GetBytes(us);
					if (LittleEndian != BitConverter.IsLittleEndian) Array.Reverse(bytes);
					m_bw.BaseStream.Seek(offset, SeekOrigin.Begin);
					Write(bytes);
				}
				finally { m_bw.BaseStream.Position = pos; }
			}
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using System.Drawing;
	using gfx;
	
	[TestFixture] internal static partial class UnitTests
	{
		private const string SrcImage = @"Q:\sdk\pr\prcs\unittest_resources\exif_image.jpg";
		private const string DstImage = @"Q:\sdk\pr\prcs\unittest_resources\exif_image_out.jpg";
		
		[Test] public static void TestExif_LoadExifToMemory()
		{
			var exif = Exif.Load(SrcImage);
			Assert.AreEqual(1.0/50.0, exif[Exif.Tag.ExposureTime].AsReal, float.Epsilon);
		}
		[Test] public static void TestExif_ReadExifFromFile()
		{
			using (var fs = new FileStream(SrcImage, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				var exif = Exif.Read(fs);
				Assert.AreEqual(1.0/50.0, exif[Exif.Tag.ExposureTime].AsReal);
			}
		}
		[Test] public static void TestExif_WriteExifToFile()
		{
			var exif_in = Exif.Load(SrcImage);
			Exif.Save(SrcImage, exif_in, DstImage);
			var exif_out = Exif.Load(DstImage);
			
			Assert.DoesNotThrow(() => new Bitmap(DstImage), "Output jpg image is invalid");
			Assert.AreEqual(exif_in.Count, exif_out.Count);
			Assert.AreEqual(exif_in[Exif.Tag.FocalLength ].AsReal, exif_out[Exif.Tag.FocalLength ].AsReal);
			Assert.AreEqual(exif_in[Exif.Tag.ExposureTime].AsReal, exif_out[Exif.Tag.ExposureTime].AsReal);
		}
		[Test] public static void TestExif_AddRemoveFields()
		{
			var exif = Exif.Load(SrcImage);
			
			// Update the field
			exif.Set(Exif.Tag.Orientation, (ushort)3);
			Assert.AreEqual(3, exif[Exif.Tag.Orientation].AsInt);
			
			// Save to the file
			Exif.Save(SrcImage, exif, DstImage);
			exif = Exif.Load(DstImage);
			Assert.AreEqual(3, exif[Exif.Tag.Orientation].AsInt);
			
			// Remove the field
			exif.Delete(Exif.Tag.Orientation);
			Assert.IsFalse(exif.HasTag(Exif.Tag.Orientation));
			
			// Save to file
			Exif.Save(SrcImage, exif, DstImage);
			exif = Exif.Load(DstImage);
			Assert.IsFalse(exif.HasTag(Exif.Tag.Orientation));
			
			// Restore the field
			exif.Set(Exif.Tag.Orientation, (ushort)1);
			Assert.AreEqual(1, exif[Exif.Tag.Orientation].AsInt);
			
			// Save to file
			Exif.Save(SrcImage, exif, DstImage);
			exif = Exif.Load(DstImage);
			Assert.AreEqual(1, exif[Exif.Tag.Orientation].AsInt);
		}
	}
}
#endif
