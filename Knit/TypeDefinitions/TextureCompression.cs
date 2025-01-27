// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public enum TextureCompression {
	UserDefined = 0,
	Raw = 1,
	S3TC = 2,
	Bink = 3, // similarly has 0 and 1 modes, checked with a flag.
	YCoCg = 4,
}
