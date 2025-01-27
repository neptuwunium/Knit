// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class Material : GrannyExtendable {
	public string Name { get; set; } = "";
	public List<MaterialMap> Maps { get; set; } = [];
	public Texture? Texture { get; set; }
}
