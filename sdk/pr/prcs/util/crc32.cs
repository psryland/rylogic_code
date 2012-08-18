// Tamir Khason http://khason.net/
//
// Released under MS-PL : 6-Apr-09

using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace pr.util
{
	/// <summary>Implements a 32-bits cyclic redundancy check (CRC) hash algorithm.</summary>
	/// <remarks>This class is not intended to be used for security purposes. For security applications use MD5, SHA1, SHA256, SHA384,
	/// or SHA512 in the System.Security.Cryptography namespace.</remarks>
	public class CRC32 :HashAlgorithm
	{
		private const uint m_all_ones = 0xffffffff;
		private static readonly CRC32 DefaultCrc;
		private static readonly Hashtable Crc32TablesCache;
		private readonly uint[] m_crc32_table;
		private uint m_crc;
		
		/// <summary>Gets the default polynomial (used in WinZip, Ethernet, etc.)</summary>
		/// <remarks>The default polynomial is a bit-reflected version of the standard polynomial 0x04C11DB7 used by WinZip, Ethernet, etc.</remarks>
		public static readonly uint DefaultPolynomial = 0xEDB88320; // Bitwise reflection of 0x04C11DB7;
		
		// static constructor
		static CRC32()
		{
			Crc32TablesCache = Hashtable.Synchronized(new Hashtable());
			DefaultCrc = new CRC32();
		}

		/// <summary>Creates a CRC32 object using the <see cref="DefaultPolynomial"/>.</summary>
		public CRC32() :this(DefaultPolynomial)
		{}

		/// <summary>Creates a CRC32 object using the specified polynomial.</summary>
		/// <remarks>The polynomial should be supplied in its bit-reflected form. <see cref="DefaultPolynomial"/>.</remarks>
		public CRC32(uint polynomial)
		{
			HashSizeValue = 32;
			m_crc32_table = (uint[])Crc32TablesCache[polynomial];
			if (m_crc32_table == null)
			{
				m_crc32_table = BuildCrc32Table(polynomial);
				Crc32TablesCache.Add(polynomial, m_crc32_table);
			}
			Initialize();
		}
	
		/// <summary>Initializes an implementation of HashAlgorithm.</summary>
		public override sealed void Initialize()
		{
			m_crc = m_all_ones;
		}
	
		/// <summary>Routes data written to the object into the hash algorithm for computing the hash.</summary>
		protected override void HashCore(byte[] buffer, int offset, int count)
		{
			for (int i = offset; i < count; i++)
			{
				ulong ptr = (m_crc & 0xFF) ^ buffer[i];
				m_crc >>= 8;
				m_crc ^= m_crc32_table[ptr];
			}
		}
	
		/// <summary>Finalizes the hash computation after the last data is processed by the cryptographic stream object.</summary>
		protected override byte[] HashFinal()
		{
			byte[] finalHash = new byte[4];
			ulong finalCRC = m_crc ^ m_all_ones;
		
			finalHash[0] = (byte)((finalCRC >> 0) & 0xFF);
			finalHash[1] = (byte)((finalCRC >> 8) & 0xFF);
			finalHash[2] = (byte)((finalCRC >> 16) & 0xFF);
			finalHash[3] = (byte)((finalCRC >> 24) & 0xFF);
		
			return finalHash;
		}
	
		/// <summary>Computes the CRC32 value for the given ASCII string using the <see cref="DefaultPolynomial"/>.</summary>
		public static int Compute(string asciiString)
		{
			DefaultCrc.Initialize();
			return ToInt32(DefaultCrc.ComputeHash(asciiString));
		}
	
		/// <summary>Computes the CRC32 value for the given input stream using the <see cref="DefaultPolynomial"/>.</summary>
		public static int Compute(Stream inputStream)
		{
			DefaultCrc.Initialize();
			return ToInt32(DefaultCrc.ComputeHash(inputStream));
		}
	
		/// <summary>Computes the CRC32 value for the input data using the <see cref="DefaultPolynomial"/>.</summary>
		public static int Compute(byte[] buffer)
		{
			DefaultCrc.Initialize();
			return ToInt32(DefaultCrc.ComputeHash(buffer));
		}
	
		/// <summary>Computes the hash value for the input data using the <see cref="DefaultPolynomial"/>.</summary>
		public static int Compute(byte[] buffer, int offset, int count)
		{
			DefaultCrc.Initialize();
			return ToInt32(DefaultCrc.ComputeHash(buffer, offset, count));
		}
	
		/// <summary>Computes the hash value for the given ASCII string.</summary>
		/// <remarks>The computation preserves the internal state between the calls, so it can be used for computation of a stream data.</remarks>
		public byte[] ComputeHash(string asciiString)
		{
			byte[] rawBytes = Encoding.ASCII.GetBytes(asciiString);
			return ComputeHash(rawBytes);
		}
	
		/// <summary>Computes the hash value for the given input stream.</summary>
		/// <remarks>The computation preserves the internal state between the calls, so it can be used for computation of a stream data.</remarks>
		new public byte[] ComputeHash(Stream inputStream)
		{
			byte[] buffer = new byte[4096];
			int bytesRead;
			while ((bytesRead = inputStream.Read(buffer, 0, 4096)) > 0)
			{
				HashCore(buffer, 0, bytesRead);
			}
			return HashFinal();
		}
	
		/// <summary>Computes the hash value for the input data.</summary>
		/// <remarks>The computation preserves the internal state between the calls, so it can be used for computation of a stream data.</remarks>
		new public byte[] ComputeHash(byte[] buffer)
		{
			return ComputeHash(buffer, 0, buffer.Length);
		}
	
		/// <summary>Computes the hash value for the input data.</summary>
		/// <remarks>The computation preserves the internal state between the calls, so it can be used for computation of a stream data.</remarks>
		new public byte[] ComputeHash(byte[] buffer, int offset, int count)
		{
			HashCore(buffer, offset, count);
			return HashFinal();
		}

		/// <summary>Builds a crc32 table given a polynomial</summary>
		private static uint[] BuildCrc32Table(uint polynomial)
		{
			uint[] table = new uint[256];
		
			// 256 values representing ASCII character codes.
			for (int i = 0; i < 256; i++)
			{
				uint crc = (uint)i;
				for (int j = 8; j > 0; j--)
				{
					if ((crc & 1) == 1)
						crc = (crc >> 1) ^ polynomial;
					else
						crc >>= 1;
				}
				table[i] = crc;
			}
		
			return table;
		}
	
		private static int ToInt32(byte[] buffer)
		{
			return BitConverter.ToInt32(buffer, 0);
		}
	}
}