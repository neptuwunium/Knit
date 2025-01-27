// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

[Flags]
public enum XFormFlags : uint {
	HasPosition = 1 << 0,
	HasOrientation = 1 << 1,
	HasScaleMatrix = 1 << 2,
}
