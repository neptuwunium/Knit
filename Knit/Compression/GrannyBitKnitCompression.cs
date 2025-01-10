// SPDX-FileCopyrightText: 2025 Legiayayana (Knit, EUPL-1.2), 2024 eiz (pybg3, MIT)
//
// SPDX-License-Identifier: EUPL-1.2
// SPDX-License-Identifier: MIT
//
// BitKnit compression code is derived from:
//	https://github.com/eiz/pybg3/blob/9ebda24314822bf35580e74bb7917f666ae046c6/src/rans.h

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Knit.Compression;

public static partial class GrannyBitKnitCompression {
	public static int Decompress(Span<byte> compressed, Span<byte> decompress) {
		using var state = new Bitknit2State(decompress);
		if (!state.Decode(MemoryMarshal.Cast<byte, ushort>(compressed))) {
			return -1;
		}

		return decompress.Length;
	}

	private sealed class FrequencyTable : IDisposable {
		public FrequencyTable(int frequencyBits, int vocabSize, int lookupBits) {
			Trace.Assert(frequencyBits is > 0 and < 16);
			Trace.Assert(lookupBits <= frequencyBits);
			Trace.Assert(vocabSize > 0 && vocabSize < 1 << frequencyBits);
			Trace.Assert(lookupBits > 0);

			FrequencyBits = frequencyBits;
			VocabSize = vocabSize;
			LookupShift = frequencyBits - lookupBits;
			LookupBits = lookupBits;
			Sums = ArrayPool<ushort>.Shared.Rent(VocabSize + 1);
			Lookup = ArrayPool<ushort>.Shared.Rent(1 << LookupBits);
		}

		public int FrequencyBits { get; }
		public int VocabSize { get; }
		private int LookupShift { get; }
		private int LookupBits { get; }
		public ushort[] Sums { get; }
		private ushort[] Lookup { get; }

		public void Dispose() {
			ArrayPool<ushort>.Shared.Return(Sums);
			ArrayPool<ushort>.Shared.Return(Lookup);
		}

		public uint FindSymbol(int code) {
			var sym = Lookup[code >> LookupShift];
			while (code >= Sums[sym + 1]) {
				sym++;
			}

			return sym;
		}

		public void FinishUpdate() {
			var code = 0;
			var sym = 0;
			var next = Sums[1];
			while (code < 1 << FrequencyBits) {
				if (code < next) {
					Lookup[code >> LookupShift] = (ushort) sym;
					code += 1 << LookupShift;
				} else {
					sym++;
					next = Sums[sym + 1];
				}
			}
		}

		public ushort Frequency(ushort sym) => (ushort) (Sums[sym + 1] - Sums[sym]);
		public ushort SumBelow(ushort sym) => Sums[sym];
	}

	private ref struct BoundedStack(Span<ushort> data, int index) {
		public Span<ushort> Stream { get; } = data;
		public int Index { get; private set; } = index;

		public ushort Pop() {
			if (Index >= Stream.Length) {
				throw new InvalidOperationException("Stack underflow");
			}

			return Stream[Index++];
		}

		public ushort Peek() {
			if (Index >= Stream.Length) {
				throw new InvalidOperationException("Stack underflow");
			}

			return Stream[Index];
		}

		public Span<ushort> Current() => Stream[Index..];

		public void Slide(int n) {
			Index += n;
		}
	}

	private sealed class DeferredAdaptiveModel : IDisposable {
		public DeferredAdaptiveModel(int adaptationInterval, int vocabSize, int numMinProbableSymbols, int frequencyBits, int lookupBits) {
			NumEquiprobableSymbols = vocabSize - numMinProbableSymbols;
			NumMinProbableSymbols = numMinProbableSymbols;
			AdaptationInterval = adaptationInterval;
			TotalSum = (ushort) (1 << frequencyBits);
			FrequencyIncr = (ushort) ((TotalSum - vocabSize) / AdaptationInterval);
			LastFrequencyIncr = (ushort) (1 + TotalSum - vocabSize - FrequencyIncr * AdaptationInterval);

			Trace.Assert(NumEquiprobableSymbols > 0);
			CDF = new FrequencyTable(frequencyBits, vocabSize, lookupBits);
			for (var i = 0; i < NumEquiprobableSymbols; ++i) {
				CDF.Sums[i] = (ushort) ((TotalSum - NumMinProbableSymbols) * i / NumEquiprobableSymbols);
			}

			for (var i = NumEquiprobableSymbols; i <= vocabSize; ++i) {
				CDF.Sums[i] = (ushort) (TotalSum - vocabSize + i);
			}

			FrequencyAccumulator = ArrayPool<ushort>.Shared.Rent(vocabSize);
			for (var i = 0; i < vocabSize; ++i) {
				FrequencyAccumulator[i] = 1;
			}

			CDF.FinishUpdate();
		}

