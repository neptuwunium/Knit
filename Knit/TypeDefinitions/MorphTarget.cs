// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class MorphTarget {
	[GrannyMember("ScalarName")] public string Name { get; set; } = "";
	public VertexData? VertexData { get; set; }
	public bool DataIsDeltas { get; set; }
}
