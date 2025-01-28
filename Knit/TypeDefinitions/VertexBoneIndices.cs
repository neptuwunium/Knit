// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

[InlineArray(256)]
public struct VertexBoneIndices : IEquatable<VertexBoneIndices> {
	public short Value;

	public bool Equals(VertexBoneIndices other) => ((Span<short>) this).SequenceEqual(other);
	public override bool Equals(object? obj) => obj is VertexBoneWeights other && Equals(other);

	public override int GetHashCode() {
		var hashCode = new HashCode();
		hashCode.AddBytes(MemoryMarshal.AsBytes((Span<short>) this));
		return hashCode.ToHashCode();
	}

	public static bool operator ==(VertexBoneIndices left, VertexBoneIndices right) => left.Equals(right);
	public static bool operator !=(VertexBoneIndices left, VertexBoneIndices right) => !(left == right);
}
