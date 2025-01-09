// SPDX-License-Identifier: EUPL-1.2
// SPDX-License-Identifier: MPL-2.0
// SPDX-Copyright: Legiayayana (Knit, EUPL-1.2), arves100 (opengr2, MPL-2.0), arbos (nwn2mdk, Boost)
// Oodle1 compression code is derived from:
//	https://github.com/Arbos/nwn2mdk/blob/2e11e0d2a765d0e60f974f31846efdd220fd48ef/nwn2mdk-lib/gr2_decompress.cpp
//	https://github.com/arves100/opengr2/blob/e50487abd1b1618211c5e64eab57da4632ffa9f2/libopengrn/oodle1.c

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Knit.Compression;

public static partial class GrannyOodleCompression {
	public static bool AllowUnmanagedDecompression { get; set; } = false;

	public static int Decompress(Span<byte> compressed, Span<byte> decompress, int stop1, int stop2, int stop3, bool reverseEndianness, bool isOodle1) {
		if (AllowUnmanagedDecompression &&
		    ((OperatingSystem.IsLinux() && File.Exists("libopengrn.so")) ||
		     (OperatingSystem.IsWindows() && File.Exists("libopengrn.dll")) ||
		     (OperatingSystem.IsMacOS() && File.Exists("libopengrn.dylib")))) {
			unsafe {
				fixed (void* compressedPtr = compressed) {
					fixed (void* decompressedPtr = decompress) {
						if (!NativeMethods.Compression_UnOodle1((nint) compressedPtr, compressed.Length, (nint) decompressedPtr, decompress.Length, stop1, stop2, reverseEndianness, !isOodle1)) {
							return -1;
						}

						return decompress.Length;
					}
				}
			}
		}

		return DecompressManaged(compressed, decompress, stop1, stop2, stop3, reverseEndianness, isOodle1);
	}

	public static int DecompressManaged(Span<byte> compressed, Span<byte> decompress, int stop1, int stop2, int stop3, bool reverseEndianness, bool isOodle1) {
		var fragSize = Unsafe.SizeOf<OodleFragmentHeader>() * 3;
		if (reverseEndianness) {
			if (isOodle1) {
				compressed[..fragSize].Reverse32();
			} else {
				compressed.Reverse32();
			}
		}

		var fragments = MemoryMarshal.Cast<byte, OodleFragmentHeader>(compressed[..fragSize]);

		var decoder = new OodleContext {
			Numerator = (uint) (compressed[fragSize] >> 1),
			Denominator = 0x80,
			Stream = compressed[fragSize..],
		};

		Span<int> ranges = stackalloc int[3];
		ranges[0] = stop1;
		ranges[1] = stop2;
		ranges[2] = stop3;

		var cursor = 0;
		for (var i = 0; i < 3; ++i) {
			using var dict = new OodleDictionary(fragments[i]);

			var stop = ranges[i];
			while (cursor < stop) {
				cursor += dict.Decompress(ref decoder, decompress, cursor);
			}
		}

		return cursor;
	}

	private static partial class NativeMethods {
		[LibraryImport("libopengrn"), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
		[return: MarshalAs(UnmanagedType.I4)]
		public static partial bool Compression_UnOodle1(nint compressedData, int compressedLength, nint decompressedData, int decompressedLength, int oodleStop1, int oodleStop2, [MarshalAs(UnmanagedType.I4)] bool endianessMismatch, [MarshalAs(UnmanagedType.I4)] bool isOodle0);
	}

	[InlineArray(4)]
	public struct OodleFragmentSizes : IEquatable<OodleFragmentSizes> {
		public byte Value;

		public bool Equals(OodleFragmentSizes other) => ((Span<byte>) this).SequenceEqual(other);
		public override bool Equals(object? obj) => obj is OodleFragmentSizes other && Equals(other);
		public override int GetHashCode() => MemoryMarshal.Read<int>(this);
		public static bool operator ==(OodleFragmentSizes left, OodleFragmentSizes right) => left.Equals(right);
		public static bool operator !=(OodleFragmentSizes left, OodleFragmentSizes right) => !(left == right);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public record struct OodleFragmentHeader {
		public uint Value1 { get; set; }
		public uint Value2 { get; set; }
		public OodleFragmentSizes SizeCount { get; set; }

		public int DecodedValueMax => (int) (Value1 & 0x1FF);
		public int BackrefValueMax => (int) (Value1 >> 9);
		public int DecodedCount => (int) (Value2 & 0x1FF);
		public int HighBitCount => (int) (Value2 >> 19);
	}

