// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

[Flags]
public enum VertexAttributePresence : ulong {
	Position = 1 << 0,
	Normal = 1 << 1,
	Tangent = 1 << 2,
	Binormal = 1 << 3,
	TangentBinormalCross = 1 << 4,
	BoneWeights = 1 << 5,
	BoneIndices = 1 << 6,
	DiffuseColor = 1 << 7,
	SpecularColor = 1 << 8,
	TangentHasW = 1ul << 32,
	TextureCoordinates0 = 1ul << 33,
	TextureCoordinates1 = 1ul << 34,
	TextureCoordinates2 = 1ul << 35,
	TextureCoordinates3 = 1ul << 36,
	TextureCoordinates4 = 1ul << 37,
	TextureCoordinates5 = 1ul << 38,
	TextureCoordinates6 = 1ul << 39,
	TextureCoordinates7 = 1ul << 40,
}
