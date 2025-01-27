// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class Model : GrannyExtendable {
	public string Name { get; set; } = "";
	public Skeleton? Skeleton { get; set; }
	public XForm InitialPlacement { get; set; }
	public List<MeshBinding> MeshBindings { get; set; } = [];
}
