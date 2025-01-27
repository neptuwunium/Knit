// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit;

internal static class Checksum {
	public static uint Hash(ReadOnlySpan<byte> bytes, uint hash = 0xffffffff) {
		foreach (var value in bytes) {
			hash ^= value;

			for (var j = 7; j >= 0; j--) {
				hash = (hash >> 1) ^ (0xEDB88320 & (uint) -(hash & 1));
			}
		}

		return hash;
	}
}
