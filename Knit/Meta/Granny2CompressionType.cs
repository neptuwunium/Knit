// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.Meta;

public enum Granny2CompressionType {
	None = 0,

	// granny2_x64.dll has different methods for Oodle0 and Oodle1.
	// Oodle0 is functionally the same as Oodle1, but reverses the entire data buffer if the endianness mismatches.
	// This is not a modern Oodle super-compressor like Kraken or Mermaid, but a LZ77 + arithmetic coding algorithm.
	Oodle0 = 1,
	Oodle1 = 2,

	// BitKnit1 is identical to BitKnit2, BitKnit2 has an encode bugfix. Both should be decodable with the same decoder.
	// https://fgiesen.wordpress.com/2023/05/06/a-very-brief-bitknit-retrospective/#comment-26615
	BitKnit1 = 3,
	BitKnit2 = 4,
}
