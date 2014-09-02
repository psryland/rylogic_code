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
	/// <summary>Implements a 16-bits cyclic redundancy check (CRC) hash algorithm.</summary>
	/// <remarks>This class is not intended to be used for security purposes. For security applications use MD5, SHA1, SHA256, SHA384,
	/// or SHA512 in the System.Security.Cryptography namespace.</remarks>
	public class CRC16 :HashAlgorithm
	{
		private const ushort m_all_ones = 0xffff;
		private static readonly CRC16 DefaultCrc;
		private static readonly Hashtable Crc16TablesCache;
		private readonly ushort[] m_crc16_table;
		private ushort m_crc;

		/// <summary>Gets the default polynomial.</summary>
		public static readonly ushort DefaultPolynomial = 0x8408; // Bit reversion of 0xA001;
	
		// static constructor
		static CRC16()
		{
			Crc16TablesCache = Hashtable.Synchronized(new Hashtable());
			DefaultCrc = new CRC16();
		}

		/// <summary>Creates a CRC16 object using the <see cref="DefaultPolynomial"/>.</summary>
		public CRC16() :this(DefaultPolynomial)
		{}
	
		/// <summary>Creates a CRC16 object using the specified polynomial.</summary>
		public CRC16(ushort polynomial)
		{
			HashSizeValue = 16;
			m_crc16_table = (ushort[])Crc16TablesCache[polynomial];
			if (m_crc16_table == null)
			{
				m_crc16_table = BuildCrc16Table(polynomial);
				Crc16TablesCache.Add(polynomial, m_crc16_table);
			}
			Initialize();
		}

		/// <summary>Initializes an implementation of HashAlgorithm.</summary>
		public override sealed void Initialize()
		{
			m_crc = 0;
		}
	
		/// <summary>Routes data written to the object into the hash algorithm for computing the hash.</summary>
		protected override void HashCore(byte[] buffer, int offset, int count)
		{
			foreach (byte t in buffer)
			{
				byte index = (byte)(m_crc ^ t);
				m_crc = (ushort)((m_crc >> 8) ^ m_crc16_table[index]);
			}
		}

		/// <summary>Finalizes the hash computation after the last data is processed by the cryptographic stream object.</summary>
		protected override byte[] HashFinal()
		{
			byte[] finalHash = new byte[2];
			ushort finalCRC = (ushort)(m_crc ^ m_all_ones);
		
			finalHash[0] = (byte)((finalCRC >> 0) & 0xFF);
			finalHash[1] = (byte)((finalCRC >> 8) & 0xFF);
		
			return finalHash;
		}
	
		/// <summary>Computes the CRC16 value for the given ASCII string using the <see cref="DefaultPolynomial"/>.</summary>
		public static short Compute(string asciiString)
		{
			DefaultCrc.Initialize();
			return ToInt16(DefaultCrc.ComputeHash(asciiString));
		}
	
		/// <summary>Computes the CRC16 value for the given input stream using the <see cref="DefaultPolynomial"/>.</summary>
		public static short Compute(Stream inputStream)
		{
			DefaultCrc.Initialize();
			return ToInt16(DefaultCrc.ComputeHash(inputStream));
		}
	
		/// <summary>Computes the CRC16 value for the input data using the <see cref="DefaultPolynomial"/>.</summary>
		public static short Compute(byte[] buffer)
		{
			DefaultCrc.Initialize();
			return ToInt16(DefaultCrc.ComputeHash(buffer));
		}
	
		/// <summary>Computes the hash value for the input data using the <see cref="DefaultPolynomial"/>.</summary>
		public static short Compute(byte[] buffer, int offset, int count)
		{
			DefaultCrc.Initialize();
			return ToInt16(DefaultCrc.ComputeHash(buffer, offset, count));
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

		// Builds a crc16 table given a polynomial
		private static ushort[] BuildCrc16Table(ushort polynomial)
		{
			// 256 values representing ASCII character codes.
			ushort[] table = new ushort[256];
			for (ushort i = 0; i < table.Length; i++)
			{
				ushort value = 0;
				ushort temp = i;
				for (byte j = 0; j < 8; j++)
				{
					if (((value ^ temp) & 0x0001) != 0)
					{
						value = (ushort)((value >> 1) ^ polynomial);
					}
					else
					{
						value >>= 1;
					}
					temp >>= 1;
				}
				table[i] = value;
			}
			return table;
		}
	
		private static short ToInt16(byte[] buffer)
		{
			return BitConverter.ToInt16(buffer, 0);
		}
	}
}
