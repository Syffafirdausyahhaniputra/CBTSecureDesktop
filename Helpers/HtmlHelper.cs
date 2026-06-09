using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CBTSecureDesktop.Helpers
{
    public static class HtmlHelper
    {
        private static readonly Regex HtmlTokenRegex = new(@"(?s)<[^>]+>|[^<]+", RegexOptions.Compiled);
        private static readonly Regex TagNameRegex = new(@"^/?\s*([a-zA-Z0-9]+)", RegexOptions.Compiled);

        /// <summary>
        /// Mengonversi teks raw HTML dari WYSIWYG editor menjadi Plain Text murni.
        /// </summary>
        /// <param name="rawHtml">String HTML dari database</param>
        /// <returns>Plain text yang sudah dibersihkan</returns>
        public static string ConvertToPlainText(string rawHtml)
        {
            if (string.IsNullOrWhiteSpace(rawHtml))
            {
                return string.Empty;
            }

            string result = rawHtml;

            result = Regex.Replace(result, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"</(div|p|li|blockquote|pre)>", "\n", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"</?(ul|ol)>", "\n", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"<[^>]+>", string.Empty);
            result = WebUtility.HtmlDecode(result);
            result = result.Replace("\r\n", "\n").Replace("\r", "\n");
            result = Regex.Replace(result, @"(\n\s*){3,}", "\n\n");

            return result.Trim();
        }

        /// <summary>
        /// Creates a TextBlock that can render a limited HTML subset used by the exam editor.
        /// Supported tags: p, br, strong, em, u, s, blockquote, pre, span style="color: ...", ol, ul, li.
        /// </summary>
        public static TextBlock CreateFormattedTextBlock(string rawHtml, Brush? foreground = null, double fontSize = 16, TextWrapping textWrapping = TextWrapping.Wrap)
        {
            var textBlock = new TextBlock
            {
                TextWrapping = textWrapping,
                FontSize = fontSize,
                Foreground = foreground ?? new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };

            ApplyFormattedHtml(textBlock, rawHtml);
            return textBlock;
        }

        public static void ApplyFormattedHtml(TextBlock target, string rawHtml)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            target.Inlines.Clear();

            if (string.IsNullOrWhiteSpace(rawHtml))
            {
                return;
            }

            target.Inlines.Add(BuildInlineTree(rawHtml));
        }

        private static Span BuildInlineTree(string rawHtml)
        {
            var root = new Span();
            var spanStack = new Stack<Span>();
            var listStack = new Stack<ListContext>();
            bool inPre = false;

            spanStack.Push(root);

            foreach (Match match in HtmlTokenRegex.Matches(rawHtml))
            {
                string token = match.Value;
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (token.StartsWith("<", StringComparison.Ordinal))
                {
                    HandleTag(token, spanStack, listStack, ref inPre);
                }
                else
                {
                    AddText(token, spanStack.Peek(), inPre);
                }
            }

            TrimTrailingLineBreak(root);
            return root;
        }

        private static void HandleTag(string token, Stack<Span> spanStack, Stack<ListContext> listStack, ref bool inPre)
        {
            if (IsCommentToken(token))
            {
                return;
            }

            ParseTag(token, out var tagName, out var isClosing, out var isSelfClosing, out var attributes);
            if (string.IsNullOrEmpty(tagName))
            {
                return;
            }

            if (isSelfClosing || tagName is "br" or "hr")
            {
                if (!isClosing)
                {
                    AddLineBreak(spanStack.Peek());
                }
                return;
            }

            if (isClosing)
            {
                switch (tagName)
                {
                    case "p":
                    case "div":
                    case "section":
                    case "article":
                    case "blockquote":
                    case "pre":
                    case "li":
                        AddLineBreak(spanStack.Peek());
                        break;
                }

                if (tagName is "strong" or "b" or "em" or "i" or "u" or "s" or "strike" or "del" or "span" or "pre" or "blockquote")
                {
                    PopSpan(spanStack);
                }

                if (tagName == "ol" || tagName == "ul")
                {
                    if (listStack.Count > 0)
                    {
                        listStack.Pop();
                    }
                }

                if (tagName == "pre")
                {
                    inPre = false;
                }

                return;
            }

            switch (tagName)
            {
                case "p":
                case "div":
                case "section":
                case "article":
                    AddBlockBreak(spanStack.Peek());
                    break;

                case "blockquote":
                {
                    AddBlockBreak(spanStack.Peek());
                    var blockquote = new Span
                    {
                        FontStyle = FontStyles.Italic,
                        Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
                    };
                    spanStack.Peek().Inlines.Add(blockquote);
                    spanStack.Push(blockquote);
                    break;
                }

                case "strong":
                case "b":
                    PushStyledSpan(spanStack, FontWeights.Bold, null, null, null);
                    break;

                case "em":
                case "i":
                    PushStyledSpan(spanStack, null, FontStyles.Italic, null, null);
                    break;

                case "u":
                    PushStyledSpan(spanStack, null, null, TextDecorations.Underline, null);
                    break;

                case "s":
                case "strike":
                case "del":
                    PushStyledSpan(spanStack, null, null, TextDecorations.Strikethrough, null);
                    break;

                case "span":
                {
                    var color = TryGetColorFromStyle(attributes);
                    var styledSpan = CreateStyledSpan(null, null, null, color);
                    spanStack.Peek().Inlines.Add(styledSpan);
                    spanStack.Push(styledSpan);
                    break;
                }

                case "pre":
                {
                    AddBlockBreak(spanStack.Peek());
                    var preSpan = new Span
                    {
                        FontFamily = new FontFamily("Consolas")
                    };
                    spanStack.Peek().Inlines.Add(preSpan);
                    spanStack.Push(preSpan);
                    inPre = true;
                    break;
                }

                case "ol":
                    AddBlockBreak(spanStack.Peek());
                    listStack.Push(new ListContext(true));
                    break;

                case "ul":
                    AddBlockBreak(spanStack.Peek());
                    listStack.Push(new ListContext(false));
                    break;

                case "li":
                    AddListItemPrefix(spanStack.Peek(), listStack);
                    break;
            }
        }

        private static void AddText(string token, Span current, bool inPre)
        {
            string decoded = WebUtility.HtmlDecode(token);
            if (string.IsNullOrEmpty(decoded))
            {
                return;
            }

            if (inPre)
            {
                current.Inlines.Add(new Run(decoded));
                return;
            }

            decoded = decoded.Replace("\r\n", "\n").Replace("\r", "\n");
            decoded = Regex.Replace(decoded, @"\s+", " ");

            if (string.IsNullOrWhiteSpace(decoded))
            {
                return;
            }

            current.Inlines.Add(new Run(decoded));
        }

        private static void AddListItemPrefix(Span current, Stack<ListContext> listStack)
        {
            AddBlockBreak(current);

            if (listStack.Count == 0)
            {
                current.Inlines.Add(new Run("• "));
                return;
            }

            var context = listStack.Peek();
            string prefix = context.Ordered ? $"{context.NextIndex++}. " : "• ";
            if (listStack.Count > 1)
            {
                prefix = new string(' ', (listStack.Count - 1) * 2) + prefix;
            }

            current.Inlines.Add(new Run(prefix));
        }

        private static void PushStyledSpan(Stack<Span> spanStack, FontWeight? fontWeight, FontStyle? fontStyle, TextDecorationCollection? textDecorations, Brush? foreground)
        {
            var span = CreateStyledSpan(fontWeight, fontStyle, textDecorations, foreground);
            spanStack.Peek().Inlines.Add(span);
            spanStack.Push(span);
        }

        private static Span CreateStyledSpan(FontWeight? fontWeight, FontStyle? fontStyle, TextDecorationCollection? textDecorations, Brush? foreground)
        {
            var span = new Span();
            if (fontWeight.HasValue)
            {
                span.FontWeight = fontWeight.Value;
            }

            if (fontStyle.HasValue)
            {
                span.FontStyle = fontStyle.Value;
            }

            if (textDecorations != null)
            {
                span.TextDecorations = textDecorations;
            }

            if (foreground != null)
            {
                span.Foreground = foreground;
            }

            return span;
        }

        private static void AddBlockBreak(Span current)
        {
            if (HasContent(current) && !EndsWithLineBreak(current))
            {
                current.Inlines.Add(new LineBreak());
            }
        }

        private static void AddLineBreak(Span current)
        {
            if (!EndsWithLineBreak(current))
            {
                current.Inlines.Add(new LineBreak());
            }
        }

        private static bool HasContent(Span span)
        {
            return span.Inlines.FirstInline != null;
        }

        private static bool EndsWithLineBreak(Span span)
        {
            return span.Inlines.LastInline is LineBreak;
        }

        private static void TrimTrailingLineBreak(Span root)
        {
            while (root.Inlines.LastInline is LineBreak)
            {
                root.Inlines.Remove(root.Inlines.LastInline);
            }
        }

        private static void PopSpan(Stack<Span> spanStack)
        {
            if (spanStack.Count > 1)
            {
                spanStack.Pop();
            }
        }

        private static void ParseTag(string token, out string tagName, out bool isClosing, out bool isSelfClosing, out string attributes)
        {
            string inner = token.Trim('<', '>', ' ', '\t', '\r', '\n');
            isClosing = inner.StartsWith("/", StringComparison.Ordinal);
            if (isClosing)
            {
                inner = inner[1..].TrimStart();
            }

            isSelfClosing = inner.EndsWith("/", StringComparison.Ordinal);
            if (isSelfClosing)
            {
                inner = inner[..^1].TrimEnd();
            }

            var match = TagNameRegex.Match(inner);
            tagName = match.Success ? match.Groups[1].Value.ToLowerInvariant() : string.Empty;

            int tagNameLength = match.Success ? match.Groups[1].Length : 0;
            attributes = tagNameLength > 0 && inner.Length > tagNameLength
                ? inner[tagNameLength..].Trim()
                : string.Empty;
        }

        private static bool IsCommentToken(string token)
        {
            return token.StartsWith("<!--", StringComparison.Ordinal);
        }

        private static Brush? TryGetColorFromStyle(string attributes)
        {
            if (string.IsNullOrWhiteSpace(attributes))
            {
                return null;
            }

            const string stylePrefix = "style=";
            int styleIndex = attributes.IndexOf(stylePrefix, StringComparison.OrdinalIgnoreCase);
            if (styleIndex < 0)
            {
                return null;
            }

            string styleValue = attributes[(styleIndex + stylePrefix.Length)..].TrimStart();
            string? style = null;

            if (styleValue.StartsWith("\"", StringComparison.Ordinal))
            {
                int endQuote = styleValue.IndexOf('"', 1);
                if (endQuote > 1)
                {
                    style = styleValue.Substring(1, endQuote - 1);
                }
            }
            else if (styleValue.StartsWith("'", StringComparison.Ordinal))
            {
                int endQuote = styleValue.IndexOf('\'', 1);
                if (endQuote > 1)
                {
                    style = styleValue.Substring(1, endQuote - 1);
                }
            }
            else
            {
                int end = styleValue.IndexOfAny(new[] { ' ', '>' });
                style = end > 0 ? styleValue[..end] : styleValue;
            }

            if (string.IsNullOrWhiteSpace(style))
            {
                return null;
            }

            var colorMatch = Regex.Match(style, @"color\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
            if (!colorMatch.Success)
            {
                return null;
            }

            string colorValue = colorMatch.Groups[1].Value.Trim().TrimEnd(';');
            return TryParseCssColor(colorValue);
        }

        private static Brush? TryParseCssColor(string colorValue)
        {
            if (string.IsNullOrWhiteSpace(colorValue))
            {
                return null;
            }

            colorValue = colorValue.Trim();

            if (colorValue.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            {
                var rgbMatch = Regex.Match(colorValue, @"rgb\s*\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)", RegexOptions.IgnoreCase);
                if (rgbMatch.Success)
                {
                    byte r = byte.Parse(rgbMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    byte g = byte.Parse(rgbMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                    byte b = byte.Parse(rgbMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                    return new SolidColorBrush(Color.FromRgb(r, g, b));
                }
            }

            if (colorValue.StartsWith("#", StringComparison.Ordinal))
            {
                try
                {
                    return (Brush)new BrushConverter().ConvertFromString(colorValue)!;
                }
                catch
                {
                    return null;
                }
            }

            try
            {
                return (Brush)new BrushConverter().ConvertFromString(colorValue)!;
            }
            catch
            {
                return null;
            }
        }

        private sealed class ListContext
        {
            public ListContext(bool ordered)
            {
                Ordered = ordered;
            }

            public bool Ordered { get; }
            public int NextIndex { get; set; } = 1;
        }
    }
}
