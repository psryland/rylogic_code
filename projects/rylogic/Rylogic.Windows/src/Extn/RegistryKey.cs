using System;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Rylogic.Extn;

namespace Rylogic.Windows.Extn
{
	public static class RegistryKey_
	{
		/// <summary>Get the key kind and (expanded) value</summary>
		public static (RegistryValueKind type, object? value) Value(this RegistryKey key, string name)
		{
			return (key.GetValueKind(name), key.GetValue(name));
		}

		/// <summary>Decode byte data from a registry key</summary>
		public static object? Decode(RegistryValueKind kind, byte[] data)
		{
			switch (kind)
			{
				case RegistryValueKind.None:
				{
					return null;
				}
				case RegistryValueKind.Unknown:
				{
					return data;
				}
				case RegistryValueKind.String:
				{
					return Encoding.Unicode.GetString(data, 0, data.Length).TrimEnd('\0');
				}
				case RegistryValueKind.ExpandString:
				{
					var re = new Regex("%(.*?)%", RegexOptions.None);
					var str = Encoding.Unicode.GetString(data, 0, data.Length).TrimEnd('\0');
					return re.Replace(str, m => Environment.GetEnvironmentVariable(m.Groups[1].Value) ?? string.Empty);
				}
				case RegistryValueKind.Binary:
				{
					return data;
				}
				case RegistryValueKind.DWord:
				{
					return BitConverter.ToUInt32(data, 0);
				}
				case RegistryValueKind.MultiString:
				{
					var multi_str = Encoding.Unicode.GetString(data, 0, data.Length);
					var strings = Str_.DecodeStringArray(multi_str);
					return strings;
				}
				case RegistryValueKind.QWord:
				{
					return BitConverter.ToUInt64(data, 0);
				}
				default:
				{
					throw new Exception($"Registry property kind {kind} unsupported");
				}
			}
		}
	}
}
