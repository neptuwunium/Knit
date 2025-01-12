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
		Console.WriteLine($"\t\tMagic: {granny.Header.Magic}");
		Console.WriteLine($"\t\tVersion: {granny.Header.Version}");
		Console.WriteLine($"\t\tSize: {granny.Header.HeaderSize}");
		Console.WriteLine($"\t\tIs Supported: {granny.Header.IsSupported}");
		Console.WriteLine($"\t\tIs 64-bit: {granny.Header.Is64Bit}");
		Console.WriteLine($"\t\tIs Version 6: {granny.Header.IsV6}");
		Console.WriteLine($"\t\tIs Version 7: {granny.Header.IsV7}");
		Console.WriteLine($"\t\tIs Little-Endian: {granny.Header.IsLittleEndian}");

		Console.WriteLine("\tFileInfo:");
		Console.WriteLine($"\t\tVersion: {granny.FileInfo.Version}");
		Console.WriteLine($"\t\tSize: {granny.FileInfo.FileSize}");
		Console.WriteLine($"\t\tIs Supported: {granny.FileInfo.IsSupported}");
		Console.WriteLine($"\t\tChecksum: {granny.FileInfo.Checksum:x8}");
		Console.WriteLine($"\t\tString Checksum: {granny.FileInfo.StringChecksum:x8}");
		Console.WriteLine($"\t\tRoot Definition: {granny.FileInfo.RootTypeDefinition}");
		Console.WriteLine($"\t\tRoot Object: {granny.FileInfo.Root}");
		Console.WriteLine($"\t\tTag: {granny.FileInfo.Tag}");
		Console.WriteLine($"\t\tExtra Tags: {granny.FileInfo.ExtraTags}");

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
			Console.WriteLine($"\t\t\tCompression: {section.Compression}");
			Console.WriteLine($"\t\t\tCompression Bits 1: {section.CompressionBits1:b32} ({section.CompressionBits1})");
			Console.WriteLine($"\t\t\tCompression Bits 2: {section.CompressionBits2:b32} ({section.CompressionBits2})");
			Console.WriteLine($"\t\t\tAlignment: {section.Alignment}");
			Console.WriteLine($"\t\t\tSize: {section.UncompressedSize}");
			Console.WriteLine($"\t\t\tData Pointer: {section.Data}");
			Console.WriteLine($"\t\t\tFixup Pointer: {section.Fixup}");
			Console.WriteLine($"\t\t\tMarshalled Fixup Pointer: {section.MarshalledFixup}");
			if (!section.IsSupported) {
				unsupportedCompressions.Add(section.Compression);
			}
		}

		File.WriteAllBytes("test.bin", granny.FileData.Memory.Span);

		if (unsupportedCompressions.Count > 0) {
			Console.Error.WriteLine($"File {file} has an unsupported compression! {string.Join(", ", unsupportedCompressions)}");
		} else {
			Console.WriteLine("\tRoot Type:");
			var rootPtr = granny.Resolve(granny.FileInfo.RootTypeDefinition);
			IterateType(granny, rootPtr, "\t\t", []);
		}

		Console.WriteLine();
	}

	private static void IterateType(Granny2File granny, SpanPointer ptr, string indent, Dictionary<int, int> visited) {
		granny.EnumerateTypeMembers(ptr, typeInfo => {
			Console.Write($"{indent}{typeInfo.Type:G} {typeInfo.GetName(granny)}");
			if (typeInfo.ChildrenOffset != 0) {
				if (visited.TryGetValue(typeInfo.ChildrenOffset, out var typeIndex)) {
					Console.WriteLine($" &{typeIndex}");
				} else {
					typeIndex = visited[typeInfo.ChildrenOffset] = visited.Count + 1;
					Console.WriteLine($" &{typeIndex}");
					IterateType(granny, typeInfo.GetTypeDefinition(granny), indent + "\t", visited);
				}
			} else {
				Console.WriteLine();
			}

			return true;
		});
	}
}
