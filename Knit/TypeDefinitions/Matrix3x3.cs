// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;
using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Matrix3x3(float M11, float M12, float M13, float M21, float M22, float M23, float M31, float M32, float M33) {
	public static Matrix3x3 Identity { get; } = new() {
		M11 = 1,
		M22 = 1,
		M33 = 1,
	};

	public override string ToString() => $"<<{M11}, {M12}, {M13}>, <{M21}, {M22}, {M23}>, <{M31}, {M32}, {M33}>>";

	public Vector3 ExtractScale() => new(M11, M22, M33);

	public static Matrix3x3 operator +(Matrix3x3 left, Matrix3x3 right) =>
		new(left.M11 + right.M11, left.M12 + right.M12, left.M13 + right.M13,
		    left.M21 + right.M21, left.M22 + right.M22, left.M23 + right.M23,
		    left.M31 + right.M31, left.M32 + right.M32, left.M33 + right.M33);

	public static Matrix3x3 operator *(Matrix3x3 left, Matrix3x3 right) =>
		new(left.M11 * right.M11, left.M12 * right.M12, left.M13 * right.M13,
		    left.M21 * right.M21, left.M22 * right.M22, left.M23 * right.M23,
		    left.M31 * right.M31, left.M32 * right.M32, left.M33 * right.M33);
}
