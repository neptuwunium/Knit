// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public readonly record struct AnyVector4<T>(T X, T Y, T Z, T W) where T : INumberBase<T> {
	public Vector4 ToVec() => new(float.CreateChecked(X), float.CreateChecked(Y), float.CreateChecked(Z), float.CreateChecked(W));

	public void Deconstruct(out T x, out T y, out T z, out T w) {
		x = X;
		y = Y;
		z = Z;
		w = Z;
	}
}
