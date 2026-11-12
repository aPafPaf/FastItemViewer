using System;
using System.Collections.Generic;
using ExileCore;

namespace FastItemViewer.UI;

public static class TextWrapper
{
    public static List<string> Wrap(Graphics graphics, IEnumerable<string> sourceLines, float maxWidth)
    {
        var result = new List<string>();

        if (sourceLines == null)
            return result;

        if (maxWidth <= 1f)
        {
            result.AddRange(sourceLines);
            return result;
        }

        foreach (var sourceLine in sourceLines)
        {
            if (string.IsNullOrEmpty(sourceLine))
            {
                result.Add(string.Empty);
                continue;
            }

            var words = sourceLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                result.Add(string.Empty);
                continue;
            }

            var current = string.Empty;

            foreach (var word in words)
            {
                var candidate = current.Length == 0 ? word : current + " " + word;

                if (graphics.MeasureText(candidate).X <= maxWidth)
                {
                    current = candidate;
                    continue;
                }

                if (current.Length > 0)
                {
                    result.Add(current);
                    current = string.Empty;
                }

                if (graphics.MeasureText(word).X <= maxWidth)
                {
                    current = word;
                    continue;
                }

                // Break a single very long token by characters.
                var remaining = word;
                while (remaining.Length > 0)
                {
                    var cut = remaining.Length;
                    while (cut > 1 && graphics.MeasureText(remaining[..cut]).X > maxWidth)
                        cut--;

                    result.Add(remaining[..cut]);
                    remaining = remaining[cut..];
                }
            }

            if (current.Length > 0)
                result.Add(current);
        }

        return result;
    }
}
