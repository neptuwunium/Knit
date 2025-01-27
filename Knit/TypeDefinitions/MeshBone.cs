// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class MeshBone {
	[GrannyMember("BoneName")] public string Name { get; set; } = "";
	public Vector3 OBBMin { get; set; }
	public Vector3 OBBMax { get; set; }
	public int[] TriangleIndices { get; set; } = [];
}
