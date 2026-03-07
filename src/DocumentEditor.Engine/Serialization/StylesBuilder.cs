using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocumentEditor.Engine.Serialization;

public static class StylesBuilder
{
    public static void AddStylesPart(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        var styles = new Styles();

        // Document defaults
        styles.Append(CreateDocDefaults());

        // Normal paragraph style
        styles.Append(CreateStyle("Normal", "Normal", StyleValues.Paragraph, isDefault: true));

        // Heading styles (1-4)
        styles.Append(CreateHeadingStyle("Heading1", "Heading 1", "28", 1, keepNext: true, spaceBefore: 240, spaceAfter: 0));
        styles.Append(CreateHeadingStyle("Heading2", "Heading 2", "26", 2, keepNext: true, spaceBefore: 200, spaceAfter: 0));
        styles.Append(CreateHeadingStyle("Heading3", "Heading 3", "24", 3, keepNext: true, spaceBefore: 160, spaceAfter: 0));
        styles.Append(CreateHeadingStyle("Heading4", "Heading 4", "22", 4, keepNext: true, spaceBefore: 120, spaceAfter: 0));

        // Hyperlink character style
        styles.Append(CreateHyperlinkStyle());

        // TableGrid table style
        styles.Append(CreateTableGridStyle());

        stylesPart.Styles = styles;
    }

    private static DocDefaults CreateDocDefaults()
    {
        return new DocDefaults(
            new RunPropertiesDefault(
                new RunPropertiesBaseStyle(
                    new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", ComplexScript = "Calibri" },
                    new FontSize { Val = "22" },  // 11pt
                    new FontSizeComplexScript { Val = "22" },
                    new Languages { Val = "en-US" }
                )
            ),
            new ParagraphPropertiesDefault(
                new SpacingBetweenLines { After = "160", Line = "259", LineRule = LineSpacingRuleValues.Auto }
            )
        );
    }

    private static Style CreateStyle(string styleId, string name, StyleValues type, bool isDefault = false)
    {
        var style = new Style { Type = type, StyleId = styleId, Default = isDefault ? true : null };
        style.Append(new StyleName { Val = name });
        if (isDefault)
            style.Append(new PrimaryStyle());
        return style;
    }

    private static Style CreateHeadingStyle(string styleId, string name, string fontSize, int outlineLevel, bool keepNext, int spaceBefore, int spaceAfter)
    {
        var style = new Style { Type = StyleValues.Paragraph, StyleId = styleId };
        style.Append(new StyleName { Val = name });
        style.Append(new BasedOn { Val = "Normal" });
        style.Append(new NextParagraphStyle { Val = "Normal" });
        style.Append(new PrimaryStyle());

        var pPr = new StyleParagraphProperties();
        if (keepNext)
            pPr.Append(new KeepNext());
        pPr.Append(new SpacingBetweenLines { Before = spaceBefore.ToString(), After = spaceAfter.ToString() });
        pPr.Append(new OutlineLevel { Val = outlineLevel - 1 }); // 0-based for TOC support
        style.Append(pPr);

        var rPr = new StyleRunProperties();
        rPr.Append(new Bold());
        rPr.Append(new FontSize { Val = fontSize });
        rPr.Append(new FontSizeComplexScript { Val = fontSize });
        style.Append(rPr);

        return style;
    }

    private static Style CreateHyperlinkStyle()
    {
        var style = new Style { Type = StyleValues.Character, StyleId = "Hyperlink" };
        style.Append(new StyleName { Val = "Hyperlink" });

        var rPr = new StyleRunProperties();
        rPr.Append(new Color { Val = "0563C1", ThemeColor = ThemeColorValues.Hyperlink });
        rPr.Append(new Underline { Val = UnderlineValues.Single });
        style.Append(rPr);

        return style;
    }

    private static Style CreateTableGridStyle()
    {
        var style = new Style { Type = StyleValues.Table, StyleId = "TableGrid" };
        style.Append(new StyleName { Val = "Table Grid" });
        style.Append(new BasedOn { Val = "TableNormal" });

        var tblPr = new StyleTableProperties();
        tblPr.Append(new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" },
            new BottomBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" },
            new LeftBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" },
            new RightBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" },
            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" }
        ));
        style.Append(tblPr);

        return style;
    }
}
