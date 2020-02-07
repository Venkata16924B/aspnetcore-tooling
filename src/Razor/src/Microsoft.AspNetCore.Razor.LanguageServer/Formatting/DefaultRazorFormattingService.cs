// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class DefaultRazorFormattingService : RazorFormattingService
    {
        private readonly ILanguageServer _server;
        private readonly CSharpFormatter _cSharpFormatter;
        private readonly HtmlFormatter _htmlFormatter;
        private readonly ILogger _logger;

        public DefaultRazorFormattingService(
            RazorDocumentMappingService documentMappingService,
            FilePathNormalizer filePathNormalizer,
            ILanguageServer server,
            ILoggerFactory loggerFactory)
        {
            if (documentMappingService is null)
            {
                throw new ArgumentNullException(nameof(documentMappingService));
            }

            if (filePathNormalizer is null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            if (server is null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _server = server;
            _cSharpFormatter = new CSharpFormatter(documentMappingService, server, filePathNormalizer);
            _htmlFormatter = new HtmlFormatter(server, filePathNormalizer);
            _logger = loggerFactory.CreateLogger<DefaultRazorFormattingService>();
        }

        public override async Task<TextEdit[]> FormatAsync(Uri uri, RazorCodeDocument codeDocument, Range range, FormattingOptions options)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var formattingContext = CreateFormattingContext(uri, codeDocument, range, options);

            var edits = await FormatCodeBlockDirectivesAsync(formattingContext);
            return edits;
        }

        internal static FormattingContext CreateFormattingContext(Uri uri, RazorCodeDocument codedocument, Range range, FormattingOptions options)
        {
            var result = new FormattingContext()
            {
                Uri = uri,
                CodeDocument = codedocument,
                Range = range,
                Options = options
            };

            var source = codedocument.Source;
            var syntaxTree = codedocument.GetSyntaxTree();
            var formattingSpans = syntaxTree.GetFormattingSpans();

            var total = 0;
            var previousIndentationLevel = 0;
            for (var i = 0; i < source.Lines.Count; i++)
            {
                // Get first non-whitespace character position
                var lineLength = source.Lines.GetLineLength(i);
                var nonWsChar = 0;
                for (var j = 0; j < lineLength; j++)
                {
                    var ch = source[total + j];
                    if (!char.IsWhiteSpace(ch) && !ParserHelpers.IsNewLine(ch))
                    {
                        nonWsChar = j;
                        break;
                    }
                }

                // position now contains the first non-whitespace character or 0. Get the corresponding FormattingSpan.
                if (TryGetFormattingSpan(total + nonWsChar, formattingSpans, out var span))
                {
                    result.Indentations[i] = new IndentationContext
                    {
                        Line = i,
                        IndentationLevel = span.IndentationLevel,
                        RelativeIndentationLevel = span.IndentationLevel - previousIndentationLevel,
                        ExistingIndentation = nonWsChar,
                        FirstSpan = span,
                    };
                    previousIndentationLevel = span.IndentationLevel;
                }
                else
                {
                    // Couldn't find a corresponding FormattingSpan.
                    result.Indentations[i] = new IndentationContext
                    {
                        Line = i,
                        IndentationLevel = -1,
                        RelativeIndentationLevel = previousIndentationLevel,
                        ExistingIndentation = nonWsChar,
                    };
                }

                total += lineLength;
            }

            return result;
        }

        internal static bool TryGetFormattingSpan(int absoluteIndex, IReadOnlyList<FormattingSpan> formattingspans, out FormattingSpan result)
        {
            result = null;
            for (var i = 0; i < formattingspans.Count; i++)
            {
                var formattingspan = formattingspans[i];
                var span = formattingspan.Span;

                if (span.Start <= absoluteIndex)
                {
                    if (span.End >= absoluteIndex)
                    {
                        if (span.End == absoluteIndex && span.Length > 0)
                        {
                            // We're at an edge.
                            // Non-marker spans (spans.length == 0) do not own the edges after it
                            continue;
                        }

                        result = formattingspan;
                        return true;
                    }
                }
            }

            return false;
        }

        private async Task<TextEdit[]> FormatCodeBlockDirectivesAsync(FormattingContext context)
        {
            var source = context.CodeDocument.Source;
            var syntaxTree = context.CodeDocument.GetSyntaxTree();
            var nodes = syntaxTree.GetCodeBlockDirectives();

            var allEdits = new List<TextEdit>();
            for (var i = nodes.Length - 1; i >= 0; i--)
            {
                if (!(nodes[i].Body is RazorDirectiveBodySyntax directiveBody))
                {
                    continue;
                }

                var node = directiveBody.CSharpCode.DescendantNodes().FirstOrDefault(n => n is CSharpCodeBlockSyntax);
                if (node == null)
                {
                    // Nothing to indent.
                    continue;
                }

                if (node.DescendantNodes().Any(n => n is MarkupBlockSyntax))
                {
                    // We currently don't support formatting code block directives with markup.
                    continue;
                }

                var lineSpan = node.GetLinePositionSpan(source);
                var codeBlockRange = new Range(
                    new Position(lineSpan.Start.Line, lineSpan.Start.Character),
                    new Position(lineSpan.End.Line, lineSpan.End.Character));

                if (!codeBlockRange.OverlapsWith(context.Range))
                {
                    // This code block directive doesn't fall under the selected range.
                    continue;
                }

                var actualRange = codeBlockRange.Overlap(context.Range);
                var edits = await _cSharpFormatter.FormatAsync(context.CodeDocument, actualRange, context.Uri, context.Options);
                edits = TransformEdits(context, codeBlockRange, edits);
                allEdits.AddRange(edits);
            }

            return allEdits.ToArray();
        }

        private TextEdit[] TransformEdits(FormattingContext context, Range codeBlockRange, TextEdit[] edits)
        {
            if (edits.Length == 0)
            {
                return Array.Empty<TextEdit>();
            }

            var sourceText = context.CodeDocument.GetSourceText();
            var currentIndentation = context.Indentations[(int)codeBlockRange.Start.Line].IndentationLevel;
            var originalCodeBlockSpan = codeBlockRange.AsTextSpan(sourceText);
            var inputRange = context.Range;
            var inputSpan = inputRange.AsTextSpan(sourceText);
            var changes = edits.Select(e => e.AsTextChange(sourceText));
            var changedText = sourceText.WithChanges(changes);
            var affectedRange = changedText.GetEncompassingTextChangeRange(sourceText);
            var changedCodeBlockSpan = TextSpan.FromBounds(originalCodeBlockSpan.Start, originalCodeBlockSpan.End + affectedRange.NewLength - affectedRange.Span.Length);

            if (!originalCodeBlockSpan.Contains(affectedRange.Span))
            {
                _logger.LogDebug($"The changed region {affectedRange.Span} was not a subset of the code block {originalCodeBlockSpan}. This shouldn't happen.");
            }

            var editsToApply = new List<TextChange>();

            // we want to keep the open '@code {' on its own line. So bring everything else after it to the next line.
            var firstLine = changedText.Lines[(int)codeBlockRange.Start.Line];
            var textAfterBlockStart = firstLine.ToString().Substring(originalCodeBlockSpan.Start - firstLine.Start);
            var isBlockStartOnSeparateLine = string.IsNullOrWhiteSpace(textAfterBlockStart);
            if (!isBlockStartOnSeparateLine)
            {
                // If the first line contains code, add a newline at the beginning and indent it.
                var desiredIndentation = currentIndentation + 1;
                var span = new TextSpan(originalCodeBlockSpan.Start, length: textAfterBlockStart.Length);
                var newFirstLine = Environment.NewLine + GetIndentationString(context, desiredIndentation) + textAfterBlockStart.Trim();
                editsToApply.Add(new TextChange(span, newFirstLine));
            }

            // we want to keep the close '}' on its own line. So bring it to the next line.
            var closeCurlyLocation = changedCodeBlockSpan.End;
            var closeCurlyLine = changedText.Lines.GetLineFromPosition(closeCurlyLocation);
            var firstNonWhitespaceCharacterLocation = closeCurlyLine.GetFirstNonWhitespaceOffset() ?? 0;
            if (closeCurlyLine.Start + firstNonWhitespaceCharacterLocation != closeCurlyLocation)
            {
                var lastLineChange = new TextChange(new TextSpan(closeCurlyLocation, length: 0), Environment.NewLine);
                editsToApply.Add(lastLineChange);
            }

            var changedInputSpan = TextSpan.FromBounds(inputSpan.Start, inputSpan.End + affectedRange.NewLength - affectedRange.Span.Length);
            var overlappingSpan = changedInputSpan.Overlap(changedCodeBlockSpan);
            if (!overlappingSpan.HasValue)
            {
                return Array.Empty<TextEdit>();
            }
            var overlappingRange = overlappingSpan.Value.AsRange(changedText);
            for (var i = (int)overlappingRange.Start.Line; i <= overlappingRange.End.Line; i++)
            {
                var line = changedText.Lines[i];
                if (line.Span.Length == 0)
                {
                    continue;
                }

                var leadingWhitespace = line.GetLeadingWhitespace();
                if (!(isBlockStartOnSeparateLine && i == firstLine.LineNumber + 1) && leadingWhitespace.Length == 0)
                {
                    // For whatever reason, the C# formatter decided to not indent this. Leave it as is.
                    // Except if it is the first line of code in the block, then we should indent it
                    // because of how our SourceMappings work.
                    continue;
                }

                var desiredIndentation = currentIndentation;
                if (changedCodeBlockSpan.Contains(line.Start))
                {
                    desiredIndentation = currentIndentation + 1;
                }

                var desiredIndentationLength = (int)context.Options.TabSize * desiredIndentation;
                if (leadingWhitespace.Length < desiredIndentationLength)
                {
                    var span = new TextSpan(line.Start, length: leadingWhitespace.Length);
                    editsToApply.Add(new TextChange(span, GetIndentationString(context, desiredIndentation)));
                }
                else if (leadingWhitespace.Length > desiredIndentationLength)
                {
                    var length = Math.Min(leadingWhitespace.Length - desiredIndentationLength, desiredIndentationLength);
                    var span = new TextSpan(line.Start, length);
                    editsToApply.Add(new TextChange(span, string.Empty));
                }
            }

            changedText = changedText.WithChanges(editsToApply);

            // Once https://github.com/dotnet/roslyn/issues/41413 is fixed,
            // the following lines can be replaced with `changes = changedText.GetTextChanges(sourceText)`.
            affectedRange = changedText.GetEncompassingTextChangeRange(sourceText);
            changedCodeBlockSpan = TextSpan.FromBounds(originalCodeBlockSpan.Start, originalCodeBlockSpan.End + affectedRange.NewLength - affectedRange.Span.Length);
            var change = new TextChange(originalCodeBlockSpan, changedText.GetSubText(changedCodeBlockSpan).ToString());

            var transformedEdits = new[] { change.AsTextEdit(sourceText) };
            return transformedEdits;
        }

        private static string GetIndentationString(FormattingContext context, int indentationLevel)
        {
            var indentChar = context.Options.InsertSpaces ? ' ' : '\t';
            var indentation = new string(indentChar, (int)context.Options.TabSize * indentationLevel);
            return indentation;
        }
    }
}
