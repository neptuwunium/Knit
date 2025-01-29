// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

// realistically means the number of points per "knot".
public enum CurveDimension : short {
	Position = 3,
	Rotation = 4,
	ScaleShear = 9,
}
