// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public record struct CurveDataHeader {
	public CurveFormat Format { get; set; }
	public byte Degree { get; set; }
}
