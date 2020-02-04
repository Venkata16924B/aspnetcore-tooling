// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class RangeExtensions
    {
        public static bool OverlapsWith(this Range range, Range other)
        {
            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var overlapStart = range.Start;
            if (range.Start.CompareTo(other.Start) < 0)
            {
                overlapStart = other.Start;
            }

            var overlapEnd = range.End;
            if (range.End.CompareTo(other.End) > 0)
            {
                overlapEnd = other.End;
            }

            // Empty ranges do not overlap with any range.
            return overlapStart.CompareTo(overlapEnd) < 0;
        }

        public static Range Overlap(this Range range, Range other)
        {
            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var overlapStart = range.Start;
            if (range.Start.CompareTo(other.Start) < 0)
            {
                overlapStart = other.Start;
            }

            var overlapEnd = range.End;
            if (range.End.CompareTo(other.End) > 0)
            {
                overlapEnd = other.End;
            }

            // Empty ranges do not overlap with any range.
            if (overlapStart.CompareTo(overlapEnd) < 0)
            {
                return new Range(overlapStart, overlapEnd);
            }

            return null;
        }
    }
}
