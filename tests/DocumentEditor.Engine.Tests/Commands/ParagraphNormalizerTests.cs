using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;

namespace DocumentEditor.Engine.Tests.Commands;

public class ParagraphNormalizerTests
{
    [Fact]
    public void Normalize_MergesAdjacentRunsWithSameProperties()
    {
        var para = new Paragraph
        {
            Children =
            [
                DocFactory.CreateRun("Hello"),
                DocFactory.CreateRun(" World")
            ]
        };

        ParagraphNormalizer.Normalize(para);

        Assert.Single(para.Children);
        Assert.Equal("Hello World", ((Run)para.Children[0]).Text);
    }

    [Fact]
    public void Normalize_SkipsDifferentProperties()
    {
        var para = new Paragraph
        {
            Children =
            [
                DocFactory.CreateRun("Hello", new RunProperties { Bold = true }),
                DocFactory.CreateRun(" World")
            ]
        };

        ParagraphNormalizer.Normalize(para);

        Assert.Equal(2, para.Children.Count);
    }

    [Fact]
    public void Normalize_RemovesEmptyRuns()
    {
        var para = new Paragraph
        {
            Children =
            [
                DocFactory.CreateRun("Hello"),
                DocFactory.CreateRun(""),
                DocFactory.CreateRun(" World")
            ]
        };

        ParagraphNormalizer.Normalize(para);

        Assert.Single(para.Children);
        Assert.Equal("Hello World", ((Run)para.Children[0]).Text);
    }

    [Fact]
    public void Normalize_KeepsOnlyEmptyRun()
    {
        var para = new Paragraph
        {
            Children = [DocFactory.CreateRun("")]
        };

        ParagraphNormalizer.Normalize(para);

        Assert.Single(para.Children);
        Assert.Equal("", ((Run)para.Children[0]).Text);
    }

    [Fact]
    public void Normalize_AddsEmptyRunWhenEmpty()
    {
        var para = new Paragraph { Children = [] };

        ParagraphNormalizer.Normalize(para);

        Assert.Single(para.Children);
        Assert.IsType<Run>(para.Children[0]);
    }

    [Fact]
    public void Normalize_SkipsHyperlinks()
    {
        var para = new Paragraph
        {
            Children =
            [
                DocFactory.CreateRun("Before "),
                DocFactory.CreateHyperlink("http://example.com", "link"),
                DocFactory.CreateRun(" after")
            ]
        };

        ParagraphNormalizer.Normalize(para);

        Assert.Equal(3, para.Children.Count);
        Assert.IsType<Run>(para.Children[0]);
        Assert.IsType<Hyperlink>(para.Children[1]);
        Assert.IsType<Run>(para.Children[2]);
    }

    [Fact]
    public void Normalize_MergesMultipleAdjacentRuns()
    {
        var para = new Paragraph
        {
            Children =
            [
                DocFactory.CreateRun("A"),
                DocFactory.CreateRun("B"),
                DocFactory.CreateRun("C")
            ]
        };

        ParagraphNormalizer.Normalize(para);

        Assert.Single(para.Children);
        Assert.Equal("ABC", ((Run)para.Children[0]).Text);
    }
}
