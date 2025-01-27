// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class GrannyFileRoot : GrannyExtendable {
	public ArtToolInfo ArtToolInfo { get; set; } = new();
	public ExporterInfo ExporterInfo { get; set; } = new();
	[GrannyMember("FromFileName")] public string Name { get; set; } = "";
	public List<Texture> Textures { get; set; } = [];
	public List<Material> Materials { get; set; } = [];
	public List<Skeleton> Skeletons { get; set; } = [];
	[GrannyMember("VertexDatas")] public List<VertexData> Vertices { get; set; } = [];
	[GrannyMember("TriTopologies")] public List<TriTopology> SubMeshes { get; set; } = [];
	public List<Mesh> Meshes { get; set; } = [];
	public List<Model> Models { get; set; } = [];
	public List<TrackGroup> TrackGroups { get; set; } = [];
	public List<Animation> Animations { get; set; } = [];
}
