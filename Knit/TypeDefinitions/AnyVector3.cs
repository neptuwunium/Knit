// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public readonly record struct AnyVector3<T>(T X, T Y, T Z) where T : INumberBase<T> {
	public Vector3 ToVec() => new(float.CreateChecked(X), float.CreateChecked(Y), float.CreateChecked(Z));

	public void Deconstruct(out T x, out T y, out T z) {
		x = X;
		y = Y;
		z = Z;
	}
}
