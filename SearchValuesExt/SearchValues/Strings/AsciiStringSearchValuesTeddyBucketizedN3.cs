﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers
{
    internal sealed class AsciiStringSearchValuesTeddyBucketizedN3<TStartCaseSensitivity, TCaseSensitivity> : AsciiStringSearchValuesTeddyBase<SearchValuesExt.TrueConst, TStartCaseSensitivity, TCaseSensitivity>
        where TStartCaseSensitivity : struct, StringSearchValuesHelper.ICaseSensitivity
        where TCaseSensitivity : struct, StringSearchValuesHelper.ICaseSensitivity
    {
        public AsciiStringSearchValuesTeddyBucketizedN3(string[][] buckets, ReadOnlySpan<string> values, HashSet<string> uniqueValues)
            : base(buckets, values, uniqueValues, n: 3)
        { }

        //[CompExactlyDependsOn(typeof(Ssse3))]
        //[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        public override int IndexOfAnyMultiString(ReadOnlySpan<char> span) =>
            IndexOfAnyN3(span);
    }
}
