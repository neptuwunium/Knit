// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class TextTrackEntry {
	[GrannyMember("TimeStamp")] public float Timestamp { get; set; }
	public string Text { get; set; } = "";
}
