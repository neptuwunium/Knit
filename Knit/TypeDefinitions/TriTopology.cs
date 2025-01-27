// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class TriTopology {
	public List<TriMaterialGroup> Groups { get; set; } = [];
	[GrannyMember("Indices")] public int[] Indices32 { get; set; } = [];
	public short[] Indices16 { get; set; } = [];
	public int[] VertexToVertexMap { get; set; } = [];
	public int[] VertexToTriangleMap { get; set; } = [];
	public int[] SideToNeighborMap { get; set; } = [];
	public int[] PolygonIndexStarts { get; set; } = [];
	public int[] PolygonIndices { get; set; } = [];
	public int[] BonesForTriangle { get; set; } = [];
	public int[] TriangleToBoneIndices { get; set; } = [];
	[GrannyMember("TriAnnotationSets")] public List<VertexAnnotation> Annotations { get; set; } = [];
}
