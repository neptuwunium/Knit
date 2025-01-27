// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class SkeletonBone : GrannyExtendable {
	public string Name { get; set; } = "";
	public int ParentIndex { get; set; }
	public XForm Transform { get; set; }
	[GrannyMember("InverseWorldTransform")] public Matrix4x4 InverseTransform { get; set; }
	public float LODError { get; set; }
}
