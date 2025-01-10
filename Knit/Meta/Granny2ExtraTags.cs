// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Knit.Meta;

[InlineArray(4)]
public struct Granny2ExtraTags : IEquatable<Granny2ExtraTags>, IEquatable<Span<uint>>, IEquatable<ReadOnlySpan<uint>>, IEquatable<uint>, IEquatable<Span<Granny2Tag>>, IEquatable<ReadOnlySpan<Granny2Tag>>, IEquatable<Granny2Tag> {
	public Granny2Tag Value;

	public override int GetHashCode() {
		var hashCode = new HashCode();
		hashCode.Add(this[0]);
		hashCode.Add(this[1]);
		hashCode.Add(this[2]);
		hashCode.Add(this[3]);
		return hashCode.ToHashCode();
	}

	public override string ToString() => $"[{this[0]}, {this[1]}, {this[2]}, {this[3]}]";

	public bool Equals(Granny2ExtraTags other) => Equals((ReadOnlySpan<Granny2Tag>) other);
	public bool Equals(Span<uint> other) => other.SequenceEqual(MemoryMarshal.Cast<Granny2Tag, uint>(this));
	public bool Equals(ReadOnlySpan<uint> other) => other.SequenceEqual(MemoryMarshal.Cast<Granny2Tag, uint>(this));
	public bool Equals(uint other) => ((ReadOnlySpan<Granny2Tag>) this).Contains(other);
	public bool Equals(Span<Granny2Tag> other) => other.SequenceEqual(this);
	public bool Equals(ReadOnlySpan<Granny2Tag> other) => other.SequenceEqual(this);
	public bool Equals(Granny2Tag other) => ((ReadOnlySpan<Granny2Tag>) this).Contains(other);
	public override bool Equals(object? obj) => obj is Granny2ExtraTags other && Equals(other);
	public static bool operator ==(Granny2ExtraTags left, Granny2ExtraTags right) => left.Equals(right);
	public static bool operator !=(Granny2ExtraTags left, Granny2ExtraTags right) => !(left == right);
	public static bool operator ==(Granny2ExtraTags left, Span<uint> right) => left.Equals(right);
	public static bool operator !=(Granny2ExtraTags left, Span<uint> right) => !(left == right);
	public static bool operator ==(Granny2ExtraTags left, ReadOnlySpan<uint> right) => left.Equals(right);
	public static bool operator !=(Granny2ExtraTags left, ReadOnlySpan<uint> right) => !(left == right);
	public static bool operator ==(Granny2ExtraTags left, uint right) => left.Equals(right);
	public static bool operator !=(Granny2ExtraTags left, uint right) => !(left == right);
	public static bool operator ==(Granny2ExtraTags left, Span<Granny2Tag> right) => left.Equals(right);
	public static bool operator !=(Granny2ExtraTags left, Span<Granny2Tag> right) => !(left == right);
	public static bool operator ==(Granny2ExtraTags left, ReadOnlySpan<Granny2Tag> right) => left.Equals(right);
	public static bool operator !=(Granny2ExtraTags left, ReadOnlySpan<Granny2Tag> right) => !(left == right);
	public static bool operator ==(Granny2ExtraTags left, Granny2Tag right) => left.Equals(right);
	public static bool operator !=(Granny2ExtraTags left, Granny2Tag right) => !(left == right);
}
