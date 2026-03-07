using DocumentFormat.OpenXml.Packaging;

namespace DocumentEditor.Engine.Serialization;

public static class RelationshipsBuilder
{
    public static string AddHyperlinkRelationship(MainDocumentPart mainPart, string url)
    {
        var uri = new Uri(url, UriKind.Absolute);
        var rel = mainPart.AddHyperlinkRelationship(uri, true);
        return rel.Id;
    }
}
