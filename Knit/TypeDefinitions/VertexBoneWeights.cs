// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

[InlineArray(256)]
public struct VertexBoneWeights : IEquatable<VertexBoneWeights> {
	public float Value;

	public bool Equals(VertexBoneWeights other) => ((Span<float>) this).SequenceEqual(other);
	public override bool Equals(object? obj) => obj is VertexBoneWeights other && Equals(other);

	public override int GetHashCode() {
		var hashCode = new HashCode();
		hashCode.AddBytes(MemoryMarshal.AsBytes((Span<float>) this));
		return hashCode.ToHashCode();
	}

	public static bool operator ==(VertexBoneWeights left, VertexBoneWeights right) => left.Equals(right);
	public static bool operator !=(VertexBoneWeights left, VertexBoneWeights right) => !(left == right);
}

[InlineArray(4)]
public struct ComponentInfo : IEquatable<ComponentInfo> {
	public int Value;

	public bool Equals(ComponentInfo other) => ((Span<int>) this).SequenceEqual(other);
	public override bool Equals(object? obj) => obj is VertexBoneWeights other && Equals(other);

	public override int GetHashCode() {
		var hashCode = new HashCode();
		hashCode.AddBytes(MemoryMarshal.AsBytes((Span<int>) this));
		return hashCode.ToHashCode();
	}

	public static bool operator ==(ComponentInfo left, ComponentInfo right) => left.Equals(right);
	public static bool operator !=(ComponentInfo left, ComponentInfo right) => !(left == right);
}
