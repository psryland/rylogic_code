using System;
using System.Security.Cryptography;

namespace pr.common
{
	// Licencing
	//  Serial number:
	//    Generate a number where the first part is a random number (seed) and the
	//    second part is the first part hashed and signed using the private key.
	//    In the app, load the public key from a resource, hash and encrypt the
	//    seed using the public key, it should equal the second part of the key
	public static class SerialNumber
	{
		/// <summary>Generates a public/private key pair as xml strings</summary>
		public static void GenerateKeys(out string pub, out string priv)
		{
			var rsa = new RSACryptoServiceProvider();
			pub  = rsa.ToXmlString(false);
			priv = rsa.ToXmlString(true);
		}

		/// <summary>Generates a serial number using the provided private key</summary>
		public static string Generate(string private_key)
		{
			// Start with a guid as the seed
			var seed = Guid.NewGuid().ToByteArray();
			
			// Hash and encrypt the seed
			var alg = new RSACryptoServiceProvider();
			alg.FromXmlString(private_key);
			var hseed = alg.SignData(seed, new SHA1CryptoServiceProvider());
			
			// Combine the seed with the hashed seed
			string result = Convert.ToBase64String(seed) + Convert.ToBase64String(hseed);
			return result;
		}

		/// <summary>Returns true if 'serial' is valid given 'public' key</summary>
		public static bool Validate(string serial, string public_key)
		{
			var len = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Length;
			
			// Get the base seed and the hashed and encrypted seed from the serial number
			var seed = Convert.FromBase64String(serial.Substring(0, len));
			var hseed = Convert.FromBase64String(serial.Substring(len));
			
			// Validate the serial by checking that the seed hashed and encrypted with the public key equals 'hseed'
			var alg = new RSACryptoServiceProvider();
			alg.FromXmlString(public_key);
			bool valid = alg.VerifyData(seed, new SHA1CryptoServiceProvider(), hseed);
			return valid;
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using pr.common;
		
	[TestFixture] internal partial class UnitTests
	{
		[Test] public static void TestSerialNumber()
		{
			// Generate a publc and private key.
			// Save the public key in the app (in a resource file)
			// Save the private key somewhere safe, you need that to generate more serial numbers for the app
			string pub, priv;
			SerialNumber.GenerateKeys(out pub, out priv);
			
			// Generate a serial number for the app
			var key = SerialNumber.Generate(priv);
			
			// Pretend this is in the app:
			// Validate the serial number
			bool valid = SerialNumber.Validate(key, pub);
			
			Assert.AreNotEqual(pub, priv);
			Assert.IsTrue(valid);
			
			key = key.Replace('a','A');
			valid = SerialNumber.Validate(key, pub);
			Assert.IsFalse(valid);
		}
	}
}
#endif
	