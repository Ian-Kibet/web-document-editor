namespace DocumentEditor.Engine.Model;

public static class IdGen
{
    public static string Next() => Guid.NewGuid().ToString("N")[..12];
}
