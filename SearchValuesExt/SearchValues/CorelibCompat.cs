using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers
{
    internal static class CorelibCompat
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShuffleUnsafe(Vector128<byte> vector, Vector128<byte> indices)
        {
            if (Ssse3.IsSupported)
            {
                return Ssse3.Shuffle(vector, indices);
            }

            if (AdvSimd.Arm64.IsSupported)
            {
                return AdvSimd.Arm64.VectorTableLookup(vector, indices);
            }

            return Vector128.Shuffle(vector, indices);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ResetLowestSetBit(uint value)
        {
            return value & (value - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong ResetLowestSetBit(ulong value)
        {
            return value & (value - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<byte> FixUpPackedVector256Result(Vector256<byte> result)
        {
            Debug.Assert(Avx2.IsSupported);
            // Avx2.PackUnsignedSaturate(Vector256.Create((short)1), Vector256.Create((short)2)) will result in
            // 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2
            // We want to swap the X and Y bits
            // 1, 1, 1, 1, 1, 1, 1, 1, X, X, X, X, X, X, X, X, Y, Y, Y, Y, Y, Y, Y, Y, 2, 2, 2, 2, 2, 2, 2, 2
            return Avx2.Permute4x64(result.AsInt64(), 0b_11_01_10_00).AsByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector512<byte> FixUpPackedVector512Result(Vector512<byte> result)
        {
            Debug.Assert(Avx512F.IsSupported);
            // Avx512BW.PackUnsignedSaturate will interleave the inputs in 8-byte blocks.
            // We want to preserve the order of the two input vectors, so we deinterleave the packed value.
            return Avx512F.PermuteVar8x64(result.AsInt64(), Vector512.Create(0, 2, 4, 6, 1, 3, 5, 7)).AsByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ToUpperAsciiInvariant(char c)
        {
            if (char.IsAsciiLetterLower(c))
            {
                c = (char)(c & 0x5F); // = low 7 bits of ~0x20
            }
            return c;
        }

        private delegate int ToUpperOrdinalDelegate(ReadOnlySpan<char> source, Span<char> destination);

        private static readonly ToUpperOrdinalDelegate s_toUpperOrdinal = typeof(object).Assembly
            .GetType("System.Globalization.Ordinal")!
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)!
            .Single(m => m.Name == "ToUpperOrdinal" && m.ReturnType == typeof(int))
            .CreateDelegate<ToUpperOrdinalDelegate>();

        private static readonly Func<char, char> s_ordinalCasingToUpper = typeof(object).Assembly
            .GetType("System.Globalization.OrdinalCasing")!
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)!
            .Single(m => m.Name == "ToUpper" && m.ReturnType == typeof(char))
            .CreateDelegate<Func<char, char>>();

        private delegate void SurrogateCasingToUpperlDelegate(char h, char l, out char hr, out char lr);

        private static readonly SurrogateCasingToUpperlDelegate s_surrogateCasingToUpper = typeof(object).Assembly
            .GetType("System.Globalization.SurrogateCasing")!
            .GetMethod("ToUpper", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<SurrogateCasingToUpperlDelegate>();

        public static bool Invariant => false; // TODO
        public static bool UseNls => false;

        private static readonly ushort[] s_basicLatin =
        {
            // Upper Casing

            /* 0000-000f */  0x0000, 0x0001, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007, 0x0008, 0x0009, 0x000a, 0x000b, 0x000c, 0x000d, 0x000e, 0x000f,
            /* 0010-001f */  0x0010, 0x0011, 0x0012, 0x0013, 0x0014, 0x0015, 0x0016, 0x0017, 0x0018, 0x0019, 0x001a, 0x001b, 0x001c, 0x001d, 0x001e, 0x001f,
            /* 0020-002f */  0x0020, 0x0021, 0x0022, 0x0023, 0x0024, 0x0025, 0x0026, 0x0027, 0x0028, 0x0029, 0x002a, 0x002b, 0x002c, 0x002d, 0x002e, 0x002f,
            /* 0030-003f */  0x0030, 0x0031, 0x0032, 0x0033, 0x0034, 0x0035, 0x0036, 0x0037, 0x0038, 0x0039, 0x003a, 0x003b, 0x003c, 0x003d, 0x003e, 0x003f,
            /* 0040-004f */  0x0040, 0x0041, 0x0042, 0x0043, 0x0044, 0x0045, 0x0046, 0x0047, 0x0048, 0x0049, 0x004a, 0x004b, 0x004c, 0x004d, 0x004e, 0x004f,
            /* 0050-005f */  0x0050, 0x0051, 0x0052, 0x0053, 0x0054, 0x0055, 0x0056, 0x0057, 0x0058, 0x0059, 0x005a, 0x005b, 0x005c, 0x005d, 0x005e, 0x005f,
            /* 0060-006f */  0x0060, 0x0041, 0x0042, 0x0043, 0x0044, 0x0045, 0x0046, 0x0047, 0x0048, 0x0049, 0x004a, 0x004b, 0x004c, 0x004d, 0x004e, 0x004f,
            /* 0070-007f */  0x0050, 0x0051, 0x0052, 0x0053, 0x0054, 0x0055, 0x0056, 0x0057, 0x0058, 0x0059, 0x005a, 0x007b, 0x007c, 0x007d, 0x007e, 0x007f,
            /* 0080-008f */  0x0080, 0x0081, 0x0082, 0x0083, 0x0084, 0x0085, 0x0086, 0x0087, 0x0088, 0x0089, 0x008a, 0x008b, 0x008c, 0x008d, 0x008e, 0x008f,
            /* 0090-009f */  0x0090, 0x0091, 0x0092, 0x0093, 0x0094, 0x0095, 0x0096, 0x0097, 0x0098, 0x0099, 0x009a, 0x009b, 0x009c, 0x009d, 0x009e, 0x009f,
            /* 00a0-00af */  0x00a0, 0x00a1, 0x00a2, 0x00a3, 0x00a4, 0x00a5, 0x00a6, 0x00a7, 0x00a8, 0x00a9, 0x00aa, 0x00ab, 0x00ac, 0x00ad, 0x00ae, 0x00af,
            /* 00b0-00bf */  0x00b0, 0x00b1, 0x00b2, 0x00b3, 0x00b4, 0x039c, 0x00b6, 0x00b7, 0x00b8, 0x00b9, 0x00ba, 0x00bb, 0x00bc, 0x00bd, 0x00be, 0x00bf,
            /* 00c0-00cf */  0x00c0, 0x00c1, 0x00c2, 0x00c3, 0x00c4, 0x00c5, 0x00c6, 0x00c7, 0x00c8, 0x00c9, 0x00ca, 0x00cb, 0x00cc, 0x00cd, 0x00ce, 0x00cf,
            /* 00d0-00df */  0x00d0, 0x00d1, 0x00d2, 0x00d3, 0x00d4, 0x00d5, 0x00d6, 0x00d7, 0x00d8, 0x00d9, 0x00da, 0x00db, 0x00dc, 0x00dd, 0x00de, 0x00df,
            /* 00e0-00ef */  0x00c0, 0x00c1, 0x00c2, 0x00c3, 0x00c4, 0x00c5, 0x00c6, 0x00c7, 0x00c8, 0x00c9, 0x00ca, 0x00cb, 0x00cc, 0x00cd, 0x00ce, 0x00cf,
            /* 00f0-00ff */  0x00d0, 0x00d1, 0x00d2, 0x00d3, 0x00d4, 0x00d5, 0x00d6, 0x00f7, 0x00d8, 0x00d9, 0x00da, 0x00db, 0x00dc, 0x00dd, 0x00de, 0x0178,
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ToUpperOrdinal(char c)
        {
            //if (Invariant)
            //{
            //    return InvariantModeCasing.ToUpper(c);
            //}

            //if (UseNls)
            //{
            //    return char.IsAscii(c)
            //        ? ToUpperAsciiInvariant(c)
            //        : Invariant.ChangeCase(c, toUpper: true);
            //}

            int pageNumber = ((int)c) >> 8;
            if (pageNumber == 0) // optimize for ASCII range
            {
                return (char)s_basicLatin[(int)c];
            }

            return s_ordinalCasingToUpper(c);
        }

        internal static int ToUpperOrdinal(ReadOnlySpan<char> source, Span<char> destination) =>
            s_toUpperOrdinal(source, destination);

        internal static void SurrogateCasingToUpper(char h, char l, out char hr, out char lr) =>
            s_surrogateCasingToUpper(h, l, out hr, out lr);
    }

    internal ref partial struct ValueListBuilder<T>
    {
        private Span<T> _span;
        private T[]? _arrayFromPool;
        private int _pos;

        public ValueListBuilder(Span<T> initialSpan)
        {
            _span = initialSpan;
            _arrayFromPool = null;
            _pos = 0;
        }

        public int Length
        {
            get => _pos;
            set
            {
                Debug.Assert(value >= 0);
                Debug.Assert(value <= _span.Length);
                _pos = value;
            }
        }

        public ref T this[int index]
        {
            get
            {
                Debug.Assert(index < _pos);
                return ref _span[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(T item)
        {
            int pos = _pos;

            // Workaround for https://github.com/dotnet/runtime/issues/72004
            Span<T> span = _span;
            if ((uint)pos < (uint)span.Length)
            {
                span[pos] = item;
                _pos = pos + 1;
            }
            else
            {
                AddWithResize(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(scoped ReadOnlySpan<T> source)
        {
            int pos = _pos;
            Span<T> span = _span;
            if (source.Length == 1 && (uint)pos < (uint)span.Length)
            {
                span[pos] = source[0];
                _pos = pos + 1;
            }
            else
            {
                AppendMultiChar(source);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AppendMultiChar(scoped ReadOnlySpan<T> source)
        {
            if ((uint)(_pos + source.Length) > (uint)_span.Length)
            {
                Grow(_span.Length - _pos + source.Length);
            }

            source.CopyTo(_span.Slice(_pos));
            _pos += source.Length;
        }

        public void Insert(int index, scoped ReadOnlySpan<T> source)
        {
            Debug.Assert(index >= 0 && index <= _pos);

            if ((uint)(_pos + source.Length) > (uint)_span.Length)
            {
                Grow(source.Length);
            }

            _span.Slice(0, _pos).CopyTo(_span.Slice(source.Length));
            source.CopyTo(_span);
            _pos += source.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AppendSpan(int length)
        {
            Debug.Assert(length >= 0);

            int pos = _pos;
            Span<T> span = _span;
            if ((ulong)(uint)pos + (ulong)(uint)length <= (ulong)(uint)span.Length) // same guard condition as in Span<T>.Slice on 64-bit
            {
                _pos = pos + length;
                return span.Slice(pos, length);
            }
            else
            {
                return AppendSpanWithGrow(length);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Span<T> AppendSpanWithGrow(int length)
        {
            int pos = _pos;
            Grow(_span.Length - pos + length);
            _pos += length;
            return _span.Slice(pos, length);
        }

        // Hide uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddWithResize(T item)
        {
            Debug.Assert(_pos == _span.Length);
            int pos = _pos;
            Grow(1);
            _span[pos] = item;
            _pos = pos + 1;
        }

        public ReadOnlySpan<T> AsSpan()
        {
            return _span.Slice(0, _pos);
        }

        public bool TryCopyTo(Span<T> destination, out int itemsWritten)
        {
            if (_span.Slice(0, _pos).TryCopyTo(destination))
            {
                itemsWritten = _pos;
                return true;
            }

            itemsWritten = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            T[]? toReturn = _arrayFromPool;
            if (toReturn != null)
            {
                _arrayFromPool = null;
                ArrayPool<T>.Shared.Return(toReturn);
            }
        }

        // Note that consuming implementations depend on the list only growing if it's absolutely
        // required.  If the list is already large enough to hold the additional items be added,
        // it must not grow. The list is used in a number of places where the reference is checked
        // and it's expected to match the initial reference provided to the constructor if that
        // span was sufficiently large.
        private void Grow(int additionalCapacityRequired = 1)
        {
            const int ArrayMaxLength = 0x7FFFFFC7; // same as Array.MaxLength

            // Double the size of the span.  If it's currently empty, default to size 4,
            // although it'll be increased in Rent to the pool's minimum bucket size.
            int nextCapacity = Math.Max(_span.Length != 0 ? _span.Length * 2 : 4, _span.Length + additionalCapacityRequired);

            // If the computed doubled capacity exceeds the possible length of an array, then we
            // want to downgrade to either the maximum array length if that's large enough to hold
            // an additional item, or the current length + 1 if it's larger than the max length, in
            // which case it'll result in an OOM when calling Rent below.  In the exceedingly rare
            // case where _span.Length is already int.MaxValue (in which case it couldn't be a managed
            // array), just use that same value again and let it OOM in Rent as well.
            if ((uint)nextCapacity > ArrayMaxLength)
            {
                nextCapacity = Math.Max(Math.Max(_span.Length + 1, ArrayMaxLength), _span.Length);
            }

            T[] array = ArrayPool<T>.Shared.Rent(nextCapacity);
            _span.CopyTo(array);

            T[]? toReturn = _arrayFromPool;
            _span = _arrayFromPool = array;
            if (toReturn != null)
            {
                ArrayPool<T>.Shared.Return(toReturn);
            }
        }
    }
}
