// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct TriMaterialGroup {
	public int MaterialIndex { get; set; }
	public int Start { get; set; }
	public int Count { get; set; }
}
