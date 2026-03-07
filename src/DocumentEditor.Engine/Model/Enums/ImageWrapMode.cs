namespace DocumentEditor.Engine.Model.Enums;

public enum ImageWrapMode
{
    Inline,         // wp:inline — flows with text
    FloatLeft,      // wp:wrapSquare/Tight/Through, wrapText="right" → image on left
    FloatRight,     // wp:wrapSquare/Tight/Through, wrapText="left"  → image on right
    TopAndBottom,   // wp:wrapTopAndBtm — text only above and below
    BehindText,     // wp:wrapNone + behindDoc=1
    InFrontOfText   // wp:wrapNone + behindDoc=0
}
