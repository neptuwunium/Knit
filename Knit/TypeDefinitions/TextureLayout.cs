// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct TextureLayout {
	public int BytesPerPixel { get; set; }
	public int ShiftForComponent { get; set; }
	public int BitsForComponent { get; set; }
}
