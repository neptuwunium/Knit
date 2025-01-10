// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.Meta;

public enum Granny2TextureCompression {
	UserDefined = 0,
	Raw = 1,
	S3TC = 2,
	Bink = 3, // similarly has 0 and 1 modes, checked with a flag.
	YCoCg = 4,
}