	private ref struct OodleContext {
		public uint Numerator { get; set; }
		public uint Denominator { get; set; }
		public Span<byte> Stream { get; set; }
		private uint NextDenominator { get; set; }

		public ushort Decode(ushort max) {
			for (; Denominator <= 0x800000; Denominator <<= 8) {
				Numerator <<= 8;
				Numerator |= (ushort) ((Stream[0] << 7) & 0x80);
				Numerator |= (ushort) ((Stream[1] >> 1) & 0x7f);
				Stream = Stream[1..];
			}

			NextDenominator = Denominator / max;
			return (ushort) uint.Min(Numerator / NextDenominator, (uint) (max - 1));
		}

		public ushort Commit(ushort max, ushort val, ushort err) {
			Numerator -= NextDenominator * val;

			if (val + err < max) {
				Denominator = NextDenominator * err;
			} else {
				Denominator -= NextDenominator * val;
			}

			return val;
		}

		public ushort DecodeCommit(ushort max) => Commit(max, Decode(max), 1);
	}

	private sealed class OodleWindow : IDisposable {
		public OodleWindow(int maxValue, int countCap) {
			Total = 4;
			CountCap = countCap + 1;

			RangesLength = 2;
			Ranges = ArrayPool<ushort>.Shared.Rent(RangesLength);
			Ranges[0] = 0;
			Ranges[1] = 0x4000;

			WeightsLength = 1;
			Weights = ArrayPool<ushort>.Shared.Rent(WeightsLength);
			Weights[0] = 4;

			ValuesLength = 1;
			Values = ArrayPool<ushort>.Shared.Rent(ValuesLength);
			Values[0] = 0;

			Increase = 4;
			RangeRebuild = 8;
			WeightRebuild = uint.Max(256, uint.Min((uint) (32 * maxValue), 15160));
			IncreaseCap = maxValue <= 64 ? 128 : uint.Min((uint) (2 * maxValue), (WeightRebuild >> 1) - 32);
		}

		private int CountCap { get; }

		private ushort[] Ranges { get; set; }

		public ushort[] Values { get; private set; }

		private ushort[] Weights { get; set; }

		// can't use RangesLength because ArrayPool aligns up, size may actually be larger than what we're aware of.
		private int RangesLength { get; set; }
		private int ValuesLength { get; set; }
		private int WeightsLength { get; set; }
		private uint Total { get; set; }
		private uint Increase { get; set; }
		private uint IncreaseCap { get; }
		private uint RangeRebuild { get; set; }
		private uint WeightRebuild { get; }

		public void Dispose() {
			ArrayPool<ushort>.Shared.Return(Ranges);
			ArrayPool<ushort>.Shared.Return(Values);
			ArrayPool<ushort>.Shared.Return(Weights);
		}

		private void RebuildRanges() {
			if (RangesLength != WeightsLength + 1) {
				Ranges = Ranges.PooledResize(RangesLength, WeightsLength + 1);
				RangesLength = WeightsLength + 1;
			}

			var rangeWeight = (ushort) (0x20000 / Total);
			ushort rangeStart = 0;
			for (var i = 0; i < WeightsLength; ++i) {
				Ranges[i] = rangeStart;
				rangeStart += (ushort) (Weights[i] * rangeWeight / 8);
			}

			Ranges[RangesLength - 1] = 0x4000;

			if (Increase > IncreaseCap >> 1) {
				RangeRebuild = Total + IncreaseCap;
			} else {
				Increase <<= 1;
				RangeRebuild = Total + Increase;
			}
		}

