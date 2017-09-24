using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class FormatedLineWrapper : IEnumerator<FormatedLine>
{
    private readonly FormatedLine[] m_lsFormatLine;
    public int m_iCount;
    private int m_iCurIndex;
    public int m_iStartIndex;

    public FormatedLine Current
    {
        get
        {
            return m_lsFormatLine[m_iCurIndex % m_lsFormatLine.Length];
        }
    }

    object IEnumerator.Current { get { return Current; } }

    public FormatedLineWrapper(int iCount)
    {
        m_lsFormatLine = new FormatedLine[iCount];
    }

    public void Dispose() { }

    public bool MoveNext()
    {
        m_iCurIndex++;
        return m_iCurIndex <= m_iStartIndex + m_iCount;
    }

    public void Reset()
    {
        m_iCurIndex = m_iStartIndex;
    }
}
