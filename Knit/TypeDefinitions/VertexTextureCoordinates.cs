// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

[InlineArray(8)]
public struct VertexTextureCoordinates : IEquatable<VertexTextureCoordinates> {
	public Vector3 Value;

	public bool Equals(VertexTextureCoordinates other) => ((Span<Vector3>) this).SequenceEqual(other);
	public override bool Equals(object? obj) => obj is VertexBoneWeights other && Equals(other);

	public override int GetHashCode() {
		var hashCode = new HashCode();
		hashCode.AddBytes(MemoryMarshal.AsBytes((Span<Vector3>) this));
		return hashCode.ToHashCode();
	}

	public static bool operator ==(VertexTextureCoordinates left, VertexTextureCoordinates right) => left.Equals(right);
	public static bool operator !=(VertexTextureCoordinates left, VertexTextureCoordinates right) => !(left == right);
}
