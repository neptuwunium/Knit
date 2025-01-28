// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using Knit.glTF;

namespace Knit.Convert;

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
			using var gltf = new GrannyGLTF(granny.LoadRoot() ?? throw new InvalidOperationException());
			gltf.Write(Path.ChangeExtension(file, ".gltf"));
		} catch(Exception e) {
			Console.Error.WriteLine($"Failed to convert {file}: {e}");
		}
	}
}
