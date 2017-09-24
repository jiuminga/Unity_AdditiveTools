using System;
using System.Collections.Generic;
using System.Linq;

public enum SymbolKind : byte
{
    None,
    Error,
    _Keyword,
    _Snippet,
    Namespace,
    Interface,
    Enum,
    Struct,
    Class,
    Delegate,
    Field,
    ConstantField,
    LocalConstant,
    EnumMember,
    Property,
    Event,
    Indexer,
    Method,
    ExtensionMethod,
    MethodGroup,
    Constructor,
    Destructor,
    Operator,
    Accessor,
    LambdaExpression,
    Parameter,
    CatchParameter,
    Variable,
    ForEachVariable,
    FromClauseVariable,
    TypeParameter,
    TypeParameterConstraintList,
    BaseTypesList,
    Instance,
    Null,
    Label,
    ImportedNamespace,
    TypeAlias,
}

[Flags]
public enum Modifiers
{
    None = 0,
    Public = 1 << 0,
    Internal = 1 << 1,
    Protected = 1 << 2,
    Private = 1 << 3,
    Static = 1 << 4,
    New = 1 << 5,
    Sealed = 1 << 6,
    Abstract = 1 << 7,
    ReadOnly = 1 << 8,
    Volatile = 1 << 9,
    Virtual = 1 << 10,
    Override = 1 << 11,
    Extern = 1 << 12,
    Ref = 1 << 13,
    Out = 1 << 14,
    Params = 1 << 15,
    This = 1 << 16,
    Partial = 1 << 17,
}

public enum AccessLevel : byte
{
    None = 0,
    Private = 1, // private
    ProtectedAndInternal = 2, // n/a
    ProtectedOrInternal = 4, // protected internal
    Protected, // protected
    Internal, // internal
    Public, // public
}

[Flags]
public enum AccessLevelMask : byte
{
    None = 0,
    Private = 1, // private
    Protected = 2, // protected
    Internal = 4, // internal
    Public = 8, // public

    Any = Private | Protected | Internal | Public,
    NonPublic = Private | Protected | Internal,
}

public struct TypeAlias
{
    public string name;
    public SymbolReference type;
    public SymbolDeclaration declaration;
}

static class ToCSharpStringExtensions
{
    public static string ToCSharpString(this AccessLevel self)
    {
        switch (self)
        {
            case AccessLevel.Public:
                return "public";
            case AccessLevel.Internal:
                return "internal";
            case AccessLevel.Protected:
                return "protected";
            case AccessLevel.ProtectedOrInternal:
                return "protected internal";
            default:
                return "private";
        }
    }
}

public static class ListExtensions
{
    public static T FirstOrDefault<T>(this List<T> self)
    {
        return self.Count == 0 ? default(T) : self[0];
    }

    public static T ElementAtOrDefault<T>(this List<T> self, int index)
    {
        return index >= self.Count ? default(T) : self[index];
    }
}

public class ValueParameter : SD_Instance_Parameter { }

static class DictExtensions
{
    public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    {
        return "{" + string.Join(",", dictionary.Select(kv => kv.Key.ToString() + "=" + kv.Value.ToString()).ToArray()) + "}";
    }
}

