// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

[Flags]
public enum AnimationFlags : uint {
	DefaultLoopCountValid = 1 << 0,
	DefaultToLoopClamping = 1 << 1,
}
