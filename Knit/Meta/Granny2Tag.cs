// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;

namespace Knit.Meta;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
public record struct Granny2Tag {
	public const uint TagMask = 0x7FFFFFFF;
	public const uint IsRADMask = 0x80000000;
	internal uint RawTag { get; set; }

	public uint Tag => RawTag & TagMask;
	public bool IsRAD => (RawTag & IsRADMask) != 0;

	public static implicit operator uint(Granny2Tag tag) => tag.RawTag;

	public static implicit operator Granny2Tag(uint tag) => new() {
		RawTag = tag,
	};

	public override string ToString() => $"{(IsRAD ? "RAD:" : "TAG:")}{Tag}";
}
