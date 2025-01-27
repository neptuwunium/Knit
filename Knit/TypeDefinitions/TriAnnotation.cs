// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class TriAnnotation {
	public string Name { get; set; } = "";

	// todo: disabled until we implement untyped unmarshalling.
	// public Dictionary<string, object> TriAnnotations { get; set; } = [];
	public bool IndicesMapFromTriToAnnotation { get; set; }
	public int[] TriAnnotationIndices { get; set; } = [];
}