		private void RebuildWeights() {
			ushort weightTotal = 0;
			for (var i = 0; i < WeightsLength; ++i) {
				var weight = Weights[i] >>= 1;
				weightTotal += weight;
			}

			Total = weightTotal;

			for (var i = 1; i < WeightsLength; i++) {
				while (i < WeightsLength && Weights[i] == 0) {
					Weights[i] = Weights[--WeightsLength];
					Values[i] = Values[--ValuesLength];
				}
			}

			var index = Weights.AsSpan(1, WeightsLength - 1).MaxIndex() + 1;
			if (index < WeightsLength) {
				var tmp = Weights[index];
				Weights[index] = Weights[WeightsLength - 1];
				Weights[WeightsLength - 1] = tmp;

				tmp = Values[index];
				Values[index] = Values[ValuesLength - 1];
				Values[ValuesLength - 1] = tmp;
			}

			if (WeightsLength < CountCap && Weights[0] == 0) {
				Weights[0] = 1;
				Total++;
			}
		}

		public OodlePair Decode(ref OodleContext decoder) {
			if (Total >= RangeRebuild) {
				if (RangeRebuild >= WeightRebuild) {
					RebuildWeights();
				}

				RebuildRanges();
			}

			var value = decoder.Decode(0x4000);
			var rangeIndex = Ranges.AsSpan(0, RangesLength - 1).UpperBound(value) - 1;
			if (rangeIndex < 0) {
				rangeIndex = 0;
			}

			decoder.Commit(0x4000, Ranges[rangeIndex], (ushort) (Ranges[rangeIndex + 1] - Ranges[rangeIndex]));

			Weights[rangeIndex]++;
			Total++;

			if (rangeIndex > 0) {
				return new OodlePair {
					Index = -1,
					Value = Values[rangeIndex],
				};
			}

			if (WeightsLength >= RangesLength && decoder.DecodeCommit(2) == 1) {
				var index = RangesLength + decoder.DecodeCommit((ushort) (WeightsLength - RangesLength + 1)) - 1;
				Weights[index] += 2;
				Total += 2;
				return new OodlePair {
					Index = -1,
					Value = Values[index],
				};
			}

			Values = Values.PooledResize(ValuesLength, ++ValuesLength);
			Values[ValuesLength - 1] = 0;
			Weights = Weights.PooledResize(WeightsLength, ++WeightsLength);
			Weights[WeightsLength - 1] = 2;

			Total += 2;

			if (WeightsLength >= CountCap) {
				Total -= Weights[0];
				Weights[0] = 0;
			}

			return new OodlePair {
				Index = ValuesLength - 1,
				Value = 0,
			};
		}
	}

	private sealed class OodleDictionary : IDisposable {
		private static readonly int[] RefList = [0x80, 0xC0, 0x100, 0x200];

		public OodleDictionary(OodleFragmentHeader fragment) {
			DecodedSize = 0;
			BackrefSize = 0;

			DecodedValueMax = fragment.DecodedValueMax;
			BackrefValueMax = fragment.BackrefValueMax;
			LowBitValueMax = int.Min(fragment.BackrefValueMax + 1, 4);
			MidBitValueMax = int.Min((fragment.BackrefValueMax >> 2) + 1, 256);
			HighBitValueMax = (fragment.BackrefValueMax >> 10) + 1;

			LowBitWindow = new OodleWindow(LowBitValueMax - 1, LowBitValueMax);
			HighBitWindow = new OodleWindow(HighBitValueMax - 1, fragment.HighBitCount + 1);

			MidBitWindows = ArrayPool<OodleWindow>.Shared.Rent(HighBitValueMax);
			for (var i = 0; i < MidBitWindows.Length; ++i) {
				MidBitWindows[i] = new OodleWindow(MidBitValueMax - 1, MidBitValueMax);
			}

			DecodedWindows = ArrayPool<OodleWindow>.Shared.Rent(4);
			for (var i = 0; i < 4; ++i) {
				DecodedWindows[i] = new OodleWindow(DecodedValueMax - 1, fragment.DecodedCount);
			}

			SizeWindows = ArrayPool<OodleWindow>.Shared.Rent(0x41);
			for (var i = 0; i < 4; ++i) {
				for (var j = 0; j < 16; ++j) {
					SizeWindows[(i << 4) + j] = new OodleWindow(64, fragment.SizeCount[3 - i]);
				}
			}

			SizeWindows[64] = new OodleWindow(64, fragment.SizeCount[0]);
		}

