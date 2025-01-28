// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using GLTF.Scaffold;
using Knit.TypeDefinitions;

namespace Knit.glTF;

public static class Extensions {
	public static void ToGLTF(this XForm transform, Node node, float scale = 1.0f) {
		if ((transform.Flags & XFormFlags.HasPosition) != 0) {
			node.Translation = (transform.Position * scale).ToGLTF();
		}

		if ((transform.Flags & XFormFlags.HasRotation) != 0) {
			node.Rotation = transform.Rotation.ToGLTF();
		}

		if ((transform.Flags & XFormFlags.HasScaleMatrix) != 0) {
			node.Scale = (transform.Scale * scale).ToGLTF();
		}
	}
}
