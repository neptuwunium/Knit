// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using Knit.Meta;
using Pluto.IO;

namespace Knit.TypeDefinitions;

public interface IGrannyType {
	public bool Visit(Granny2File file, string name, IGranny2TypeDefinition typeInfo, SpanPointer objectLocation);
}
