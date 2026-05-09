// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using Knit.glTF;
using Pluto.CommandLine;
using Pluto.IO.FileSystem;

namespace Knit.Convert;

internal record ProgramFlags : CommandLineFlags {
	[Flag("one-bone", Help = "Treat meshes that have no weight information has having one bone")]
	public bool OneBone { get; set; }

	[Flag("generate-normals", Help = "Generate smooth normals when no normal data is present")]
	public bool GenerateNormals { get; set; }

	[Flag("rescale", Help = "Rescale so that 1 unit is 1 meter")]
	public bool Rescale { get; set; }

	[Flag("paths", Help = "Paths of directories or files to process", Positional = 0)]
	public HashSet<string> Paths { get; set; } = [];
}

internal static class Program {
	private static ProgramFlags Flags { get; set; } = null!;
	private static GrannyGLTFOptions ExportOptions { get; set; } = null!;

	private static void Main() {
		Flags = CommandLineFlagsParser.ParseFlags<ProgramFlags>();
		ExportOptions = new GrannyGLTFOptions {
			OneBoned = Flags.OneBone,
			GenerateNormals = Flags.GenerateNormals,
			Rescale = Flags.Rescale,
		};

		foreach (var arg in new FileEnumerator(Flags.Paths, new EnumerationOptions { RecurseSubdirectories = true }, "*.gr2")) {
			ProcessFile(arg);
		}
	}

	private static void ProcessFile(string file) {
		Console.WriteLine(file);

	#if !DEBUG
		try {
		#endif
			using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var granny = new Granny2File(stream);
			using var gltf = new GrannyGLTF(granny.LoadRoot() ?? throw new InvalidOperationException(), ExportOptions);
			gltf.Write(Path.ChangeExtension(file, ".gltf"));
		#if !DEBUG
		} catch (Exception e) {
			Console.Error.WriteLine($"Failed to convert {file}: {e}");
		}
	#endif
	}
}
