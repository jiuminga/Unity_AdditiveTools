using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FormatedLine
{
    public enum BlockState : byte
    {
        None = 0,
        CommentBlock = 1,
        StringBlock = 2,
    }

    public enum FormatedState
    {
        None,
        Lexed,
        SyntaxParsed,
        SymbolResolved,
    }

    public BlockState blockState;
    public FormatedState formatedState;

    public RegionTree regionTree;
    public int index;
    public List<LexerToken> tokens;

    public string m_sSrcLine;
    //public int lastChange = -1;
    //public int savedVersion = -1;
    //public int laLines;
}
