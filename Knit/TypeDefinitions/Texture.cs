// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class Texture : GrannyExtendable {
	[GrannyMember("FromFileName")] public string Name { get; set; } = "";
	public TextureType TextureType { get; set; }
	public int Width { get; set; }
	public int Height { get; set; }
	public TextureCompression Encoding { get; set; }
	public int SubFormat { get; set; }
	public TextureLayout Layout { get; set; }
}
