using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using pr.crypt;
using pr.util;

namespace pr.crypt
{
	public static class Crypt
	{
		/// <summary>The id used to check whether a signature is present already</summary>
		public static readonly byte[] SignatureId = new Guid("ACDC91BB-6E26-4F33-9F14-7C9A3F928750").ToByteArray();

		/// <summary>Generates a public/private key pair as xml strings</summary>
		public static void GenerateRSAKeyPair(out string pub, out string priv, int key_size = 1024)
		{
			var rsa = new RSACryptoServiceProvider(key_size);
			pub  = rsa.ToXmlString(false);
			priv = rsa.ToXmlString(true);
		}

		/// <summary>Attaches a signature to the end of a file</summary>
		public static void SignFile(string filepath, string private_key)
		{
			// Create the crypto service
			var alg = new RSACryptoServiceProvider();
			alg.FromXmlString(private_key);
			var key_size_bytes = alg.KeySize / 8;
			
			// Read the file into memory (no fancy streaming...)
			var filebuf = File.ReadAllBytes(filepath);
			var length = filebuf.Length;
			
			// The total length of the signature in bytes
			var sig_length = key_size_bytes + SignatureId.Length;
			
			// Check whether the file has an existing signature
			// If so, shorten the length of file buf that's signed
			if (length >= sig_length && Util.Compare(
				filebuf, length - sig_length, SignatureId.Length,
				SignatureId, 0, SignatureId.Length) == 0)
				length -= sig_length;
			
			// Generate the signature
			var buf = filebuf.Take(length).ToArray();
			var sig = alg.SignData(buf, new SHA1CryptoServiceProvider());
			
			// Append the signature to the end of the file
			using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Write))
			{
				fs.Seek(length, SeekOrigin.Begin);
				fs.Write(SignatureId, 0, SignatureId.Length);
				fs.Write(sig, 0, sig.Length);
			}
		}

		/// <summary>Verify that a file has a valid signature</summary>
		public static bool Validate(string filepath, string public_key, bool thrw = true)
		{
			// Create the crypto service
			var alg = new RSACryptoServiceProvider();
			alg.FromXmlString(public_key);
			var key_size_bytes = alg.KeySize / 8;
			
			// Read the file into memory (no fancy streaming...)
			var filebuf = File.ReadAllBytes(filepath);
			var length = filebuf.Length;
			
			// The total length of the signature in bytes
			var sig_length = key_size_bytes + SignatureId.Length;
			
			// Check whether the file has a signature
			if (length >= sig_length && Util.Compare(
				filebuf, length - sig_length, SignatureId.Length,
				SignatureId, 0, SignatureId.Length) == 0)
				length -= sig_length;
			else
			{
				if (thrw) throw new VerificationException("File is not signed");
				return false;
			}
			
			// Validate the signature
			var buf = filebuf.Take(length).ToArray();
			var sig = filebuf.Skip(length + SignatureId.Length).Take(key_size_bytes).ToArray();
			bool valid = alg.VerifyData(buf, new SHA1CryptoServiceProvider(), sig);
			
			if (thrw && !valid) throw new VerificationException("File signature incorrect");
			return valid;
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;

	[TestFixture] public static partial class UnitTests
	{
		[Test] public static void TestCrypt_SignFile()
		{
			// These would normally be stored as files and read into
			// a string using File.ReadAllText()
			string pub,prv;
			Crypt.GenerateRSAKeyPair(out pub, out prv);
			
			const string filepath = "tmp.bin";
			var data = new byte[]{0,1,2,3,4,5,6,7,8,9};
			
			// Create a file to sign
			using (var fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
				fs.Write(data, 0, data.Length);
			
			// Sign the file
			Crypt.SignFile(filepath, prv);
			
			// Check that the start of the file is the same
			var buf1 = File.ReadAllBytes(filepath);
			Assert.IsTrue(Util.Compare(buf1, 0, data.Length, data, 0, data.Length) == 0);
			
			// Verify the signed file using the public key
			Assert.IsTrue(Crypt.Validate(filepath, pub));
			
			// Sign it again to make sure the function handles the case that the signature is already there
			Crypt.SignFile(filepath, prv);
			
			// Check that the whole file is the same
			var buf2 = File.ReadAllBytes(filepath);
			Assert.IsTrue(Util.Compare(buf2, 0, buf2.Length, buf1, 0, buf1.Length) == 0);
			
			// Verify the signed file using the public key
			Assert.IsTrue(Crypt.Validate(filepath, pub));
		}
	}
}
#endif