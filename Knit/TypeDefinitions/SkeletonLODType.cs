// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

[Flags]
public enum SkeletonLODType : uint {
	None = 0,
	Estimated = 1 << 0,
	Measured = 1 << 1,
}
