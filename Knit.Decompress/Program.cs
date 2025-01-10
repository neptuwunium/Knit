// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.Decompress;

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
		Console.WriteLine($"{file}");

		using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var granny = new Granny2File(stream);
		using var decompressStream = new FileStream(Path.ChangeExtension(file, ".decompressed.gr2"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
		granny.Save(decompressStream);
	}
}
