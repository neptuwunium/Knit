// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class Mesh : GrannyExtendable {
	public string Name { get; set; } = "";
	public VertexData? PrimaryVertexData { get; set; }
	public TriTopology? PrimaryTopology { get; set; }
	public List<MorphTarget> MorphTargets { get; set; } = [];
	public List<Material> MaterialBindings { get; set; } = [];
	public List<MeshBone> BoneBindings { get; set; } = [];
}
