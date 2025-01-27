// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class TextTrack {
	public string Name { get; set; } = "";
	public List<TextTrackEntry> Entries { get; set; } = [];
}
