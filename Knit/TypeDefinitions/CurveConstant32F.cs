// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class CurveConstant32F : CurveValue {
	public short Padding { get; set; }
	public Matrix3x3 Controls { get; set; } = Matrix3x3.Identity;

	public override int Frames => 1;
	public override CurveDimension Dimension => CurveDimension.ScaleShear;

	public override void Decompress(out List<float> timestamps, out List<Vector3> positions, out List<Quaternion> rotations, out List<Matrix3x3> scales) {
		base.Decompress(out timestamps, out positions, out rotations, out scales);
		timestamps.Add(0.0f);
		scales.Add(Controls);
	}
}
