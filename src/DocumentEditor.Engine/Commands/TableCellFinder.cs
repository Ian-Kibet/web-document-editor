using DocumentEditor.Engine.Model;

namespace DocumentEditor.Engine.Commands;

internal static class TableCellFinder
{
    internal static TableCell? Find(DocxDocument doc, string cellId)
    {
        foreach (var block in doc.Children)
            if (block is Table table) { var f = FindInTable(table, cellId); if (f != null) return f; }
        return null;
    }

    private static TableCell? FindInTable(Table table, string cellId)
    {
        foreach (var row in table.Rows)
            foreach (var cell in row.Cells)
            {
                if (cell.Id == cellId) return cell;
                foreach (var block in cell.Children)
                    if (block is Table nested) { var f = FindInTable(nested, cellId); if (f != null) return f; }
            }
        return null;
    }
}