		private int NumEquiprobableSymbols { get; }
		private int NumMinProbableSymbols { get; }
		private int AdaptationInterval { get; }
		private ushort TotalSum { get; }
		private ushort FrequencyIncr { get; }
		private ushort LastFrequencyIncr { get; }
		public FrequencyTable CDF { get; }
		private ushort[] FrequencyAccumulator { get; }
		private int AdaptationCounter { get; set; }

		public void Dispose() {
			CDF.Dispose();
			ArrayPool<ushort>.Shared.Return(FrequencyAccumulator);
		}

		public void ObserveSymbol(ushort symbol) {
			FrequencyAccumulator[symbol] += FrequencyIncr;
			AdaptationCounter = (AdaptationCounter + 1) % AdaptationInterval;
			if (AdaptationCounter == 0) {
				FrequencyAccumulator[symbol] += LastFrequencyIncr;
				uint sum = 0;
				for (var i = 1; i <= CDF.VocabSize; ++i) {
					sum += FrequencyAccumulator[i - 1];
					CDF.Sums[i] = (ushort) (CDF.Sums[i] + (sum - CDF.Sums[i]) / 2);
					FrequencyAccumulator[i - 1] = 1;
				}

				CDF.FinishUpdate();
			}
		}
	}

	private sealed class RANSState {
		public RANSState() {
			RefillShift = 16;
			RefillThreshold = 1u << RefillShift;
			Bits = RefillThreshold;
		}

		public RANSState(uint bits) : this() => Bits = bits;

		private int RefillShift { get; }
		private uint RefillThreshold { get; }
		public uint Bits { get; set; }

		public uint PopBits(ref BoundedStack stream, int nbits) {
			Trace.Assert(nbits < RefillShift);
			var sym = Bits & ((1u << nbits) - 1);
			Bits >>= nbits;
			MaybeRefill(ref stream);
			return sym;
		}

		public uint PopCDF(ref BoundedStack stream, FrequencyTable cdf) {
			Trace.Assert(cdf.FrequencyBits < RefillShift);
			var code = Bits & ((1u << cdf.FrequencyBits) - 1);
			var sym = cdf.FindSymbol((ushort) code); // note: suspicious cast
			var freq = cdf.Frequency((ushort) sym); // note: suspicious cast
			Bits = (Bits >> cdf.FrequencyBits) * freq + code - cdf.SumBelow((ushort) sym); // note: suspicious cast
			MaybeRefill(ref stream);
			return sym;
		}

		public void MaybeRefill(ref BoundedStack stream) {
			if (Bits < RefillThreshold) {
				Bits = (Bits << RefillShift) | stream.Pop();
			}
		}
	}

	private sealed class RegisterLRUCache : IDisposable {
		public RegisterLRUCache() {
			Entries = ArrayPool<uint>.Shared.Rent(8);
			for (var i = 0; i < 8; ++i) {
				Entries[i] = 1;
			}
		}

		private uint[] Entries { get; }
		private uint EntryOrder { get; set; } = 0x76543210;

		public void Dispose() {
			ArrayPool<uint>.Shared.Return(Entries);
		}

		public void Insert(uint value) {
			Entries[EntryOrder >> 28] = Entries[(EntryOrder >> 24) & 15];
			Entries[(EntryOrder >> 24) & 15] = value;
		}

		public uint Hit(int index) {
			var slot = (EntryOrder >> (index * 4)) & 15;
			var rotate_mask = (16u << (index * 4)) - 1;
			var rotated_order = ((EntryOrder << 4) | slot) & rotate_mask;
			EntryOrder = (EntryOrder & ~rotate_mask) | rotated_order;
			return Entries[slot];
		}
	}

