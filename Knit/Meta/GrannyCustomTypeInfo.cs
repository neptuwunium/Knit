// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.CompilerServices;

namespace Knit.Meta;

[InlineArray(3)]
public struct GrannyCustomTypeInfo : IEquatable<GrannyCustomTypeInfo> {
	public int Value;

	public bool Equals(GrannyCustomTypeInfo other) => ((Span<int>) this).SequenceEqual(other);
	public override bool Equals(object? obj) => obj is GrannyCustomTypeInfo other && Equals(other);

	public override int GetHashCode() {
		var hashCode = new HashCode();
		hashCode.AddBytes(((Span<int>) this).AsBytes());
		return hashCode.ToHashCode();
	}

	public static bool operator ==(GrannyCustomTypeInfo left, GrannyCustomTypeInfo right) => left.Equals(right);
	public static bool operator !=(GrannyCustomTypeInfo left, GrannyCustomTypeInfo right) => !(left == right);
}
