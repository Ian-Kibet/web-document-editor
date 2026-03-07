using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Properties;

namespace DocumentEditor.Engine.Model;

/// <summary>
/// Computes section boundaries from the flat block list.
/// Section breaks live on ParagraphProperties.SectionBreak; the final section
/// uses DocumentProperties for its page geometry.
/// </summary>
public static class SectionResolver
{
    public static List<SectionInfo> GetSections(DocxDocument document)
    {
        var sections = new List<SectionInfo>();
        var startIndex = 0;

        for (var i = 0; i < document.Children.Count; i++)
        {
            if (document.Children[i] is Paragraph para && para.Properties.SectionBreak is not null)
            {
                var sp = para.Properties.SectionBreak;
                sections.Add(new SectionInfo
                {
                    StartBlockIndex = startIndex,
                    EndBlockIndex = i,
                    Properties = sp
                });
                startIndex = i + 1;
            }
        }

        // Final section — uses document-level properties
        sections.Add(new SectionInfo
        {
            StartBlockIndex = startIndex,
            EndBlockIndex = document.Children.Count - 1,
            Properties = new SectionProperties
            {
                BreakType = SectionBreakType.NextPage,
                Orientation = document.Properties.Orientation,
                PageWidth = document.Properties.PageWidth,
                PageHeight = document.Properties.PageHeight,
                MarginTop = document.Properties.MarginTop,
                MarginBottom = document.Properties.MarginBottom,
                MarginLeft = document.Properties.MarginLeft,
                MarginRight = document.Properties.MarginRight,
                HeaderDistance = document.Properties.HeaderDistance,
                FooterDistance = document.Properties.FooterDistance,
                TitlePage = document.Properties.TitlePage,
                Headers = document.Properties.Headers,
                Footers = document.Properties.Footers,
                ColumnCount = document.Properties.ColumnCount,
                ColumnSpacing = document.Properties.ColumnSpacing,
                ColumnSeparator = document.Properties.ColumnSeparator,
                HeaderFooterParts = document.Properties.HeaderFooterParts
            }
        });

        // Forward pass: inherit headers/footers from previous section (OOXML spec behavior).
        // If a mid-document section's w:sectPr doesn't define its own header/footer references,
        // it inherits from the previous section.
        for (var i = 1; i < sections.Count; i++)
        {
            var prev = sections[i - 1].Properties;
            var curr = sections[i].Properties;
            curr.Headers ??= prev.Headers;
            curr.Footers ??= prev.Footers;
            curr.HeaderFooterParts ??= prev.HeaderFooterParts;
        }

        // Document-level fallback: fills any sections still missing headers/footers.
        // Handles common case where only the body-level sectPr defines them.
        var docHeaders = document.Properties.Headers;
        var docFooters = document.Properties.Footers;
        var docHFParts = document.Properties.HeaderFooterParts;
        for (var i = 0; i < sections.Count; i++)
        {
            var curr = sections[i].Properties;
            curr.Headers ??= docHeaders;
            curr.Footers ??= docFooters;
            curr.HeaderFooterParts ??= docHFParts;
        }

        return sections;
    }

    /// <summary>
    /// Find which section a given block index belongs to.
    /// </summary>
    public static int GetSectionIndex(List<SectionInfo> sections, int blockIndex)
    {
        for (var i = 0; i < sections.Count; i++)
        {
            if (blockIndex >= sections[i].StartBlockIndex && blockIndex <= sections[i].EndBlockIndex)
                return i;
        }
        return sections.Count - 1;
    }
}

public class SectionInfo
{
    public int StartBlockIndex { get; set; }
    public int EndBlockIndex { get; set; }
    public SectionProperties Properties { get; set; } = new();
}
