// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

[Flags]
public enum AccumulationFlags : uint {
	Extracted = 1 << 0,
	Sorted = 1 << 1,
	VDA = 1 << 2,
}
