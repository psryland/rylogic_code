//***************************************************
// Byte Array Functions
//  Copyright (c) Rylogic Ltd 2010
//***************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Rylogic.Common;
using Rylogic.Utility;

namespace Rylogic.Extn
{
	public static class Array_
	{
		/// <summary>Create a new array of constructed classes</summary>
		public static T[] New<T>(int length) where T : new()
		{
			return New(length, i => new T());
		}

		/// <summary>Create a new 2-dimensional array of constructed classes</summary>
		public static T[,] New<T>(int col_count, int row_count) where T : new()
		{
			return New(col_count, row_count, (c,r) => new T());
		}

		/// <summary>Create a new array of length 'length' initialised by 'factory' for each element</summary>
		public static T[] New<T>(int length, Func<int,T> factory)
		{
			var arr = new T[length];
			for (int i = 0; i != arr.Length; ++i)
				arr[i] = factory(i);

			return arr;
		}

		/// <summary>Create a new 2-dimensional array initialised by 'factory' for each element</summary>
		public static T[,] New<T>(int col_count, int row_count, Func<int,int,T> construct)
		{
			var arr = new T[col_count, row_count];
			for (var r = 0; r != row_count; ++r)
				for (var c = 0; c != col_count; ++c)
					arr[c,r] = construct(c,r);

			return arr;
		}

		/// <summary>Create a new array of length 'length' initialised with 'init' for each element</summary>
		public static T[] New<T>(int length, T init)
		{
			return New(length, i => init);
		}

		/// <summary>Create a new 2-dimensional array initialised with 'init' for each element</summary>
		public static T[,] New<T>(int col_count, int row_count, T init)
		{
			return New(col_count, row_count, (c,r) => init);
		}

		/// <summary>Reset the array to default</summary>
		public static void Clear<T>(this T[] arr)
		{
			Array.Clear(arr, 0, arr.Length);
		}
		public static void Clear<T>(this T[] arr, int index, int length)
		{
			Array.Clear(arr, index, length);
		}

		/// <summary>Create a shallow copy of this array</summary>
		public static T[] Dup<T>(this T[] arr)
		{
			return (T[])arr.Clone();
		}
		public static T[] Dup<T>(this T[] arr, int ofs, int count)
		{
			var result = new T[count];
			Array.Copy(arr, ofs, result, 0, count);
			return result;
		}

		/// <summary>Resize an array filling new elements using 'factory'</summary>
		public static T[] Resize<T>(this T[] arr, int new_size, Func<int,T>? factory = null)
		{
			var old_size = arr.Length;
			Array.Resize(ref arr, new_size);

			// Fill new elements
			if (factory != null)
			{
				for (int i = old_size; i < new_size; ++i)
					arr[i] = factory(i);
			}

			// Return for method chaining
			return arr;
		}

		/// <summary>Join two arrays</summary>
		public static T[] Append<T>(this T[] lhs, params T[] rhs)
		{
			// This is basically 'Concat', except that 'lhs' is returned, not a new array instance.
			// Don't call the method Concat because the behaviour is different to System.Linq.Concat.
			var len = lhs.Length;
			return Resize(lhs, lhs.Length + rhs.Length, i => rhs[i - len]);
		}

		/// <summary>Returns the index of 'what' in the array</summary>
		public static int IndexOf<T>(this T[] arr, T what)
		{
			int idx = 0;
			foreach (var i in arr)
			{
				if (!Equals(i,what)) ++idx;
				else return idx;
			}
			return -1;
		}

		/// <summary>Compare sub-ranges within arrays for value equality</summary>
		public static bool SequenceEqual<T>(this T[] lhs, T[] rhs, int len)
		{
			return SequenceEqual(lhs,rhs,0,0,len);
		}

		/// <summary>Compare sub-ranges within arrays for value equality</summary>
		public static bool SequenceEqual<T>(this T[] lhs, T[] rhs, int ofs0, int ofs1, int len)
		{
			for (int i = ofs0, j = ofs1; len-- != 0; ++i, ++j)
				if (!Equals(lhs[i], rhs[i]))
					return false;
			return true;
		}

