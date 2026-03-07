using DocumentEditor.Engine.Model;

namespace DocumentEditor.Engine.History;

public static class DocumentCloner
{
    public static DocxDocument Clone(DocxDocument doc) => doc.DeepClone();
}
