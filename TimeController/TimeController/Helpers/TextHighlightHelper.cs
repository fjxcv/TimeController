using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace TimeController.Helpers
{
    public static class TextHighlightHelper
    {
        private static readonly string[] Keywords = new[]
        {
            "完成率","推迟","任务分配","计划","目标","平衡","优化"
        };

        public static FlowDocument CreateHighlightedDocument(string text, Brush highlightBrush)
        {
            var doc = new FlowDocument();
            doc.PagePadding = new System.Windows.Thickness(0);
            var paragraph = new Paragraph();
            doc.Blocks.Add(paragraph);

            int index = 0;
            while (index < text.Length)
            {
                // find next keyword occurrence
                var next = FindNextKeyword(text, index, out string keyword);
                if (next < 0)
                {
                    paragraph.Inlines.Add(new Run(text.Substring(index)));
                    break;
                }

                if (next > index)
                {
                    paragraph.Inlines.Add(new Run(text.Substring(index, next - index)));
                }

                var run = new Run(keyword)
                {
                    Foreground = highlightBrush,
                    FontWeight = FontWeights.SemiBold
                };
                paragraph.Inlines.Add(run);
                index = next + keyword.Length;
            }

            return doc;
        }

        private static int FindNextKeyword(string text, int startIndex, out string found)
        {
            int pos = -1;
            found = null;
            foreach (var k in Keywords)
            {
                var i = text.IndexOf(k, startIndex);
                if (i >= 0 && (pos == -1 || i < pos))
                {
                    pos = i;
                    found = k;
                }
            }
            return pos;
        }
    }
}