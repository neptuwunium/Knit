// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class CurveD4Constant32F : CurveValue {
	public short Padding { get; set; }
	public Quaternion Controls { get; set; }

	public override CurveDimension Dimension => CurveDimension.Rotation;
	public override int Frames => 1;

	public override void Decompress(out List<float> timestamps, out List<Vector3> positions, out List<Quaternion> rotations, out List<Matrix3x3> scales) {
		base.Decompress(out timestamps, out positions, out rotations, out scales);
		timestamps.Add(0.0f);
		rotations.Add(Controls);
	}
}
