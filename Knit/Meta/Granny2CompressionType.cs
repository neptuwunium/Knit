namespace Knit.Meta;

public enum Granny2CompressionType {
	None,

	// granny2_x64.dll has different methods for Oodle0 and Oodle1.
	// Oodle0 is functionally the same as Oodle1, but the parameters are in a slightly different layout.
	// This is not a modern Oodle super-compressor like Kraken or Mermaid, but a legacy compressor (likely LZF/LZW.)
	Oodle0,
	Oodle1,

	// granny2_x64.dll considers BitKnit0 and BitKnit1 the same.
	// I assume 0 is a 32-bit variant since BitKnit1 punts all types to 64-bit which it does not do for Oodle0 and 1.
	BitKnit0,
	BitKnit1,
}
