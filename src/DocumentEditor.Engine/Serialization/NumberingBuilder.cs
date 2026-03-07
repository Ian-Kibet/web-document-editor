using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocumentEditor.Engine.Serialization;

public static class NumberingBuilder
{
    public static void AddNumberingPart(MainDocumentPart mainPart)
    {
        var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
        var numbering = new Numbering();

        // Abstract numbering 0: Bullet list (3 levels)
        numbering.Append(CreateBulletAbstractNum());

        // Abstract numbering 1: Numbered list (3 levels)
        numbering.Append(CreateNumberedAbstractNum());

        // Concrete numbering instances
        // NumId 1 → bullet (abstract 0)
        numbering.Append(new NumberingInstance(
            new AbstractNumId { Val = 0 }
        )
        { NumberID = 1 });

        // NumId 2 → numbered (abstract 1)
        numbering.Append(new NumberingInstance(
            new AbstractNumId { Val = 0 }
        )
        { NumberID = 2 });
        // Fix: NumId 2 should reference abstract 1
        var numInst2 = numbering.Elements<NumberingInstance>().Last();
        numInst2.Elements<AbstractNumId>().First().Val = 1;

        numberingPart.Numbering = numbering;
    }

    private static AbstractNum CreateBulletAbstractNum()
    {
        var abstractNum = new AbstractNum { AbstractNumberId = 0 };
        abstractNum.Append(new MultiLevelType { Val = MultiLevelValues.HybridMultilevel });

        // Level 0: bullet (•)
        abstractNum.Append(CreateBulletLevel(0, "\u2022", "Symbol", 720, 360));
        // Level 1: circle (○)
        abstractNum.Append(CreateBulletLevel(1, "o", "Courier New", 1440, 360));
        // Level 2: square (■)
        abstractNum.Append(CreateBulletLevel(2, "\u25A0", "Wingdings", 2160, 360));

        return abstractNum;
    }

    private static AbstractNum CreateNumberedAbstractNum()
    {
        var abstractNum = new AbstractNum { AbstractNumberId = 1 };
        abstractNum.Append(new MultiLevelType { Val = MultiLevelValues.HybridMultilevel });

        // Level 0: 1. 2. 3.
        abstractNum.Append(CreateNumberedLevel(0, NumberFormatValues.Decimal, "%1.", 720, 360));
        // Level 1: a. b. c.
        abstractNum.Append(CreateNumberedLevel(1, NumberFormatValues.LowerLetter, "%2.", 1440, 360));
        // Level 2: i. ii. iii.
        abstractNum.Append(CreateNumberedLevel(2, NumberFormatValues.LowerRoman, "%3.", 2160, 360));

        return abstractNum;
    }

    private static Level CreateBulletLevel(int level, string bulletChar, string fontName, int indent, int hanging)
    {
        var lvl = new Level { LevelIndex = level };
        lvl.Append(new StartNumberingValue { Val = 1 });
        lvl.Append(new NumberingFormat { Val = NumberFormatValues.Bullet });
        lvl.Append(new LevelText { Val = bulletChar });
        lvl.Append(new LevelJustification { Val = LevelJustificationValues.Left });

        var pPr = new PreviousParagraphProperties();
        pPr.Append(new Indentation { Left = indent.ToString(), Hanging = hanging.ToString() });
        lvl.Append(pPr);

        var rPr = new NumberingSymbolRunProperties();
        rPr.Append(new RunFonts { Ascii = fontName, HighAnsi = fontName, Hint = FontTypeHintValues.Default });
        lvl.Append(rPr);

        return lvl;
    }

    private static Level CreateNumberedLevel(int level, NumberFormatValues format, string text, int indent, int hanging)
    {
        var lvl = new Level { LevelIndex = level };
        lvl.Append(new StartNumberingValue { Val = 1 });
        lvl.Append(new NumberingFormat { Val = format });
        lvl.Append(new LevelText { Val = text });
        lvl.Append(new LevelJustification { Val = LevelJustificationValues.Left });

        var pPr = new PreviousParagraphProperties();
        pPr.Append(new Indentation { Left = indent.ToString(), Hanging = hanging.ToString() });
        lvl.Append(pPr);

        return lvl;
    }
}
