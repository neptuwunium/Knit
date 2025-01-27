// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct Matrix3x3 {
	public float M11 { get; set; }
	public float M12 { get; set; }
	public float M13 { get; set; }
	public float M21 { get; set; }
	public float M22 { get; set; }
	public float M23 { get; set; }
	public float M31 { get; set; }
	public float M32 { get; set; }
	public float M33 { get; set; }
}