		/// <summary>Value equality for the contents of two arrays</summary>
		public static bool Equal<T>(T[] lhs, T[] rhs)
		{
			if (lhs == rhs) return true;
			if (lhs == null || rhs == null) return false;
			if (lhs.Length != rhs.Length) return false;

			var task = Parallel.For(0, lhs.Length, (i, loop_state) =>
			{
				if (Equals(lhs[i], rhs[i])) return;
				loop_state.Stop();
			});

			return task.IsCompleted;
		}
		public static bool Equal<T>(T[,] lhs, T[,] rhs)
		{
			if (lhs == rhs) return true;
			if (lhs == null || rhs == null) return false;
			if (lhs.Length != rhs.Length) return false;

			var dim0 = lhs.GetLength(0);
			if (dim0 != rhs.GetLength(0))
				return false;

			var task = Parallel.For(0, lhs.Length, (k, loop_state) =>
			{
				var i = k % dim0;
				var j = k / dim0;
				if (Equals(lhs[i,j], rhs[i,j])) return;
				loop_state.Stop();
			});

			return task.IsCompleted;
		}
		public static bool Equal(Array lhs, Array rhs)
		{
			if (lhs == rhs) return true;
			if (lhs == null || rhs == null) return false;
			if (lhs.Length != rhs.Length) return false;
			if (lhs.Rank != rhs.Rank) return false;

			// Get the dimensions of the array and the elements per dimension
			var dim = new int[lhs.Rank];
			var div = new int[lhs.Rank];
			var next = 1;
			for (int i = 0; i != dim.Length; ++i)
			{
				if (lhs.GetLength(i) != rhs.GetLength(i)) return false;
				dim[i] = lhs.GetLength(i);
				div[i] = next;
				next = next * dim[i];
			}

			// Compare all elements in parallel
			var task = Parallel.For(0, lhs.Length, (k, loop_state) =>
			{
				var indices = new int[dim.Length];
				for (int i = 0; i != indices.Length; ++i)
					indices[i] = (k / div[i]) % dim[i];

				if (Equals(lhs.GetValue(indices), rhs.GetValue(indices))) return;
				loop_state.Stop();
			});

			return task.IsCompleted;
		}
		public static bool Equal(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
		{
			// 'byte[]' is implicitly convertable to ReadOnlySpan<byte>
			// This is comparible to memcmp in speed
			return lhs.SequenceEqual(lhs);
		}
		public static bool Equal(this byte[] arr, byte[] rhs, int start, int length)
		{
			return Equal(arr, new Span<byte>(rhs, start, length));
		}

		/// <summary>Compare two ranges within a byte array</summary>
		public static int Compare(byte[] lhs, int lstart, int llength, byte[] rhs, int rstart, int rlength)
		{
			for (; llength != 0 && rlength != 0; ++lstart, ++rstart, --llength, --rlength)
			{
				if (lhs[lstart] == rhs[rstart]) continue;
				return
					lhs[lstart] < rhs[rstart] ? -1 :
					lhs[lstart] > rhs[rstart] ? +1 :
					0;
			}
			return
				llength < rlength ? -1 :
				llength > rlength ? +1 :
				0;
		}

		/// <summary>BitConverter byte[] to type conversion</summary>
		public static T As<T>(this byte[] arr) where T : struct => As<T>(arr, 0);
		public static T As<T>(this byte[] arr, int start) where T : struct => As<T>(arr, start, arr.Length - start);
		public static T As<T>(this byte[] arr, int start, int len) where T : struct
		{
			switch (Type.GetTypeCode(typeof(T)))
			{
				case TypeCode.Byte:    return (T)(object)(byte)arr[start];
				case TypeCode.SByte:   return (T)(object)(sbyte)arr[start];
				case TypeCode.UInt16:  return (T)(object)BitConverter.ToUInt16(arr, start);
				case TypeCode.UInt32:  return (T)(object)BitConverter.ToUInt32(arr, start);
				case TypeCode.UInt64:  return (T)(object)BitConverter.ToUInt64(arr, start);
				case TypeCode.Int16:   return (T)(object)BitConverter.ToInt16(arr, start);
				case TypeCode.Int32:   return (T)(object)BitConverter.ToInt32(arr, start);
				case TypeCode.Int64:   return (T)(object)BitConverter.ToInt64(arr, start);
				case TypeCode.Single:  return (T)(object)BitConverter.ToSingle(arr, start);
				case TypeCode.Double:  return (T)(object)BitConverter.ToDouble(arr, start);
				case TypeCode.Boolean: return (T)(object)BitConverter.ToBoolean(arr, start);
				case TypeCode.Char:    return (T)(object)BitConverter.ToChar(arr, start);
				case TypeCode.String:  return (T)(object)Encoding.UTF8.GetString(arr, start, len);
				default:
				{
					if (start + len < Marshal.SizeOf(typeof(T)))
						throw new Exception($"As<T>: Insufficient data. {Marshal.SizeOf(typeof(T))} bytes required, {len - start} available");

					using var handle = GCHandle_.Alloc(arr, GCHandleType.Pinned);
					return Marshal.PtrToStructure<T>(handle.Handle.AddrOfPinnedObject() + start);
				}
			}
		}

		/// <summary>Return the checksum of this array of bytes</summary>
		public static int Crc32(this byte[] arr, uint initial_value = 0xFFFFFFFF)
		{
			return CRC32.Compute(arr, initial_value);
		}

		/// <summary>Return the MD5 hash of this array of bytes. The hash is a 16byte array</summary>
		public static byte[] Md5(this byte[] arr)
		{
			using var alg = System.Security.Cryptography.MD5.Create();
			return alg.ComputeHash(arr);
		}

		/// <summary>Set bytes in the array to 'value' using native 'memset'</summary>
		public static byte[] Memset(this byte[] arr, byte value)
		{
			int block = 32, index = 0;
			int length = Math.Min(block, arr.Length);

			// Fill the initial array
			while (index < length)
				arr[index++] = value;

			// Make use of 'BlockCopy' for performance
			for (length = arr.Length; index < length;)
			{
				Buffer.BlockCopy(arr, 0, arr, index, Math.Min(block, length-index));
				index += block;
				block *= 2;
			}

			return arr;
		}

		/// <summary>Convert this byte array into a string, without trying to encode. (Opposite of string.ToBytes())</summary>
		public static string ToStringWithoutEncoding(this byte[] arr)
		{
			var chars = new char[arr.Length / sizeof(char)];
			Buffer.BlockCopy(arr, 0, chars, 0, arr.Length);
			return new string(chars);
		}

		/// <summary>Convert a byte array to a hex string. e.g A3 FF 12 4D etc</summary>
		public static string ToHexString(this byte[] arr, int start = 0, int count = int.MaxValue, string sep = " ", string line_sep = "\n", int width = 16)
		{
			return Util.ToHexString(arr, start, count, sep, line_sep, width);
		}
	}

