// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class TextLineExtensions
    {
        public static string GetLeadingWhitespace(this TextLine line)
        {
            var linePosition = line.GetFirstNonWhitespacePosition();
            if (!linePosition.HasValue)
            {
                return line.ToString();
            }

            var lineText = line.ToString();
            return lineText.Substring(0, linePosition.Value - line.Start);
        }

        public static int? GetFirstNonWhitespacePosition(this TextLine line)
        {
            var firstNonWhitespaceOffset = line.GetFirstNonWhitespaceOffset();

            return firstNonWhitespaceOffset.HasValue
                ? firstNonWhitespaceOffset + line.Start
                : null;
        }

        public static int? GetFirstNonWhitespaceOffset(this TextLine line)
        {
            var lineString = line.ToString();
            for (var i = 0; i < lineString.Length; i++)
            {
                if (!char.IsWhiteSpace(lineString[i]))
                {
                    return i;
                }
            }

            return null;
        }
    }
}
