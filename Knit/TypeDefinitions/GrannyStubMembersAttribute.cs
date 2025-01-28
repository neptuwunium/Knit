// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

// workaround for games that fake a 64-bit number by having two consecutive identical values
[AttributeUsage(AttributeTargets.Class)]
public sealed class GrannyStubMembersAttribute : Attribute;
