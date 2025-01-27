// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

[Flags]
public enum BinkFormatFlags : uint {
	HasAlpha = 1 << 0,
	sRGB = 1 << 1,
	Bink1 = 1 << 2,
}
