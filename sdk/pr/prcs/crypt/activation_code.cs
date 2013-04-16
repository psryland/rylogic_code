using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using pr.util;

namespace pr.common
{
	// Licensing
	//  Activation Code:
	//    Generate a number where the first part is a random number (seed) and the
	//    second part is the first part hashed and signed using the private key.
	//    In the app, load the public key from a resource, hash and encrypt the
	//    seed using the public key, it should equal the second part of the key
	public static class ActivationCode
	{
		private static readonly int SeedLength = Guid.Empty.ToByteArray().Length;

		/// <summary>Returns true if the code is made up of valid characters</summary>
		public static bool CheckChars(string code32)
		{
			return Base32Encoding.IsBase32String(code32);
		}

		/// <summary>Returns true if 'code' has the correct number of characters</summary>
		public static bool CheckLength(string code32, string public_key)
		{
			var alg = RSAService(public_key);
			var code = Base32Encoding.ToBytes(Base32Encoding.Sanitise(code32));
			return code.Length == SeedLength + alg.KeySize/8 + 1;
		}

		/// <summary>Check that the activation code is self consistent</summary>
		public static bool CheckCrc(string code32)
		{
			var code = Base32Encoding.ToBytes(Base32Encoding.Sanitise(code32));
			if (code.Length == 0) return false;
			byte crc = CalcCrc(code, 0, code.Length - 1);
			return crc == code[code.Length - 1];
		}

		/// <summary>Returns true if 'code32' is valid given 'public' key</summary>
		public static bool Validate(string code32, string public_key)
		{
			// Create the crypto service
			var alg = RSAService(public_key);
			var sig_length = alg.KeySize / 8;
			
			var code = Base32Encoding.ToBytes(Base32Encoding.Sanitise(code32));
			if (code.Length != SeedLength + sig_length + 1)
				return false;
			
			var seed   = Util.SubRange(code, 0, SeedLength);
			var sig    = Util.SubRange(code, SeedLength, code.Length - SeedLength - 1);
			
			bool valid = alg.VerifyData(seed, new SHA1CryptoServiceProvider(), sig);
			return valid;
		}

		/// <summary>Generates an activation code using the provided private key</summary>
		public static string Generate(string private_key)
		{
			// Create the crypto service
			var alg = RSAService(private_key);
			
			// Start with a guid as the seed
			var seed = Guid.NewGuid().ToByteArray();
			Debug.Assert(seed.Length == SeedLength);
			
			// Generate a signature for the seed
			var sig = alg.SignData(seed, new SHA1CryptoServiceProvider());
			
			// Combine the seed and signature
			var code = new byte[seed.Length + sig.Length + 1];
			Array.Copy(seed, 0, code, 0, seed.Length);
			Array.Copy(sig, 0, code, seed.Length, sig.Length);
			code[seed.Length + sig.Length] = CalcCrc(code, 0, code.Length - 1); // Add a simple crc
			
			var code32 = Base32Encoding.ToString(code);
			return code32;
		}

		/// <summary>Returns an instance of the RSA service</summary>
		private static RSACryptoServiceProvider RSAService(string key)
		{
			var alg = new RSACryptoServiceProvider();
			alg.FromXmlString(key);
			return alg;
		}

		/// <summary>Calculates a crude 8-bit crc for a range of bytes</summary>
		private static byte CalcCrc(byte[] data, int start, int length)
		{
			byte crc = 0x55;
			const byte poly = 0xB7;
			for (int i = start, iend = start + length; i != iend; ++i)
				crc = (byte)(((crc ^ poly) >> 1) ^ data[i]);
			return crc;
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using common;
	using crypt;
	
	[TestFixture] internal partial class UnitTests
	{
		internal static class TestActivationCode
		{
			[Test] public static void ActivationCodeGen()
			{
				// Generate a public and private key.
				// Save the public key in the app (in a resource file)
				// Save the private key somewhere safe, you need that to generate more code numbers for the app
				string pub, priv;
				Crypt.GenerateRSAKeyPair(out pub, out priv, 384);
				Assert.AreNotEqual(pub, priv);
			
				// Generate a code number for the app
				var key = ActivationCode.Generate(priv);
			
				// Pretend this is in the app:
			
				// Quick check that the key is entered correctly
				var valid = ActivationCode.CheckCrc(key);
				Assert.IsTrue(valid);

				// Validate the code number
				valid = ActivationCode.Validate(key, pub);
				Assert.IsTrue(valid);
			
				{
					var sb = new StringBuilder(key); sb[2] = sb[2] == 'A' ? 'B' : 'A';
					var key1 = sb.ToString();
					valid = ActivationCode.Validate(key1, pub);
					Assert.IsFalse(valid);
				}
				{
					var sb = new StringBuilder(key); var tmp = sb[2]; sb[2] = sb[3]; sb[3] = tmp;
					var key1 = sb.ToString();
					valid = ActivationCode.Validate(key1, pub);
					Assert.IsFalse(valid);
				}
			}
		}
	}
}
#endif
