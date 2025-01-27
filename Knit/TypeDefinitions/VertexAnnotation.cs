// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class VertexAnnotation {
	public string Name { get; set; } = "";

	// todo: disabled until we implement untyped unmarshalling.
	// public Dictionary<string, object> VertexAnnotations { get; set; } = [];
	public bool IndicesMapFromVertexToAnnotation { get; set; }
	public int[] VertexAnnotationIndices { get; set; } = [];
}
