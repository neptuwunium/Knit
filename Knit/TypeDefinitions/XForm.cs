// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;
using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct XForm {
	public XFormFlags Flags { get; set; }
	public Vector3 Position { get; set; }
	public Quaternion Rotation { get; set; }
	public Matrix3x3 ScaleMatrix { get; set; }
}
