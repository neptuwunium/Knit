// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class PeriodicLoop {
	public float Radius { get; set; }
	[GrannyMember("dAngle")] public float Angle { get; set; }
	[GrannyMember("dZ")] public float Z { get; set; }
	public Vector3 BasisX { get; set; }
	public Vector3 BasisY { get; set; }
	public Vector3 BasisZ { get; set; }
	public Vector3 Axis { get; set; }
}