		private int DecodedSize { get; set; }
		private int BackrefSize { get; set; }
		private int DecodedValueMax { get; }
		private int BackrefValueMax { get; }
		private int LowBitValueMax { get; }
		private int MidBitValueMax { get; }
		private int HighBitValueMax { get; }
		private OodleWindow LowBitWindow { get; }
		private OodleWindow HighBitWindow { get; }
		private OodleWindow[] MidBitWindows { get; }
		private OodleWindow[] DecodedWindows { get; }
		private OodleWindow[] SizeWindows { get; }

		public void Dispose() {
			LowBitWindow.Dispose();
			HighBitWindow.Dispose();

			foreach (var window in (OodleWindow?[]) MidBitWindows) {
				window?.Dispose();
			}

			ArrayPool<OodleWindow>.Shared.Return(MidBitWindows);

			foreach (var window in (OodleWindow?[]) DecodedWindows) {
				window?.Dispose();
			}

			ArrayPool<OodleWindow>.Shared.Return(DecodedWindows);

			foreach (var window in (OodleWindow?[]) SizeWindows) {
				window?.Dispose();
			}

			ArrayPool<OodleWindow>.Shared.Return(SizeWindows);
		}

		public int Decompress(ref OodleContext decoder, Span<byte> decompress, int cursor) {
			var d1 = SizeWindows[BackrefSize].Decode(ref decoder);
			if (d1.Index > -1) {
				d1.Value = SizeWindows[BackrefSize].Values[d1.Index] = decoder.DecodeCommit(65);
			}

			BackrefSize = d1.Value;

			if (BackrefSize <= 0) {
				var index = cursor % 4;
				var d2 = DecodedWindows[index].Decode(ref decoder);
				if (d2.Index > -1) {
					d2.Value = DecodedWindows[index].Values[d2.Index] = decoder.DecodeCommit((ushort) DecodedValueMax);
				}

				decompress[cursor] = (byte) d2.Value;
				DecodedSize++;
				return 1;
			}

			var backrefSize = BackrefSize < 61 ? BackrefSize + 1 : RefList[BackrefSize - 61u];
			var backrefRange = int.Min(BackrefValueMax, DecodedSize);

			var d3 = LowBitWindow.Decode(ref decoder);
			if (d3.Index > -1) {
				d3.Value = LowBitWindow.Values[d3.Index] = decoder.DecodeCommit((ushort) LowBitValueMax);
			}

			var d4 = HighBitWindow.Decode(ref decoder);
			if (d4.Index > -1) {
				d4.Value = HighBitWindow.Values[d4.Index] = decoder.DecodeCommit((ushort) (backrefRange / 1024u + 1));
			}

			var d5 = MidBitWindows[d4.Value].Decode(ref decoder);
			if (d5.Index > -1) {
				d5.Value = MidBitWindows[d4.Value].Values[d5.Index] = decoder.DecodeCommit((ushort) int.Min(backrefRange / 4 + 1, 256));
			}

			var backrefOffset = (int) (((uint) d4.Value << 10) + ((uint) d5.Value << 2) + d3.Value + 1u);
			DecodedSize += backrefSize;
			var repeat = backrefSize / backrefOffset;
			var remain = backrefSize % backrefOffset;

			var backref = decompress.Slice(cursor - backrefOffset, backrefOffset);
			for (var i = 0; i < repeat; ++i) {
				var slice = decompress.Slice(cursor + i * backrefOffset, backrefOffset);
				backref.CopyTo(slice);
			}

			var remainSlice = decompress[(cursor + repeat * backrefOffset)..];
			if (remain > remainSlice.Length) {
				remain = remainSlice.Length;
			}

			backref[..remain].CopyTo(remainSlice);
			return backrefSize;
		}
	}

	private struct OodlePair {
		public int Index { get; init; }
		public ushort Value { get; set; }
	}
}
