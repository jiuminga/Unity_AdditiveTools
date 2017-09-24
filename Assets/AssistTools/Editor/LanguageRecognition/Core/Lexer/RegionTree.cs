using System.Collections.Generic;

public class RegionTree
{
    public enum Kind
    {
        None,
        Region,
        If,
        Elif,
        Else,

        LastActive,

        InactiveRegion,
        InactiveIf,
        InactiveElif,
        InactiveElse
    }
    public Kind kind;
    public FormatedLine line;
    public RegionTree parent;
    public List<RegionTree> children;
}