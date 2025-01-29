// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class CurveIdentity : CurveValue {
	[GrannyMember("Dimension")] public CurveDimension DimensionRaw { get; set; }
	public override CurveDimension Dimension => DimensionRaw;
	public override int Frames => 1;

	public override void Decompress(out List<float> timestamps, out List<Vector3> positions, out List<Quaternion> rotations, out List<Matrix3x3> scales) {
		base.Decompress(out timestamps, out positions, out rotations, out scales);

		timestamps.Add(0.0f);
		switch (Dimension) {
			case CurveDimension.Position: {
				positions.Add(Vector3.Zero);
				break;
			}
			case CurveDimension.Rotation: {
				rotations.Add(Quaternion.Identity);
				break;
			}
			case CurveDimension.ScaleShear: {
				scales.Add(Matrix3x3.Identity);
				break;
			}
		}
	}
}
