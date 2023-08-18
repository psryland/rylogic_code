//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Rylogic.Utility;

namespace Rylogic.Extn
{
	/// <summary>Extensions for strings</summary>
	public static class Stream_
	{
		/// <summary>Write to the stream</summary>
		public static TStream Write2<TStream>(this TStream s, byte[] buffer, int offset, int count) where TStream : Stream
		{
			s.Write(buffer, offset, count);
			return s;
		}

		/// <summary>Copies a maximum of 'count' bytes from this stream to 'dst' using the given buffer. Returns the number of bytes copied</summary>
		public static long CopyTo(this Stream src, long count, Stream dst, byte[] buffer)
		{
			long copied = 0;
			for (int n; (n = src.Read(buffer, 0, (int)Math.Min(count - copied, buffer.Length))) != 0; copied += n)
				dst.Write(buffer, 0, n);
			return copied;
		}

		/// <summary>Copies a maximum of 'count' bytes from this stream to 'dst'. Returns the number of bytes copied</summary>
		public static long CopyTo(this Stream src, long count, Stream dst)
		{
			return src.CopyTo(count, dst, new byte[4096]);
		}

		/// <summary>Return an array of bytes read from the stream</summary>
		public static byte[] ToBytes(this Stream src)
		{
			using var ms = new MemoryStream();
			src.CopyTo(ms);
			return ms.ToArray();
		}
		public async static Task<byte[]> ToBytesAsync(this Stream src)
		{
			using var ms = new MemoryStream();
			await src.CopyToAsync(ms);
			return ms.ToArray();
		}

		/// <summary>Fill 'buffer' from the stream, throwing if not enough data is available</summary>
		public static byte[] Read(this Stream src, byte[] buffer)
		{
			var read = src.Read(buffer, 0, buffer.Length);
			if (read == buffer.Length) return buffer;
			var msg = src.CanSeek
				? $"Incomplete read. Expected {buffer.Length} bytes at stream position {src.Position - read} (of {src.Length})"
				: $"Incomplete read. Expected {buffer.Length} bytes from stream";
			throw new Exception(msg);
		}

		/// <summary>Read a structure from the stream</summary>
		public static T Read<T>(this Stream src) where T:struct
		{
			try
			{
				var buffer = Util.CreateByteArrayOfSize<T>();
				src.Read(buffer);
				return Util.FromBytes<T>(buffer);
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to read type {typeof(T).Name}.\r\n{ex.Message}");
			}
		}

		/// <summary>Return an RAII object to preserve the current stream position</summary>
		public static Scope PreservePosition(this Stream src)
		{
			return Scope.Create(
				() => src.Position,
				sp => src.Position = sp);
		}

		/// <summary>Get/Set the stream position for a stream reader</summary>
		public static long Position(this StreamReader reader)
		{
			// Shift 'Position' back by the number of bytes read into StreamReader's internal buffer.
			var byte_len = (int)m_sr_byte_length_field.GetValue(reader)!;
			var position = reader.BaseStream.Position - byte_len;

			// If we have consumed chars from the buffer we need to calculate how many
			// bytes they represent in the current encoding and add that to the position.
			var char_pos = (int)m_sr_char_pos_field.GetValue(reader)!;
			if (char_pos > 0)
			{
				var char_buffer = (char[])m_sr_char_buffer_field.GetValue(reader)!;
				var encoding = reader.CurrentEncoding;
				var bytes_consumed = encoding.GetBytes(char_buffer, 0, char_pos).Length;
				position += bytes_consumed;
			}

			Debug.Assert(position >= 0);
			return position;
		}
		public static void Position(this StreamReader reader, long position)
		{
			reader.DiscardBufferedData();
			reader.BaseStream.Seek(position, SeekOrigin.Begin);
		}
		private static readonly FieldInfo m_sr_char_pos_field = typeof(StreamReader).GetField("charPos", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) ?? throw new MissingFieldException();
		private static readonly FieldInfo m_sr_byte_length_field = typeof(StreamReader).GetField("byteLen", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) ?? throw new MissingFieldException();
		private static readonly FieldInfo m_sr_char_buffer_field = typeof(StreamReader).GetField("charBuffer", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) ?? throw new MissingFieldException();
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;

	[TestFixture] public class TestStreamExtns
	{
		[Test] public void CopyNTo()
		{
			using (var ms0 = new MemoryStream(new byte[]{1,2,3,4,5,6,7,8}, false))
			using (var ms1 = new MemoryStream())
			{
				ms0.CopyTo(5, ms1);
				Assert.Equal(8L, ms0.Length);
				Assert.Equal(5L, ms1.Length);

				var b0 = ms0.ToArray();
				var b1 = ms1.ToArray();
				for (int i = 0; i != ms1.Length; ++i)
					Assert.Equal(b0[i], b1[i]);
			}
		}
	}
}
#endif
