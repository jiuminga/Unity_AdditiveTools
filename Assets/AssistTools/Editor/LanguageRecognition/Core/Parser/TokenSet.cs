using UnityEngine;
using System;
using System.Collections;
using System.Text;
public class TokenSet
{
    // true if empty input is acceptable.
    protected bool empty;

    // if != null: many elements.
    private BitArray set;

    // else if >= 0: single element.
    private int tokenId = -1;

    public int GetDataSet(out BitArray bitArray)
    {
        bitArray = set;
        return tokenId;
    }

    // empty set, doesn't accept even empty input.
    public TokenSet() { }

    public TokenSet(int tokenId)
    {
        this.tokenId = tokenId;
    }

    public TokenSet(TokenSet s)
    {
        empty = s.empty;
        if (s.set != null)
            set = new BitArray(s.set);
        else
            tokenId = s.tokenId;
    }

    public void AddEmpty()
    {
        empty = true;
    }

    public void RemoveEmpty()
    {
        empty = false;
    }

    public bool Remove(int token)
    {
        if (set == null)
        {
            if (token != tokenId)
                return false;
            tokenId = -1;
            return true;
        }
        if (token >= set.Count)
            Debug.LogError("Unknown token " + token);
        bool result = set[token];
        set[token] = false;
        return result;
    }

    // set to accept additional set of tokens.
    // returns true if set changed.
    public bool Add(TokenSet s)
    {
        var result = false;
        if (s.empty && !empty)
        {
            empty = true;
            result = true;
        }
        if (s.set != null)
        {
            if (set != null)
            {
                for (var n = 0; n < s.set.Count; ++n)
                {
                    if (s.set[n] && !set[n])
                    {
                        set.Set(n, true);
                        result = true;
                    }
                }
            }
            else
            {
                set = new BitArray(s.set);
                if (tokenId >= 0)
                {
                    set.Set(tokenId, true);
                    tokenId = -1;
                }
                result = true;  // s.set cannot just contain one token
            }
        }
        else if (s.tokenId >= 0)
        {
            if (set != null)
            {
                if (!set.Get(s.tokenId))
                {
                    set.Set(s.tokenId, true);
                    result = true;
                }
            }
            else if (tokenId >= 0)
            {
                if (tokenId != s.tokenId)
                {
                    set = new BitArray(700, false);
                    set.Set(s.tokenId, true);
                    set.Set(tokenId, true);
                    tokenId = -1;
                    result = true;
                }
            }
            else
            {
                tokenId = s.tokenId;
                result = true;
            }
        }
        return result;
    }

    // checks if lookahead accepts empty input.
    public bool ContainsEmpty()
    {
        return empty;
    }

    // checks if lookahead accepts input symbol.
    public bool Contains(TokenSet tokenSet)
    {
        if (tokenSet == null)
            return false;
        if (tokenSet.tokenId >= 0)
            return set != null ? set[tokenSet.tokenId] : tokenId == tokenSet.tokenId;
        throw new Exception("matches() botched");
    }

    // checks if lookahead accepts input symbol.
    public bool Contains(LexerToken token)
    {
        return set != null ? set[token.tokenId] : token.tokenId == tokenId;
    }

    // checks if lookahead accepts input symbol.
    public bool Contains(int token)
    {
        if (set == null)
            return token == tokenId;
        if (token >= set.Count)
            Debug.LogError("Unknown token " + token);
        return set[token];
    }

    // checks for ambiguous lookahead.
    public bool Accepts(TokenSet s)
    {
        if (s.set != null)
        {
            if (set != null)
            {
                var intersection = new BitArray(set);
                intersection = intersection.And(s.set);
                for (var n = 0; n < intersection.Count; ++n)
                    if (intersection[n])
                        return true;
            }
            else if (tokenId >= 0)
                return s.set[tokenId];
        }
        else if (s.tokenId >= 0)
        {
            if (set != null)
                return set[s.tokenId];
            if (tokenId >= 0)
                return tokenId == s.tokenId;
        }
        return false;
    }

    public TokenSet Intersecton(TokenSet s)
    {
        if (s.set != null)
        {
            if (set != null)
            {
                var intersection = new BitArray(set);
                intersection = intersection.And(s.set);
                var ts = new TokenSet();
                for (var i = 0; i < intersection.Length; ++i)
                    if (intersection[i])
                        ts.Add(new TokenSet(i));
                return ts;
            }
            else if (tokenId >= 0 && s.set[tokenId])
                return this;
        }
        else if (s.tokenId >= 0)
        {
            if (set != null && set[s.tokenId])
                return s;
            if (tokenId >= 0 && tokenId == s.tokenId)
                return this;
        }
        return new TokenSet();
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        var delim = "";
        if (empty)
        {
            result.Append("empty");
            delim = ", ";
        }
        if (set != null)
            result.Append(delim + "set " + set);
        else if (tokenId >= 0)
            result.Append(delim + "token " + tokenId);
        return "{" + result + "}";
    }

    private string cached;
    public string ToString(ParseNode_Root parser)
    {
        if (cached != null)
            return cached;

        var result = new StringBuilder();
        var delim = string.Empty;
        if (empty)
        {
            result.Append("[empty]");
            delim = ", ";
        }
        if (set != null)
        {
            for (var n = 0; n < set.Count; ++n)
            {
                if (set.Get(n))
                {
                    result.Append(delim + parser.GetToken(n));
                    delim = n == set.Count - 2 ? ", or " : ", ";
                }
            }
        }
        else if (tokenId >= 0)
        {
            result.Append(delim + parser.GetToken(tokenId));
        }
        return cached = result.ToString();
    }
}
