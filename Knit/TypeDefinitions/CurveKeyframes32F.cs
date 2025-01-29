// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;
using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

public class CurveKeyframes32F : CurveValue {
	[GrannyMember("Dimension")] public CurveDimension DimensionRaw { get; set; }
	public float[] Controls { get; set; } = [];

	public override CurveDimension Dimension => DimensionRaw;
	public override int Frames => Controls.Length / (int) Dimension;

	public override void Decompress(out List<float> timestamps, out List<Vector3> positions, out List<Quaternion> rotations, out List<Matrix3x3> scales) {
		base.Decompress(out timestamps, out positions, out rotations, out scales);

		timestamps.AddRange(Enumerable.Range(0, Frames).Select(x => (float) x));
		var controlsSpan = MemoryMarshal.AsBytes(Controls.AsSpan());

		switch (Dimension) {
			case CurveDimension.Position: {
				foreach (var position in MemoryMarshal.Cast<byte, Vector3>(controlsSpan)) {
					positions.Add(position);
				}

				break;
			}
			case CurveDimension.Rotation: {
				foreach (var rotation in MemoryMarshal.Cast<byte, Quaternion>(controlsSpan)) {
					rotations.Add(rotation);
				}

				break;
			}
			case CurveDimension.ScaleShear: {
				foreach (var scale in MemoryMarshal.Cast<byte, Matrix3x3>(controlsSpan)) {
					scales.Add(scale);
				}

				break;
			}
		}
	}
}
