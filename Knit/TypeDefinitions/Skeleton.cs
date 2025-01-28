// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class Skeleton : GrannyExtendable {
	public string Name { get; set; } = "";
	public List<SkeletonBone> Bones { get; set; } = [];
	public SkeletonLODType LODType { get; set; }

	public override string ToString() => "Skeleton { " +
	                                     $"Name = {Name}, " +
	                                     $"Bones = {Bones.Count}, " +
	                                     $"LOD Type = {LODType} " +
	                                     "}";
}
