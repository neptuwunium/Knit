// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;
using System.Runtime.InteropServices;
using Knit.Meta;
using Pluto.IO;

namespace Knit.TypeDefinitions;

public class CurveDX32F : CurveValue {
	public short Padding { get; set; }
	public float[] Knots { get; set; } = [];
	public float[] Controls { get; set; } = [];
	public override CurveDimension Dimension => (CurveDimension) (Controls.Length / Knots.Length);
	public override int Frames => Knots.Length;

	public override bool Visit(Granny2File file, string name, IGranny2TypeDefinition typeInfo, SpanPointer objectLocation) {
		if (name == "Degree") {
			Header = new CurveDataHeader {
				Format = CurveFormat.Unknown,
				Degree = MemoryMarshal.Read<byte>(objectLocation),
			};
			return true;
		}

		return base.Visit(file, name, typeInfo, objectLocation);
	}

	public override void Decompress(out List<float> timestamps, out List<Vector3> positions, out List<Quaternion> rotations, out List<Matrix3x3> scales) {
		base.Decompress(out timestamps, out positions, out rotations, out scales);

		timestamps.AddRange(Knots);
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
