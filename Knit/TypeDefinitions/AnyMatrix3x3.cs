// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public readonly record struct AnyMatrix3x3<T>(T M11, T M12, T M13, T M21, T M22, T M23, T M31, T M32, T M33) where T : INumberBase<T> {
	public Matrix3x3 ToMatrix() => new(float.CreateChecked(M11), float.CreateChecked(M12), float.CreateChecked(M13),
	                                   float.CreateChecked(M21), float.CreateChecked(M22), float.CreateChecked(M23),
	                                   float.CreateChecked(M31), float.CreateChecked(M32), float.CreateChecked(M33));
}
