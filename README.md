<!--
SPDX-FileCopyrightText: 2025 Legiayayana

SPDX-License-Identifier: EUPL-1.2
-->

# Knit

A managed C# reader and decompressor for Gr2 files.

Due to Granny's extensible nature, it's better to find a specialized parser for a given game.

⚠️ This library will never support serializing new structures into granny files.

⚠️ Big Endian files, while supported are completely untested.

⚠️ Oodle0 compression codec is not supported.

⚠️ The Bink0 and Bink1 Texture Codecs are not supported, but planned.

## Attribution

- [opengr2](https://github.com/arves100/opengr2) (MPL-2.0) - Oodle decompression
- [pybg3](https://github.com/eiz/pybg3) (MIT) - BitKnit decompression
- [LSLib](https://github.com/Norbyte/lslib) (MIT) - Reference for Animations
