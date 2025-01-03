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
		try {
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
			Console.WriteLine($"\t\tIs Big-Endian: {granny.Header.IsBigEndian}");

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
			var sectors = granny.Sectors.Span;
			var unsupportedCompressions = new HashSet<Granny2CompressionType>();
			for (var i = 0; i < sectors.Length; ++i) {
				var sector = sectors[i];
				Console.Write($"\t\t{(Granny2SectorId) i:G}:");
				if (sector.IsEmpty) {
					Console.WriteLine(" empty");
					continue;
				}

				Console.WriteLine();
				Console.WriteLine($"\t\t\tCompression: {sector.Compression}");
				Console.WriteLine($"\t\t\tCompression Bits: {sector.CompressionBits1:b32}{sector.CompressionBits2:b32}");
				Console.WriteLine($"\t\t\tAlignment: {sector.Alignment}");
				Console.WriteLine($"\t\t\tData Pointer: {sector.Data}");
				Console.WriteLine($"\t\t\tFixup Pointer: {sector.Fixup}");
				Console.WriteLine($"\t\t\tMarshalled Fixup Pointer: {sector.MarshalledFixup}");
				if (!sector.IsSupported) {
					unsupportedCompressions.Add(sector.Compression);
				}
			}

			if (unsupportedCompressions.Count > 0) {
				Console.Error.WriteLine($"File {file} has an unsupported compression! {string.Join(", ", unsupportedCompressions)}");
			}

			Console.WriteLine();
		} catch (Exception e) {
			Console.WriteLine($"\tFailed to read file: {e.Message}");
		}
	}
}
