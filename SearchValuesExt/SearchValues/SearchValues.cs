// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers
{
    /// <summary>
    /// Provides a set of initialization methods for instances of the <see cref="SearchValues{T}"/> class.
    /// </summary>
    /// <remarks>
    /// SearchValues are optimized for situations where the same set of values is frequently used for searching at runtime.
    /// </remarks>
    public static class SearchValuesExt
    {
        /// <summary>
        /// Creates an optimized representation of <paramref name="values"/> used for efficient searching.
        /// <para>Only <see cref="StringComparison.Ordinal"/> or <see cref="StringComparison.OrdinalIgnoreCase"/> may be used.</para>
        /// </summary>
        /// <param name="values">The set of values.</param>
        /// <param name="comparisonType">Specifies whether to use <see cref="StringComparison.Ordinal"/> or <see cref="StringComparison.OrdinalIgnoreCase"/> search semantics.</param>
        public static SearchValuesExt<string> Create(ReadOnlySpan<string> values, StringComparison comparisonType)
        {
            if (comparisonType is not (StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("No", nameof(comparisonType));
            }

            return StringSearchValues.Create(values, ignoreCase: comparisonType == StringComparison.OrdinalIgnoreCase);
        }

        internal interface IRuntimeConst
        {
            static abstract bool Value { get; }
        }

        internal readonly struct TrueConst : IRuntimeConst
        {
            public static bool Value => true;
        }

        internal readonly struct FalseConst : IRuntimeConst
        {
            public static bool Value => false;
        }
    }
}
