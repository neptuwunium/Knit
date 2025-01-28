// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using Knit.Meta;

namespace Knit.Diagnostics;

internal static class Program {
	private static void Main(string[] args) {
		foreach (var arg in args) {
			var fileInfo = new FileInfo(arg);

			if ((fileInfo.Attributes & FileAttributes.Directory) != 0) {
				var dirInfo = new DirectoryInfo(arg);
				if (dirInfo.Exists) {
					foreach (var file in dirInfo.EnumerateFiles("*.gr2", SearchOption.AllDirectories)) {
						ProcessFile(file.FullName);
					}
				}
			} else if (fileInfo.Exists) {
				ProcessFile(fileInfo.FullName);
			}
		}
	}

	private static void ProcessFile(string file) {
		Console.WriteLine($"{file}:");

		using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var granny = new Granny2File(stream);

		Console.WriteLine("\tHeader:");
		Console.WriteLine($"\t\tMagic = {granny.Header.Magic}");
		Console.WriteLine($"\t\tVersion = {granny.Header.Version}");
		Console.WriteLine($"\t\tSize = {granny.Header.HeaderSize}");
		Console.WriteLine($"\t\tIs Supported = {granny.Header.IsSupported}");
		Console.WriteLine($"\t\tIs 64-bit = {granny.Header.Is64Bit}");
		Console.WriteLine($"\t\tIs Version 6 = {granny.Header.IsV6}");
		Console.WriteLine($"\t\tIs Version 7 = {granny.Header.IsV7}");
		Console.WriteLine($"\t\tIs Little-Endian = {granny.Header.IsLittleEndian}");

		Console.WriteLine("\tFileInfo:");
		Console.WriteLine($"\t\tVersion = {granny.FileInfo.Version}");
		Console.WriteLine($"\t\tSize = {granny.FileInfo.FileSize}");
		Console.WriteLine($"\t\tIs Supported = {granny.FileInfo.IsSupported}");
		Console.WriteLine($"\t\tChecksum = {granny.FileInfo.Checksum:x8}");
		Console.WriteLine($"\t\tString Checksum = {granny.FileInfo.StringChecksum:x8}");
		Console.WriteLine($"\t\tRoot Definition = {granny.FileInfo.RootTypeDefinition}");
		Console.WriteLine($"\t\tRoot Object = {granny.FileInfo.Root}");
		Console.WriteLine($"\t\tTag = {granny.FileInfo.Tag}");
		Console.WriteLine($"\t\tExtra Tags = {granny.FileInfo.ExtraTags}");

		Console.WriteLine("\tSections:");
		var sections = granny.Sections.Span;
		var unsupportedCompressions = new HashSet<Granny2CompressionType>();
		for (var i = 0; i < sections.Length; ++i) {
			var section = sections[i];
			Console.Write($"\t\t{(Granny2SectionId) i:G}:");
			if (section.IsEmpty) {
				Console.WriteLine(" empty");
				continue;
			}

			Console.WriteLine();
			Console.WriteLine($"\t\t\tCompression = {section.Compression}");
			Console.WriteLine($"\t\t\tCompression Bits 1 = {section.CompressionBits1:b32} ({section.CompressionBits1})");
			Console.WriteLine($"\t\t\tCompression Bits 2 = {section.CompressionBits2:b32} ({section.CompressionBits2})");
			Console.WriteLine($"\t\t\tAlignment = {section.Alignment}");
			Console.WriteLine($"\t\t\tSize = {section.UncompressedSize}");
			Console.WriteLine($"\t\t\tData Pointer = {section.Data}");
			Console.WriteLine($"\t\t\tFixup Pointer = {section.Fixup}");
			Console.WriteLine($"\t\t\tMarshalled Fixup Pointer = {section.MarshalledFixup}");
			if (!section.IsSupported) {
				unsupportedCompressions.Add(section.Compression);
			}
		}

		if (unsupportedCompressions.Count > 0) {
			Console.Error.WriteLine($"File {file} has an unsupported compression! {string.Join(", ", unsupportedCompressions)}");
		} else {
			Console.WriteLine("\tRoot Type:");
			var rootPtr = granny.Resolve(granny.FileInfo.RootTypeDefinition);
			granny.DebugType(rootPtr, "\t\t", []);
		}

		try {
			var root = granny.LoadRoot() ?? throw new NullReferenceException();

			Console.WriteLine("\tRoot Data:");
			Console.WriteLine($"\t\tName = {root.Name}");
			Console.WriteLine($"\t\t{root.ArtToolInfo}");
			Console.WriteLine($"\t\t{root.ExporterInfo}");
			Console.WriteLine($"\t\tTextures = {root.Textures.Count}");
			foreach (var texture in root.Textures) {
				Console.WriteLine($"\t\t\t{texture}");
			}

			Console.WriteLine($"\t\tMaterials = {root.Materials.Count}");
			foreach (var material in root.Materials) {
				Console.WriteLine($"\t\t\t{material}");
			}

			Console.WriteLine($"\t\tSkeletons = {root.Skeletons.Count}");
			foreach (var skeleton in root.Skeletons) {
				Console.WriteLine($"\t\t\t{skeleton}");
			}

			Console.WriteLine($"\t\tVertices = {root.Vertices.Count}");
			foreach (var vertexData in root.Vertices) {
				Console.WriteLine($"\t\t\t{vertexData}");
			}

			Console.WriteLine($"\t\tSub Meshes = {root.SubMeshes.Count}");
			foreach (var topology in root.SubMeshes) {
				Console.WriteLine($"\t\t\t{topology}");
			}

			Console.WriteLine($"\t\tMeshes = {root.Meshes.Count}");
			foreach (var mesh in root.Meshes) {
				Console.WriteLine($"\t\t\t{mesh}");
			}

			Console.WriteLine($"\t\tModels = {root.Models.Count}");
			foreach (var model in root.Models) {
				Console.WriteLine($"\t\t\t{model}");
			}

			Console.WriteLine($"\t\tTrack Groups = {root.TrackGroups.Count}");
			foreach (var track in root.TrackGroups) {
				Console.WriteLine($"\t\t\t{track}");
			}

			Console.WriteLine($"\t\tAnimations = {root.Animations.Count}");
			foreach (var animation in root.Animations) {
				Console.WriteLine($"\t\t\t{animation}");
			}
		} catch {
			Console.Error.WriteLine($"File {file} could not parse root data!");
		}

		Console.WriteLine();
	}
}
