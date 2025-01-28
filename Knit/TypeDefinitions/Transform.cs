// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;
using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct Transform {
	public static Transform Identity { get; } = new() {
		Flags = 0,
		Position = Vector3.Zero,
		Rotation = Quaternion.Zero,
		ShearMatrix = Matrix3x3.Identity,
	};

	public XFormFlags Flags { get; set; }
	public Vector3 Position { get; set; }
	public Quaternion Rotation { get; set; }
	public Matrix3x3 ShearMatrix { get; set; }

	public Vector3 Scale => new(ShearMatrix.M11, ShearMatrix.M22, ShearMatrix.M33);
}
