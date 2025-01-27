// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class MaterialMap {
	public string Usage { get; set; } = "";
	[GrannyMember("Map")] public Material? Material { get; set; }
}
