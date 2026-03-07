using DocumentEditor.Engine.Model.Enums;

namespace DocumentEditor.Engine.Model;

/// <summary>Maps to w:drawing (inline image) inside a w:r</summary>
public sealed class ImageContent : TextContent
{
    public override string ContentType => "image";

    /// <summary>Base64-encoded image bytes</summary>
    public string ImageData { get; set; } = "";

    /// <summary>MIME type (e.g. "image/png", "image/jpeg")</summary>
    public string ContentMimeType { get; set; } = "image/png";

    /// <summary>Width in EMU (914400 EMU = 1 inch)</summary>
    public long WidthEmu { get; set; }

    /// <summary>Height in EMU (914400 EMU = 1 inch)</summary>
    public long HeightEmu { get; set; }

    /// <summary>Alt text for accessibility (from wp:docPr descr attribute)</summary>
    public string? AltText { get; set; }

    /// <summary>Image name (from wp:docPr name attribute)</summary>
    public string? Name { get; set; }

    /// <summary>Text wrapping mode (only meaningful for wp:anchor images)</summary>
    public ImageWrapMode WrapMode { get; set; } = ImageWrapMode.Inline;

    /// <summary>Horizontal position offset in EMU (from wp:positionH/wp:posOffset)</summary>
    public long? HorizontalOffsetEmu { get; set; }

    /// <summary>Vertical position offset in EMU (from wp:positionV/wp:posOffset)</summary>
    public long? VerticalOffsetEmu { get; set; }

    /// <summary>Distance from left edge of text in EMU (from wp:anchor @distL)</summary>
    public long? DistLeftEmu { get; set; }

    /// <summary>Distance from right edge of text in EMU (from wp:anchor @distR)</summary>
    public long? DistRightEmu { get; set; }

    /// <summary>Distance from top edge of text in EMU (from wp:anchor @distT)</summary>
    public long? DistTopEmu { get; set; }

    /// <summary>Distance from bottom edge of text in EMU (from wp:anchor @distB)</summary>
    public long? DistBottomEmu { get; set; }

    /// <summary>Clockwise rotation in degrees (OOXML: a:xfrm/@rot / 60000)</summary>
    public double RotationDegrees { get; set; } = 0;

    public ImageContent DeepClone() => new()
    {
        ImageData = ImageData,
        ContentMimeType = ContentMimeType,
        WidthEmu = WidthEmu,
        HeightEmu = HeightEmu,
        AltText = AltText,
        Name = Name,
        WrapMode = WrapMode,
        HorizontalOffsetEmu = HorizontalOffsetEmu,
        VerticalOffsetEmu = VerticalOffsetEmu,
        DistLeftEmu = DistLeftEmu,
        DistRightEmu = DistRightEmu,
        DistTopEmu = DistTopEmu,
        DistBottomEmu = DistBottomEmu,
        RotationDegrees = RotationDegrees,
    };
}
