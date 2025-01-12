// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;

namespace Knit.Meta;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x10)]
public record struct Granny2MarshalledFixup {
	public int Count { get; set; }
	public int ObjectOffset { get; set; }
	public Granny2Reference TypeLocation { get; set; }
}
