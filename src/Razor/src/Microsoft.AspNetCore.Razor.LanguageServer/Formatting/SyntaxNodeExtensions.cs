// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal static class SyntaxNodeExtensions
    {
        public static LinePositionSpan GetLinePositionSpan(this SyntaxNode node, RazorSourceDocument source)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var start = node.Position;
            var end = node.EndPosition;

            Debug.Assert(start <= source.Length && end <= source.Length, "Node position exceeds source length.");

            if (start == source.Length && node.FullWidth == 0)
            {
                // Marker symbol at the end of the document.
                var location = node.GetSourceLocation(source);
                var position = GetLinePosition(location);
                return new LinePositionSpan(position, position);
            }

            var startLocation = source.Lines.GetLocation(start);
            var endLocation = source.Lines.GetLocation(end);

            return new LinePositionSpan(GetLinePosition(startLocation), GetLinePosition(endLocation));

            static LinePosition GetLinePosition(SourceLocation location)
            {
                return new LinePosition(location.LineIndex, location.CharacterIndex);
            }
        }
    }
}
