// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class TextureImage {
	[GrannyMember("MIPLevels")] public List<TextureImageMip> Mips { get; set; } = [];
}
