// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Buffers
{
    internal sealed class MultiStringIgnoreCaseSearchValuesFallback : StringSearchValuesBase
    {
        private readonly string[] _values;

        public MultiStringIgnoreCaseSearchValuesFallback(HashSet<string> uniqueValues) : base(uniqueValues)
        {
            _values = uniqueValues.ToArray();
        }

        public override int IndexOfAnyMultiString(ReadOnlySpan<char> span)
        {
            string[] values = _values;

            for (int i = 0; i < span.Length; i++)
            {
                ReadOnlySpan<char> slice = span.Slice(i);

                foreach (string value in values)
                {
                    if (slice.StartsWith(value, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}
