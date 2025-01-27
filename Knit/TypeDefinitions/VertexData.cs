// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class VertexData {
	public Vertex[] Vertices { get; set; } = [];
	[GrannyMember("VertexComponentNames")] public List<string> ComponentNames { get; set; } = [];
	[GrannyMember("VertexAnnotationSets")] public List<VertexAnnotation> Annotations { get; set; } = [];
}
