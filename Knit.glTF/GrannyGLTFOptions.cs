// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.glTF;

public record GrannyGLTFOptions {
	public bool Rescale { get; set; }
	public bool OneBoned { get; set; }
	public bool GenerateNormals { get; set; }
}
