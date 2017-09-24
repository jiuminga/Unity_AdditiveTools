using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class TokenScanner : IEnumerator<LexerToken>
{
    public Action<LexerToken> OnTokenMoveAt;
    public LexerToken EOF;

    public int CurrentLine { get { return m_iCurrentLine; } }
    public int CurrentTokenIndex { get { return m_iCurrentTokenIndex; } }

    private LexerToken m_kCurrentTokenCache;
    private FormatedLine[] m_lsFormatedLines;
    private List<LexerToken> m_lsTokens;
    private int m_iCurrentLine = -1;
    private int m_iCurrentTokenIndex = -1;

    public TokenScanner(FormatedLine[] lines)
    {
        m_lsFormatedLines = lines;
    }

    public TokenScanner Clone()
    {
        return new TokenScanner(m_lsFormatedLines)
        {
            m_kCurrentTokenCache = m_kCurrentTokenCache,
            m_lsTokens = m_lsTokens,
            m_iCurrentLine = m_iCurrentLine,
            m_iCurrentTokenIndex = m_iCurrentTokenIndex
        };
    }

    public LexerToken Current
    {
        get
        {
            if (m_kCurrentTokenCache != null)
                return m_kCurrentTokenCache;
            return m_lsTokens != null ? m_lsTokens[m_iCurrentTokenIndex] : EOF;
        }
        set
        {
            m_kCurrentTokenCache = value;
        }
    }

    object IEnumerator.Current
    {
        get { return Current; }
    }

    public void Dispose() { }

    public void Reset()
    {
        m_iCurrentLine = -1;
        m_iCurrentTokenIndex = -1;
        m_lsTokens = null;
    }

    public bool MoveNext()
    {
        //if (maxScanDistance > 0)
        //    --maxScanDistance;
        while (MoveNextSingle())
        {
            if (m_lsTokens[m_iCurrentTokenIndex].tokenId == -1)
            {
                var token = m_lsTokens[m_iCurrentTokenIndex];
                if (OnTokenMoveAt != null) OnTokenMoveAt(token);
            }

            if (m_lsTokens[m_iCurrentTokenIndex].tokenKind > LexerToken.Kind.VerbatimStringLiteral)
            {
                return true;
            }
        }
        m_lsTokens = null;
        ++m_iCurrentLine;
        m_iCurrentTokenIndex = -1;
        return false;
    }

    public bool MoveNextSingle()
    {
        while (m_lsTokens == null)
        {
            if (m_iCurrentLine + 1 >= m_lsFormatedLines.Length)
                return false;
            m_iCurrentTokenIndex = -1;
            m_lsTokens = m_lsFormatedLines[++m_iCurrentLine].tokens;
        }
        while (m_iCurrentTokenIndex + 1 >= m_lsTokens.Count)
        {
            if (m_iCurrentLine + 1 >= m_lsFormatedLines.Length)
            {
                m_lsTokens = null;
                return false;
            }
            m_iCurrentTokenIndex = -1;
            m_lsTokens = m_lsFormatedLines[++m_iCurrentLine].tokens;
            while (m_lsTokens == null)
            {
                if (m_iCurrentLine + 1 >= m_lsFormatedLines.Length)
                    return false;
                m_lsTokens = m_lsFormatedLines[++m_iCurrentLine].tokens;
            }
        }
        ++m_iCurrentTokenIndex;
        return true;
    }

    public void MoveTo(int iLineIndex, int iColumnIndex)
    {
        m_lsTokens = m_lsFormatedLines[iLineIndex].tokens;
        m_iCurrentLine = iLineIndex;
        m_iCurrentTokenIndex = iColumnIndex;
    }

    public void InsertToken(LexerToken token, int iLineIndex, int iColumnIndex)
    {
        m_lsFormatedLines[iLineIndex].tokens.Insert(iColumnIndex, token);
        if (iLineIndex == m_iCurrentLine)
            ++m_iCurrentTokenIndex;
    }

    public FormatedLine GetFormatedLine(int iLineIndex)
    {
        return m_lsFormatedLines[iLineIndex];
    }

    public bool IsEnding
    {
        get
        {
            return m_lsTokens == null && CurrentLine > 0;
        }
    }
}
