#! "net9.0"
#load "UserVars.csx"
#load "Tools.csx"
#nullable enable

// Generate a header file that includes all headers in the library
// Use: dotnet-script HarvestFiles.csx <output_header_path>

using System;
using System.Text.RegularExpressions;
using Console = System.Console;

try
{
	List<string> args =
	 	["E:\\Rylogic\\Code\\projects\\tests\\unittests\\src\\unittests.h"]
		//Environment.GetCommandLineArgs().Skip(2).ToList()
	;
	var outfile = args.Count > 0 ? args[0] : throw new Exception("Output filepath not provided");

	var srcdirs = (List<string>)[
		Tools.Path([UserVars.Root, "include"]),
	];
	var exclude = (List<Regex>)[
		new Regex(@"pr/app/"),
		new Regex(@"pr/geometry/mesh_tools\.h"),
		new Regex(@"pr/gui/"),
		new Regex(@"pr/image/"),
		new Regex(@"pr/ldraw/ldr_.*\.h"),
		new Regex(@"pr/macros/on_exit\.h"),
		new Regex(@"pr/maths/pr_to_ode\.h"),
		new Regex(@"pr/physics/"),
		new Regex(@"pr/sound/"),
		new Regex(@"pr/storage/xfile/"),
		new Regex(@"pr/storage/zip/"),
		new Regex(@"pr/script_old/"),
		new Regex(@"pr/terrain/"),
		new Regex(@"pr/collision/todo/"),
		new Regex(@"pr/collision/builder/"),
		new Regex(@"pr/view3d/"),
	];

	List<string> includes = [];
	foreach (var sd in srcdirs)
	{
		foreach (var file in Directory.GetFiles(sd, "*.h", SearchOption.AllDirectories))
		{
			var filepath = file.Replace('\\', '/');
			if (exclude.Any(x => x.IsMatch(filepath)))
				continue;

			var relpath = Path.GetRelativePath(sd, filepath).Replace('\\', '/');
			includes.Add($"#include \"{relpath}\"");
		}
	}
	includes.Sort();

	// Generate a file that includes all headers
	var output = new StringBuilder(16384);
	output.Append(
		"// This is a generated file\n" +
		"#pragma once\n" +
		"#include <sdkddkver.h>\n" +
		"#include <winsock2.h> // Include winsock2.h before windows.h\n" +
		"\n" +
		"// Headers to unit test\n"
		);
	output.Append(string.Join("\n", includes));

	// Read the existing file, and replace it if different
	var existing = Path.Exists(outfile) ? File.ReadAllText(outfile) : "";
	if (existing.CompareTo(output.ToString()) != 0)
		File.WriteAllText(outfile, output.ToString());
}
catch (Exception ex)
{
	Console.Error.WriteLine(ex.Message);
}
