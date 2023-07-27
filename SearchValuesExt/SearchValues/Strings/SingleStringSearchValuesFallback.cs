// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal sealed class SingleStringSearchValuesFallback<TIgnoreCase> : StringSearchValuesBase
        where TIgnoreCase : struct, SearchValuesExt.IRuntimeConst
    {
        private readonly string _value;

        public SingleStringSearchValuesFallback(string value, HashSet<string> uniqueValues) : base(uniqueValues)
        {
            _value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int IndexOfAnyMultiString(ReadOnlySpan<char> span) =>
            TIgnoreCase.Value
                ? span.IndexOf(_value, StringComparison.OrdinalIgnoreCase)
                : span.IndexOf(_value);
    }
}