	private ref struct Bitknit2State : IDisposable {
		public Bitknit2State(Span<byte> dst) {
			Dst = dst;

			CommandWorldModels = ArrayPool<DeferredAdaptiveModel>.Shared.Rent(4);
			CacheReferenceModels = ArrayPool<DeferredAdaptiveModel>.Shared.Rent(4);
			CopyOffsetModel = new DeferredAdaptiveModel(1024, 21, 0, 15, 10);
			CopyOffsetCache = new RegisterLRUCache();

			for (var i = 0; i < 4; ++i) {
				CommandWorldModels[i] = new DeferredAdaptiveModel(1024, 300, 36, 15, 10);
				CacheReferenceModels[i] = new DeferredAdaptiveModel(1024, 40, 0, 15, 10);
			}
		}

		private Span<byte> Dst { get; }
		private int Index { get; set; }
		private DeferredAdaptiveModel[] CommandWorldModels { get; }
		private DeferredAdaptiveModel[] CacheReferenceModels { get; }
		private DeferredAdaptiveModel CopyOffsetModel { get; }
		private RegisterLRUCache CopyOffsetCache { get; }
		private int DeltaOffset { get; set; } = 1;

		public bool Decode(Span<ushort> data) {
			var src = new BoundedStack(data, 0);
			if (src.Pop() != 0x75B1) {
				return false;
			}

			var index = Index;
			while (index < Dst.Length) {
				if (src.Index == src.Stream.Length) {
					Index = index;
					return false;
				}

				DecodeQuantum(ref src, ref index);
			}

			Index = index;
			return true;
		}

		private void DecodeQuantum(ref BoundedStack src, ref int offset) {
			var boundary = int.Min(Dst.Length, (int) (offset & 0xFFFF0000) + 0x10000);
			if (src.Peek() == 0) {
				src.Pop();
				var copyLength = int.Min((src.Stream.Length - src.Index) * 2, boundary - offset);
				src.Current().AsBytes()[..copyLength].CopyTo(Dst[offset..]);
				offset += copyLength;
				src.Slide(copyLength / 2);
				return;
			}

			var state1 = new RANSState();
			var state2 = new RANSState();
			DecodeInitialState(ref src, ref state1, ref state2);
			if (offset == 0) {
				Dst[offset++] = (byte) PopBits(ref src, 8, ref state1, ref state2);
			}

			while (offset < boundary) {
				var modelIndex = offset % 4;
				var command = PopModel(ref src, CommandWorldModels[modelIndex], ref state1, ref state2);
				if (command >= 256) {
					DecodeCopy(ref src, command, ref state1, ref state2, ref offset);
				} else {
					Dst[offset] = (byte) ((byte) command + Dst[offset - DeltaOffset]);
					offset++;
				}
			}

			if (state1.Bits != 0x10000 && state2.Bits != 0x10000) {
				throw new InvalidDataException("RANS Stream Corrupted");
			}
		}

		private void DecodeCopy(ref BoundedStack src, uint command, ref RANSState state1, ref RANSState state2, ref int offset) {
			var modelIndex = offset % 4;
			uint copyLength;
			if (command < 288) {
				copyLength = command - 254;
			} else {
				var copyLengthLength = (int) (command - 287);
				var copyLengthBits = PopBits(ref src, copyLengthLength, ref state1, ref state2);
				copyLength = (1u << copyLengthLength) + copyLengthBits + 32;
			}

			uint copyOffset;
			var cacheRef = PopModel(ref src, CacheReferenceModels[modelIndex], ref state1, ref state2);
			if (cacheRef < 8) {
				copyOffset = CopyOffsetCache.Hit((int) cacheRef);
			} else {
				var copyOffsetLength = PopModel(ref src, CopyOffsetModel, ref state1, ref state2);
				var copyOffsetBits = PopBits(ref src, (int) (copyOffsetLength % 16), ref state1, ref state2);
				if (copyOffsetLength > 16) {
					copyOffsetBits = (copyOffsetBits << 16) | src.Pop();
				}

				copyOffset = (32u << (int) copyOffsetLength) + (copyOffsetBits << 5) - 32u + (cacheRef - 7);
				CopyOffsetCache.Insert(copyOffset);
			}

			DeltaOffset = (int) copyOffset;
			for (var i = 0; i < copyLength; ++i) {
				var copyPos = (int) (offset - copyOffset);
				Dst[offset] = Dst[copyPos];
				offset++;
			}
		}

		public void Dispose() {
			foreach (var model in (DeferredAdaptiveModel?[]) CommandWorldModels) {
				model?.Dispose();
			}

			ArrayPool<DeferredAdaptiveModel>.Shared.Return(CommandWorldModels);

			foreach (var model in (DeferredAdaptiveModel?[]) CacheReferenceModels) {
				model?.Dispose();
			}

			ArrayPool<DeferredAdaptiveModel>.Shared.Return(CacheReferenceModels);

			CopyOffsetModel.Dispose();
			CopyOffsetCache.Dispose();
		}

		private static uint PopBits(ref BoundedStack src, int nbits, ref RANSState state1, ref RANSState state2) {
			var result = state1.PopBits(ref src, nbits);
			(state1, state2) = (state2, state1);
			return result;
		}

		private static uint PopModel(ref BoundedStack src, DeferredAdaptiveModel model, ref RANSState state1, ref RANSState state2) {
			var result = state1.PopCDF(ref src, model.CDF);
			model.ObserveSymbol((ushort) result); // note: suspicious cast
			(state1, state2) = (state2, state1);
			return result;
		}

		private static void DecodeInitialState(ref BoundedStack src, ref RANSState state1, ref RANSState state2) {
			var init_0 = src.Pop();
			var init_1 = src.Pop();
			var merged = new RANSState(((uint) init_0 << 16) | init_1);

			var split = (int) merged.PopBits(ref src, 4);
			state1.Bits = merged.Bits >> split;
			state1.MaybeRefill(ref src);
			state2.Bits = (merged.Bits << 16) | src.Pop();
			state2.Bits &= (1u << (16 + split)) - 1;
			state2.Bits |= 1u << (16 + split);
		}
	}
}
