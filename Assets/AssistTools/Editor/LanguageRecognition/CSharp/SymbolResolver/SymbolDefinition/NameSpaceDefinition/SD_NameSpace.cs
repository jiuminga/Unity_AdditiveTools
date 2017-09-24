using System.Collections.Generic;
using System.Reflection;

using Debug = UnityEngine.Debug;

public class SD_NameSpace : SymbolDefinition
{
    public void CollectExtensionMethods(
        string id,
        SymbolReference[] typeArgs,
        TypeDefinitionBase extendedType,
        HashSet<MethodDefinition> extensionsMethods,
        Scope_Base context)
    {
        var numTypeArguments = typeArgs == null ? -1 : typeArgs.Length;

        var contextAssembly = context.GetAssembly();

        for (var i = members.Count; i-- > 0; )
        {
            var typeDefinition = members[i];
            if (typeDefinition.kind != SymbolKind.Class || !typeDefinition.IsValid() || (typeDefinition as TypeDefinitionBase).numExtensionMethods == 0 || !typeDefinition.IsStatic || typeDefinition.NumTypeParameters > 0)
                continue;

            var accessLevelMask = AccessLevelMask.Public;
            if (typeDefinition.Assembly != null && typeDefinition.Assembly.InternalsVisibleIn(contextAssembly))
                accessLevelMask |= AccessLevelMask.Internal;

            if (!typeDefinition.IsAccessible(accessLevelMask))
                continue;

            SymbolDefinition member;
            if (typeDefinition.members.TryGetValue(id, numTypeArguments, out member))
            {
                if (member.kind == SymbolKind.MethodGroup)
                {
                    var methodGroup = member as SD_MethodGroup;
                    if (methodGroup != null)
                    {
                        foreach (var method in methodGroup.methods)
                        {
                            if (method.IsExtensionMethod && method.IsAccessible(accessLevelMask))
                            {
                                var extendsType = method.parameters[0].TypeOf() as TypeDefinitionBase;
                                if (extendedType.CanConvertTo(extendsType))
                                {
                                    if (numTypeArguments > 0)
                                    {
                                        var constructedMethod = method.ConstructMethod(typeArgs);
                                        extensionsMethods.Add(constructedMethod);
                                    }
                                    else
                                    {
                                        extensionsMethods.Add(method);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Expected a method group: " + member.GetTooltipText());
                    }
                }
            }
        }
    }

    private bool resolvingMember = false;
    public override void ResolveMember(SyntaxTreeNode_Leaf leaf, Scope_Base context, int numTypeArgs, bool asTypeOnly)
    {
        if (resolvingMember)
            return;
        resolvingMember = true;

        leaf.ResolvedSymbol = null;

        base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);

        resolvingMember = false;

        if (leaf.ResolvedSymbol == null)
        {
            if (context != null)
            {
                var assemblyDefinition = context.GetAssembly();
                assemblyDefinition.ResolveInReferencedAssemblies(leaf, this, numTypeArgs);
            }
        }
    }

    public override void ResolveAttributeMember(SyntaxTreeNode_Leaf leaf, Scope_Base context)
    {
        if (resolvingMember)
            return;
        resolvingMember = true;

        leaf.ResolvedSymbol = null;
        base.ResolveAttributeMember(leaf, context);

        resolvingMember = false;

        if (leaf.ResolvedSymbol == null)
        {
            var assemblyDefinition = context.GetAssembly();
            assemblyDefinition.ResolveAttributeInReferencedAssemblies(leaf, this);
        }
    }

    public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, SD_Assembly assembly)
    {
        GetMembersCompletionData(data, fromInstance ? 0 : BindingFlags.Static, AccessLevelMask.Any, assembly);
    }

    public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, SD_Assembly assembly)
    {
        base.GetMembersCompletionData(data, flags, mask, assembly);

        var assemblyDefinition = assembly ?? parentSymbol;
        while (assemblyDefinition != null && !(assemblyDefinition is SD_Assembly))
            assemblyDefinition = assemblyDefinition.parentSymbol;
        ((SD_Assembly)assemblyDefinition).GetMembersCompletionDataFromReferencedAssemblies(data, this);
    }

    public void GetTypesOnlyCompletionData(Dictionary<string, SymbolDefinition> data, AccessLevelMask mask, SD_Assembly assembly)
    {
        if ((mask & AccessLevelMask.Public) != 0)
        {
            if (assembly.InternalsVisibleIn(this.Assembly))
                mask |= AccessLevelMask.Internal;
            else
                mask &= ~AccessLevelMask.Internal;
        }

        foreach (var m in members)
        {
            if (m.kind == SymbolKind.Namespace)
                continue;

            if (m.kind != SymbolKind.MethodGroup)
            {
                if (m.IsAccessible(mask) && !data.ContainsKey(m.ReflectionName))
                {
                    data.Add(m.ReflectionName, m);
                }
            }
        }

        var assemblyDefinition = Assembly;
        if (assemblyDefinition != null)
            assemblyDefinition.GetTypesOnlyCompletionDataFromReferencedAssemblies(data, this);
    }

    public override TypeDefinitionBase TypeOfTypeParameter(SD_Type_Parameter tp)
    {
        return tp;
    }

    public override string GetTooltipText()
    {
        return name == string.Empty ? "global namespace" : base.GetTooltipText();
    }

    public void GetExtensionMethodsCompletionData(TypeDefinitionBase targetType, Dictionary<string, SymbolDefinition> data, AccessLevelMask accessLevelMask)
    {
        //	Debug.Log("Extensions for " + targetType.GetTooltipText());
        foreach (var t in members)
        {
            if (t.kind == SymbolKind.Class && t.IsStatic && t.NumTypeParameters == 0 &&
                (t as TypeDefinitionBase).numExtensionMethods > 0 && t.IsAccessible(accessLevelMask))
            {
                var classMembers = t.members;
                foreach (var cm in classMembers)
                {
                    if (cm.kind == SymbolKind.MethodGroup)
                    {
                        var mg = cm as SD_MethodGroup;
                        if (mg == null)
                            continue;
                        if (data.ContainsKey(mg.name))
                            continue;
                        foreach (var m in mg.methods)
                        {
                            if (m.kind != SymbolKind.Method)
                                continue;
                            if (!m.IsExtensionMethod)
                                continue;
                            if (!m.IsAccessible(accessLevelMask))
                                continue;

                            var parameters = m.GetParameters();
                            if (parameters == null || parameters.Count == 0)
                                continue;
                            if (!targetType.CanConvertTo(parameters[0].TypeOf() as TypeDefinitionBase))
                                continue;

                            data.Add(m.name, m);
                            break;
                        }
                    }
                }
            }
        }
    }
}