	/// <summary>A sub range within an array</summary>
	public struct ArraySlice<T> :IList<T> ,ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyList<T>, IReadOnlyCollection<T>
	{
		// This class exists because ArraySegment doesn't provide an indexer.
		// Yes, seriously.. ffs

		public ArraySlice(T[] arr)
			:this(arr,0,arr.Length)
		{}
		public ArraySlice(T[] arr, int offset, int length)
		{
			if (arr == null)
				throw new ArgumentNullException(nameof(arr));
			if (offset < 0 || offset > arr.Length)
				throw new ArgumentOutOfRangeException(nameof(offset), $"Offset out of range [0,{arr.Length}]");
			if (length < 0 || length > arr.Length - offset)
				throw new ArgumentOutOfRangeException(nameof(offset), $"Length out of range [0,{arr.Length - offset}]");

			SourceArray = arr;
			Offset = offset;
			Length = length;
		}

		/// <summary>The original array</summary>
		public T[] SourceArray { get; private set; }

		/// <summary>The index offset into 'Array' for the start of this slice</summary>
		public int Offset { get; private set; }

		/// <summary>The number of elements in this array slice</summary>
		public int Length { get; private set; }
		public int Count { get { return Length; } }

		/// <summary>Get/Set by index</summary>
		public T this[int idx]
		{
			get { return SourceArray[Offset + idx]; }
			set { SourceArray[Offset + idx] = value; }
		}

		/// <summary>Make a copy of the slice in a new buffer</summary>
		public T[] ToArray()
		{
			// Note: don't provide implicit conversion to T[] because
			// that makes a copy which could be unexpected.
			var buf = new T[Length];
			Array.Copy(SourceArray, Offset, buf, 0, Length);
			return buf;
		}

		/// <summary>Implicit conversion to/from ArraySegment</summary>
		public static implicit operator ArraySegment<T>(ArraySlice<T> s) { return new ArraySegment<T>(s.SourceArray, s.Offset, s.Count); }
		public static implicit operator ArraySlice<T>(ArraySegment<T> s) { return new ArraySlice<T>(s.Array!, s.Offset, s.Count); }

