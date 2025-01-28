// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class Model : GrannyExtendable {
	public string Name { get; set; } = "";
	public Skeleton? Skeleton { get; set; }
	[GrannyMember("InitialPlacement")] public Transform Transform { get; set; } = Transform.Identity;
	public List<MeshBinding> MeshBindings { get; set; } = [];

	public override string ToString() => "Model { " +
	                                     $"Name = {Name}, " +
	                                     $"Skeleton = {Skeleton?.Name ?? "None"}, " +
	                                     $"Meshes = {MeshBindings.Count}, " +
	                                     $"Transform = {Transform} " +
	                                     "}";
}
