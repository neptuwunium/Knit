// SPDX-FileCopyrightText: 2025 Legiayayana, 2017-2025 Norbyte
//
// SPDX-License-Identifier: MIT

namespace Knit.TypeDefinitions;

// ref: https://github.com/Norbyte/lslib
// Types:
// Da: (animation) 3x3 matrix
// D[1-4]: 1 - 4 component vector
// I[1/3]: 1/3 values for the main diagonal, others are zero
// n: Normalized quaternion
// Constant: Constant vector/matrix
// K[n][nothing/u/f]: n-bit value for knots; u = unsigned; f = floating point
// C[n][nothing/u/f]: n-bit value for controls; u = unsigned; f = floating point
public enum CurveFormat : byte {
	DaKeyframes32f = 0,
	DaK32fC32f = 1,
	DaIdentity = 2,
	DaConstant32f = 3,
	D3Constant32f = 4,
	D4Constant32f = 5,
	DaK16uC16u = 6,
	DaK8uC8u = 7,
	D4nK16uC15u = 8,
	D4nK8uC7u = 9,
	D3K16uC16u = 10,
	D3K8uC8u = 11,
	D9I1K16uC16u = 12,
	D9I3K16uC16u = 13,
	D9I1K8uC8u = 14,
	D9I3K8uC8u = 15,
	D3I1K32fC32f = 16,
	D3I1K16uC16u = 17,
	D3I1K8uC8u = 18,
	Unknown = 0xFF,
}
