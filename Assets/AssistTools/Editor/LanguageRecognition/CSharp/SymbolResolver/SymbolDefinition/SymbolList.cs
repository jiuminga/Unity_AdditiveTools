using System.Collections.Generic;
using System.Linq;

public class SymbolList : List<SymbolDefinition>
{
    public bool TryGetValue(string name, int numTypeParameters, out SymbolDefinition value)
    {
        for (var index = Count; index-- > 0; )
        {
            var x = base[index];
            if (x.name == name &&
                (numTypeParameters < 0 ||
                    x.kind == SymbolKind.MethodGroup ||
                    x.NumTypeParameters == numTypeParameters))
            {
                value = x;
                return true;
            }
        }

        value = null;
        return false;
    }

    public bool Remove(string name, int numTypeParameters)
    {
        for (var index = Count; index-- > 0; )
        {
            var x = base[index];
            if (x.name == name && (x.kind == SymbolKind.MethodGroup || x.NumTypeParameters == numTypeParameters))
            {
                RemoveAt(index);
                return true;
            }
        }
        return false;
    }

    public bool Contains(string name, int numTypeParameters)
    {
        SymbolDefinition value;
        return TryGetValue(name, numTypeParameters, out value);
    }

    public SymbolDefinition this[string name, int numTypeParameters]
    {
        get
        {
            SymbolDefinition value;
            if (!TryGetValue(name, numTypeParameters, out value))
                throw new KeyNotFoundException(name);
            return value;
        }

        set
        {
            var index = this.FindIndex(x => x.name == name
                && (x.kind == SymbolKind.MethodGroup || x.NumTypeParameters == numTypeParameters));
            while (index >= 0)
            {
                var old = base[index];
                if (old.declarations == null ||
                    old.declarations.Count == 0 ||
                    old.declarations.All(x => !x.IsValid()))
                {
                    RemoveAt(index);
                }
                else
                {
                    ++index;
                }

                if (index < Count)
                    index = this.FindIndex(index, x => x.name == name
                        && (x.kind == SymbolKind.MethodGroup || x.NumTypeParameters == numTypeParameters));
                else
                    break;
            }
            Add(value);
        }
    }
}