		#region IList
		public bool IsReadOnly => true;
		public int IndexOf(T item)
		{
			var idx = Array.IndexOf(SourceArray, item, Offset, Length);
			return idx >= 0 ? idx - Offset : idx;
		}
		public void Insert(int index, T item)
		{
			throw new NotSupportedException("Cannot insert items into an array slice");
		}
		public bool Remove(T item)
		{
			throw new NotSupportedException("Cannot remove items from an array slice");
		}
		public void RemoveAt(int index)
		{
			throw new NotSupportedException("Cannot remove items from an array slice");
		}
		public void Add(T item)
		{
			throw new NotSupportedException("Cannot add items to an array slice");
		}
		public void Clear()
		{
			Length = 0;
		}
		public bool Contains(T item)
		{
			return IndexOf(item) != -1;
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			Array.Copy(SourceArray, Offset, array, arrayIndex, Length);
		}
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = Offset, iend = Offset + Length; i != iend; ++i)
				yield return SourceArray[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<T>)this).GetEnumerator();
		}
		#endregion
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Linq;
	using System.Runtime.InteropServices;
	using Extn;

	[TestFixture]
	public class TestArrayExtns
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		internal struct Thing
		{
			public int m_int;
			public char m_char;
			public byte m_byte;
		}

		[Test]
		public void ArrayExtns()
		{
			var a0 = new[] { 1, 2, 3, 4 };
			var A0 = a0.Dup();

			Assert.Equal(typeof(int[]), A0.GetType());
			Assert.True(A0.SequenceEqual(a0));

			Assert.Equal(2, A0.IndexOf(3));
			Assert.Equal(-1, A0.IndexOf(5));

			for (var err = 0; err != 2; ++err)
			{
				// 1-Dimensional array value equality
				var a1 = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 };
				var A1 = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 + err };
				Assert.Equal(Array_.Equal(a1, A1), err == 0);

				// 2-Dimensional array value equality
				var v2 = 0.0;
				var a2 = new double[3, 4];
				var A2 = new double[3, 4];
				for (int i = 0; i != a2.GetLength(0); ++i)
					for (int j = 0; j != a2.GetLength(1); ++j)
					{
						a2[i, j] = v2;
						A2[i, j] = v2 + err;
						v2 += 1.0;
					}
				Assert.Equal(Array_.Equal(a2, A2), err == 0);

				// N-Dimensional array value equality
				var v3 = 0.0;
				var a3 = new double[2, 3, 4];
				var A3 = new double[2, 3, 4];
				for (int i = 0; i != a3.GetLength(0); ++i)
					for (int j = 0; j != a3.GetLength(1); ++j)
						for (int k = 0; k != a3.GetLength(2); ++k)
						{
							a3[i, j, k] = v3;
							A3[i, j, k] = v3 + err;
							v3 += 1.0;
						}
				Assert.Equal(Array_.Equal(a3, A3), err == 0);
			}
		}
		[Test]
		public void ByteArrayAs()
		{
			var b0 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
			var b1 = new byte[] { 0x00, 0x00, 0x80, 0x3f };
			var b2 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F };
			Assert.Equal((byte)0x01, b0.As<byte>());
			Assert.Equal((ushort)0x0201, b0.As<ushort>());
			Assert.Equal((uint)0x04030201U, b0.As<uint>());
			Assert.Equal(0x0807060504030201UL, b0.As<ulong>());
			Assert.Equal((sbyte)0x01, b0.As<sbyte>());
			Assert.Equal((short)0x0201, b0.As<short>());
			Assert.Equal(0x04030201, b0.As<int>());
			Assert.Equal(0x0807060504030201L, b0.As<long>());
			Assert.Equal(1f, b1.As<float>());
			Assert.Equal(1.0, b2.As<double>());
			Assert.Equal(true, b0.As<bool>());
			Assert.Equal((char)0x0201, b0.As<char>());

#if false
			Assert.Equal((byte)0x01            ,b0.AsUInt8 ());
			Assert.Equal((ushort)0x0201        ,b0.AsUInt16());
			Assert.Equal((uint)0x04030201U     ,b0.AsUInt32());
			Assert.Equal(0x0807060504030201UL  ,b0.AsUInt64());
			Assert.Equal((sbyte)0x01           ,b0.AsInt8  ());
			Assert.Equal((short)0x0201         ,b0.AsInt16 ());
			Assert.Equal(0x04030201            ,b0.AsInt32 ());
			Assert.Equal(0x0807060504030201L   ,b0.AsInt64 ());
			Assert.Equal(1f                    ,b1.AsFloat ());
			Assert.Equal(1.0                   ,b2.AsDouble());
			Assert.Equal(true                  ,b0.AsBool  ());
			Assert.Equal((char)0x0201          ,b0.AsChar  ());
#endif
		}
		[Test]
		public void ByteArrayAsStruct()
		{
			var b0 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x41, 0xAB };
			var thing = b0.As<Thing>();

			Assert.Equal(0x04030201, thing.m_int);
			Assert.Equal('A', thing.m_char);
			Assert.Equal((byte)0xab, thing.m_byte);
		}
		[Test]
		public void ByteArrayCompare()
		{
			byte[] lhs = new byte[] { 1, 2, 3, 4, 5 };
			byte[] rhs = new byte[] { 3, 4, 5, 6, 7 };

			Assert.Equal(-1, Array_.Compare(lhs, 0, 5, rhs, 0, 5));
			Assert.Equal(0, Array_.Compare(lhs, 2, 3, rhs, 0, 3));
			Assert.Equal(1, Array_.Compare(lhs, 3, 2, rhs, 0, 2));
			Assert.Equal(-1, Array_.Compare(lhs, 2, 3, rhs, 0, 4));
			Assert.Equal(1, Array_.Compare(lhs, 2, 3, rhs, 0, 2));
		}
	}
}
#endif
