// SPDX-FileCopyrightText: 2017-2025 Norbyte (LSLib, MIT)
//
// SPDX-License-Identifier: MIT

namespace Knit.TypeDefinitions;

internal class CurveD4Tables {
	internal static readonly float[] S = [
		1.4142135f, 0.70710677f, 0.35355338f, 0.35355338f,
		0.35355338f, 0.17677669f, 0.17677669f, 0.17677669f,
		-1.4142135f, -0.70710677f, -0.35355338f, -0.35355338f,
		-0.35355338f, -0.17677669f, -0.17677669f, -0.17677669f,
	];

	internal static readonly float[] O = [
		-0.70710677f, -0.35355338f, -0.53033006f, -0.17677669f,
		0.17677669f, -0.17677669f, -0.088388346f, 0.0f,
		0.70710677f, 0.35355338f, 0.53033006f, 0.17677669f,
		-0.17677669f, 0.17677669f, 0.088388346f, -0.0f,
	];
}
