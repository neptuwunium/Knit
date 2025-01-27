// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class MeshBone {
	[GrannyMember("BoneName")] public string Name { get; set; } = "";
	public Vector3 OOBMin { get; set; }
	public Vector3 OOBMax { get; set; }
	public int[] TriangleIndices { get; set; } = [];
}
