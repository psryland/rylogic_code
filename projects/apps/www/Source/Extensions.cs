using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace rylogic.co.nz;

public static class Extensions
{
	/// <summary>Get the size of a file in bytes, KB, or MB</summary>
	/// <param name="file">The file to get the size of</param>
	/// <returns>The size of the file</returns>
	public static string GetPrettySize(this FileInfo file)
	{
		var size_in_bytes = file.Length;
		return size_in_bytes switch
		{
			< 1024 => $"{size_in_bytes} bytes",
			< 1024 * 1024 => $"{size_in_bytes / 1024.0:N2} KB",
			_ => $"{size_in_bytes / 1024.0 / 1024.0:N2} MB",
		};
	}

	/// <summary>Optionally add a json file</summary>
	public static IConfigurationBuilder AddJsonFileIf(this IConfigurationBuilder builder, bool condition, string path, bool optional)
	{
		if (condition) builder.AddJsonFile(path, optional);
		return builder;
	}
}
