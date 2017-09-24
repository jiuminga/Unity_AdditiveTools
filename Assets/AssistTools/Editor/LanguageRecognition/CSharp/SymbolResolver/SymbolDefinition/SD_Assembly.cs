using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class SD_Assembly : SymbolDefinition
{
    public enum UnityAssembly
    {
        None,
        DllFirstPass,
        CSharpFirstPass,
        UnityScriptFirstPass,
        BooFirstPass,
        DllEditorFirstPass,
        CSharpEditorFirstPass,
        UnityScriptEditorFirstPass,
        BooEditorFirstPass,
        Dll,
        CSharp,
        UnityScript,
        Boo,
        DllEditor,
        CSharpEditor,
        UnityScriptEditor,
        BooEditor,

        Last = BooEditor
    }

    public readonly Assembly assembly;
    public readonly UnityAssembly assemblyId;

    private SD_Assembly[] _referencedAssemblies;
    public SD_Assembly[] referencedAssemblies
    {
        get
        {
            if (_referencedAssemblies == null)
            {
                var raSet = new HashSet<SD_Assembly>();
                if (assembly != null)
                {
                    foreach (var ra in assembly.GetReferencedAssemblies())
                    {
                        var assemblyDefinition = FromName(ra.Name);
                        if (assemblyDefinition != null)
                            raSet.Add(assemblyDefinition);
                    }
                }

                var isEditorAssembly = false;
                var isFirstPassAssembly = false;
                switch (assemblyId)
                {
                    case UnityAssembly.CSharpFirstPass:
                    case UnityAssembly.UnityScriptFirstPass:
                    case UnityAssembly.BooFirstPass:
                        isFirstPassAssembly = true;
                        break;

                    case UnityAssembly.CSharpEditorFirstPass:
                    case UnityAssembly.UnityScriptEditorFirstPass:
                    case UnityAssembly.BooEditorFirstPass:
                        isFirstPassAssembly = true;
                        isEditorAssembly = true;
                        break;

                    case UnityAssembly.CSharpEditor:
                    case UnityAssembly.UnityScriptEditor:
                    case UnityAssembly.BooEditor:
                        isEditorAssembly = true;
                        break;
                }

                var stdAssemblies = isEditorAssembly ? editorReferencedAssemblies : standardReferencedAssemblies;

                raSet.UnionWith(
                    from a in stdAssemblies
                    select FromName(a.GetName().Name)
                );

                if (isEditorAssembly || !isFirstPassAssembly)
                {
                    raSet.Add(FromId(UnityAssembly.CSharpFirstPass));
                    raSet.Add(FromId(UnityAssembly.UnityScriptFirstPass));
                    raSet.Add(FromId(UnityAssembly.BooFirstPass));
                }
                if (isEditorAssembly && !isFirstPassAssembly)
                {
                    raSet.Add(FromId(UnityAssembly.CSharp));
                    raSet.Add(FromId(UnityAssembly.UnityScript));
                    raSet.Add(FromId(UnityAssembly.Boo));
                    raSet.Add(FromId(UnityAssembly.CSharpEditorFirstPass));
                    raSet.Add(FromId(UnityAssembly.UnityScriptEditorFirstPass));
                    raSet.Add(FromId(UnityAssembly.BooEditorFirstPass));
                }

                raSet.Remove(null);

                _referencedAssemblies = new SD_Assembly[raSet.Count];
                raSet.CopyTo(_referencedAssemblies);
            }
            return _referencedAssemblies;
        }
    }

    public Dictionary<string, Scope_CompilationUnit> compilationUnits;

    private static readonly Dictionary<Assembly, SD_Assembly> allAssemblies = new Dictionary<Assembly, SD_Assembly>();
    public static SD_Assembly FromAssembly(Assembly assembly)
    {
        SD_Assembly definition;
        if (!allAssemblies.TryGetValue(assembly, out definition))
        {
            definition = new SD_Assembly(assembly);
            allAssemblies[assembly] = definition;
        }
        return definition;
    }

    private static readonly string[] unityAssemblyNames = new[]
        {
        null,
        "assembly-csharp-firstpass",
        "assembly-unityscript-firstpass",
        "assembly-boo-firstpass",
        null,
        "assembly-csharp-editor-firstpass",
        "assembly-unityscript-editor-firstpass",
        "assembly-boo-editor-firstpass",
        null,
        "assembly-csharp",
        "assembly-unityscript",
        "assembly-boo",
        null,
        "assembly-csharp-editor",
        "assembly-unityscript-editor",
        "assembly-boo-editor"
    };

    public static bool IsScriptAssemblyName(string name)
    {
        return Array.IndexOf<string>(unityAssemblyNames, name.ToLowerInvariant()) >= 0;
    }

    private static Assembly[] standardReferencedAssemblies;
    private static Assembly[] editorReferencedAssemblies;

    private static Assembly[] _domainAssemblies;
    private static Assembly[] domainAssemblies
    {
        get
        {
            if (_domainAssemblies == null || _domainAssemblies.Length != AppDomain.CurrentDomain.GetAssemblies().Length)
            {
                var standardRefs = new List<Assembly>();
                var editorRefs = new List<Assembly>();
                var assetsPath = UnityEngine.Application.dataPath.ToLowerInvariant();

                _domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in _domainAssemblies)
                {
                    if (assembly is System.Reflection.Emit.AssemblyBuilder)
                        continue;

                    var path = assembly.Location.Replace('\\', '/').ToLowerInvariant();
                    if (path.StartsWith(assetsPath, StringComparison.Ordinal))
                    {
                        path = path.Substring(assetsPath.Length - "assets".Length);
                        var id = AssemblyIdFromAssetPath(path);
                        if (id == UnityAssembly.Dll || id == UnityAssembly.DllFirstPass)
                            standardRefs.Add(assembly);
                        else if (id == UnityAssembly.DllEditor || id == UnityAssembly.DllEditorFirstPass)
                            editorRefs.Add(assembly);
                    }
                    else if (path.EndsWith("/unityengine.dll", StringComparison.Ordinal)
                        || path.EndsWith("/unityeditor.dll", StringComparison.Ordinal)
                        || path.EndsWith("/system.dll", StringComparison.Ordinal)
                        || path.EndsWith("/system.core.dll", StringComparison.Ordinal)
                        || path.EndsWith("/system.xml.linq.dll", StringComparison.Ordinal)
                        || path.EndsWith("/system.xml.dll", StringComparison.Ordinal))
                    {
                        standardRefs.Add(assembly);
                    }
                    else if (path.EndsWith("/unityeditor.graphs.dll", StringComparison.Ordinal))
                    {
                        editorRefs.Add(assembly);
                    }
                }
                standardReferencedAssemblies = standardRefs.ToArray();
                editorRefs.AddRange(standardRefs);
                editorReferencedAssemblies = editorRefs.ToArray();
            }
            return _domainAssemblies;
        }
    }

    private static SD_Assembly FromName(string assemblyName)
    {
        assemblyName = assemblyName.ToLower();
        for (var i = domainAssemblies.Length; i-- > 0;)
        {
            var assembly = domainAssemblies[i];
            if (assembly is System.Reflection.Emit.AssemblyBuilder)
                continue;
            if (assembly.GetName().Name.ToLower() == assemblyName)
                return FromAssembly(assembly);
        }
        return null;
    }

    private static readonly SD_Assembly[] unityAssemblies = new SD_Assembly[(int)UnityAssembly.Last - 1];
    public static SD_Assembly FromId(UnityAssembly assemblyId)
    {
        if (assemblyId == UnityAssembly.None)
            return null;

        var index = ((int)assemblyId) - 1;
        if (unityAssemblies[index] == null)
        {
            var assemblyName = unityAssemblyNames[index];
            unityAssemblies[index] = FromName(assemblyName) ?? new SD_Assembly(assemblyId);
        }
        return unityAssemblies[index];
    }

    public static UnityAssembly AssemblyIdFromAssetPath(string pathName)
    {
        var ext = (System.IO.Path.GetExtension(pathName) ?? string.Empty).ToLower();
        var isCSharp = ext == ".cs";
        var isUnityScript = ext == ".js";
        var isBoo = ext == ".boo";
        var isDll = ext == ".dll";
        if (!isCSharp && !isUnityScript && !isBoo && !isDll)
            return UnityAssembly.None;

        var path = (System.IO.Path.GetDirectoryName(pathName) ?? string.Empty).ToLowerInvariant() + "/";

        var isIgnoredScript = path.StartsWith("assets/webplayertemplates/", StringComparison.Ordinal);
        if (isIgnoredScript)
            return UnityAssembly.None;

        bool isUnity_5_2_1p4_orNewer = true;
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
		isUnity_5_2_1p4_orNewer =
			UnityEngine.Application.unityVersion.StartsWith("5.2.1p") &&
			int.Parse(UnityEngine.Application.unityVersion.Substring("5.2.1p".Length)) >= 4;
#endif

        var isPlugins = path.StartsWith("assets/plugins/", StringComparison.Ordinal);
        var isStandardAssets = path.StartsWith("assets/standard assets/", StringComparison.Ordinal) ||
            path.StartsWith("assets/pro standard assets/", StringComparison.Ordinal);
        var isFirstPass = isPlugins || isStandardAssets;
        bool isEditor;
        if (isFirstPass && !isUnity_5_2_1p4_orNewer)
        {
            isEditor =
                isPlugins && path.StartsWith("assets/plugins/editor/", StringComparison.Ordinal) ||
                isStandardAssets && path.StartsWith("assets/pro standard assets/editor/", StringComparison.Ordinal) ||
                isStandardAssets && path.StartsWith("assets/standard assets/editor/", StringComparison.Ordinal);
        }
        else
        {
            isEditor = path.Contains("/editor/");
        }

        UnityAssembly assemblyId;
        if (isFirstPass && isEditor)
            assemblyId = isCSharp ? UnityAssembly.CSharpEditorFirstPass : isBoo ? UnityAssembly.BooEditorFirstPass : isUnityScript ? UnityAssembly.UnityScriptEditorFirstPass : UnityAssembly.DllEditorFirstPass;
        else if (isEditor)
            assemblyId = isCSharp ? UnityAssembly.CSharpEditor : isBoo ? UnityAssembly.BooEditor : isUnityScript ? UnityAssembly.UnityScriptEditor : UnityAssembly.DllEditor;
        else if (isFirstPass)
            assemblyId = isCSharp ? UnityAssembly.CSharpFirstPass : isBoo ? UnityAssembly.BooFirstPass : isUnityScript ? UnityAssembly.UnityScriptFirstPass : UnityAssembly.DllFirstPass;
        else
            assemblyId = isCSharp ? UnityAssembly.CSharp : isBoo ? UnityAssembly.Boo : isUnityScript ? UnityAssembly.UnityScript : UnityAssembly.Dll;

        return assemblyId;
    }

    public static SD_Assembly FromAssetPath(string pathName)
    {
        return FromId(AssemblyIdFromAssetPath(pathName));
    }

    private SD_Assembly(UnityAssembly id)
    {
        assemblyId = id;
    }

    private SD_Assembly(Assembly assembly)
    {
        this.assembly = assembly;

        switch (assembly.GetName().Name.ToLower())
        {
            case "assembly-csharp-firstpass":
                assemblyId = UnityAssembly.CSharpFirstPass;
                break;
            case "assembly-unityscript-firstpass":
                assemblyId = UnityAssembly.UnityScriptFirstPass;
                break;
            case "assembly-boo-firstpass":
                assemblyId = UnityAssembly.BooFirstPass;
                break;
            case "assembly-csharp-editor-firstpass":
                assemblyId = UnityAssembly.CSharpEditorFirstPass;
                break;
            case "assembly-unityscript-editor-firstpass":
                assemblyId = UnityAssembly.UnityScriptEditorFirstPass;
                break;
            case "assembly-boo-editor-firstpass":
                assemblyId = UnityAssembly.BooEditorFirstPass;
                break;
            case "assembly-csharp":
                assemblyId = UnityAssembly.CSharp;
                break;
            case "assembly-unityscript":
                assemblyId = UnityAssembly.UnityScript;
                break;
            case "assembly-boo":
                assemblyId = UnityAssembly.Boo;
                break;
            case "assembly-csharp-editor":
                assemblyId = UnityAssembly.CSharpEditor;
                break;
            case "assembly-unityscript-editor":
                assemblyId = UnityAssembly.UnityScriptEditor;
                break;
            case "assembly-boo-editor":
                assemblyId = UnityAssembly.BooEditor;
                break;
            default:
                assemblyId = UnityAssembly.None;
                break;
        }
    }

    public string AssemblyName
    {
        get
        {
            return assembly.GetName().Name;
        }
    }

    public bool InternalsVisibleIn(SD_Assembly referencingAssembly)
    {
        if (referencingAssembly == this)
            return true;

        //TODO: Check are internals visible

        return false;
    }

    public static Scope_CompilationUnit GetCompilationUnitScope(SD_Assembly assembly, string sName, bool forceCreateNew = false)
    {
        if (assembly == null)
            return null;

        if (assembly.compilationUnits == null)
            assembly.compilationUnits = new Dictionary<string, Scope_CompilationUnit>();

        Scope_CompilationUnit scope;
        if (!assembly.compilationUnits.TryGetValue(sName, out scope) || forceCreateNew)
        {
            if (forceCreateNew)
            {
                if (scope != null && scope.typeDeclarations != null)
                {
                    var newResolverVersion = false;
                    var scopeTypes = scope.typeDeclarations;
                    for (var i = scopeTypes.Count; i-- > 0;)
                    {
                        var typeDeclaration = scopeTypes[i];
                        scope.RemoveDeclaration(typeDeclaration);
                        newResolverVersion = true;
                    }
                    if (newResolverVersion)
                    {
                        ++LR_SyntaxTree.resolverVersion;
                        if (LR_SyntaxTree.resolverVersion == 0)
                            ++LR_SyntaxTree.resolverVersion;
                    }
                }
                assembly.compilationUnits.Remove(sName);
            }

            scope = new Scope_CompilationUnit
            {
                assembly = assembly,
                path = sName,
            };
            assembly.compilationUnits[sName] = scope;

            //var cuDefinition = new CompilationUnitDefinition
            //{
            //    kind = SymbolKind.None,
            //    parentSymbol = assembly,
            //};

            scope.declaration = new Declaration_Namespace
            {
                kind = SymbolKind.Namespace,
                definition = assembly.GlobalNamespace,
            };
            scope.definition = assembly.GlobalNamespace;
        }
        return scope;
    }

    public static Scope_CompilationUnit GetCompilationUnitScope(UnityAssembly type, string sName, bool forceCreateNew = false)
    {
        var assembly = FromId(type);
        return GetCompilationUnitScope(assembly, sName, forceCreateNew);
    }

    public static Scope_CompilationUnit GetCompilationUnitScope(string assetPath, bool forceCreateNew = false)
    {
        if (assetPath == null)
            return null;

        assetPath = assetPath.ToLower();

        var assembly = FromAssetPath(assetPath);
        return GetCompilationUnitScope(assembly, assetPath, forceCreateNew);
    }

    private SD_NameSpace _globalNamespace;
    public SD_NameSpace GlobalNamespace
    {
        get { return _globalNamespace ?? InitializeGlobalNamespace(); }
        set { _globalNamespace = value; }
    }

    private SD_NameSpace InitializeGlobalNamespace()
    {
        //	var timer = new Stopwatch();
        //	timer.Start();

        _globalNamespace = new SD_NameSpace { name = "", kind = SymbolKind.Namespace, parentSymbol = this };

        if (assembly != null)
        {
            var types = assemblyId != UnityAssembly.None ? assembly.GetTypes() : assembly.GetExportedTypes();
            foreach (var t in types)
            {
                if (t.IsNested)
                    continue;

                SymbolDefinition current = _globalNamespace;

                if (!string.IsNullOrEmpty(t.Namespace))
                {
                    var ns = t.Namespace.Split('.');
                    for (var i = 0; i < ns.Length; ++i)
                    {
                        var nsName = ns[i];
                        var definition = current.FindName(nsName, 0, true);
                        if (definition != null)
                        {
                            current = definition;
                        }
                        else
                        {
                            var nsd = new SD_NameSpace
                            {
                                kind = SymbolKind.Namespace,
                                name = nsName,
                                parentSymbol = current,
                                accessLevel = AccessLevel.Public,
                                modifiers = Modifiers.Public,
                            };
                            current.AddMember(nsd);
                            current = nsd;
                        }
                    }
                }

                current.ImportReflectedType(t);
            }
        }

        //	timer.Stop();
        //	UnityEngine.Debug.Log(timer.ElapsedMilliseconds + " ms\n" + string.Join(", ", _globalNamespace.members.Keys.ToArray()));
        //	Debug.Log(_globalNamespace.Dump());

        if (builtInTypes == null)
        {
            builtInTypes = new Dictionary<string, TypeDefinitionBase>
            {
                { "int", builtInTypes_int = DefineBuiltInType(typeof(int)) },
                { "uint", builtInTypes_uint = DefineBuiltInType(typeof(uint)) },
                { "byte", builtInTypes_byte = DefineBuiltInType(typeof(byte)) },
                { "sbyte", builtInTypes_sbyte = DefineBuiltInType(typeof(sbyte)) },
                { "short", builtInTypes_short = DefineBuiltInType(typeof(short)) },
                { "ushort", builtInTypes_ushort = DefineBuiltInType(typeof(ushort)) },
                { "long", builtInTypes_long = DefineBuiltInType(typeof(long)) },
                { "ulong", builtInTypes_ulong = DefineBuiltInType(typeof(ulong)) },
                { "float", builtInTypes_float = DefineBuiltInType(typeof(float)) },
                { "double", builtInTypes_double = DefineBuiltInType(typeof(float)) },
                { "decimal", builtInTypes_decimal = DefineBuiltInType(typeof(decimal)) },
                { "char", builtInTypes_char = DefineBuiltInType(typeof(char)) },
                { "string", builtInTypes_string = DefineBuiltInType(typeof(string)) },
                { "bool", builtInTypes_bool = DefineBuiltInType(typeof(bool)) },
                { "object", builtInTypes_object = DefineBuiltInType(typeof(object)) },
                { "void", builtInTypes_void = DefineBuiltInType(typeof(void)) },
            };

            builtInTypes_Array = DefineBuiltInType(typeof(System.Array));
            builtInTypes_Nullable = DefineBuiltInType(typeof(System.Nullable<>));
            builtInTypes_IEnumerable = DefineBuiltInType(typeof(System.Collections.IEnumerable));
            builtInTypes_IEnumerable_1 = DefineBuiltInType(typeof(System.Collections.Generic.IEnumerable<>));
            builtInTypes_Exception = DefineBuiltInType(typeof(System.Exception));
        }

        return _globalNamespace;
    }

    public static SD_Type DefineBuiltInType(Type type)
    {
        var assembly = FromAssembly(type.Assembly);
        var @namespace = assembly.FindNamespace(type.Namespace);
        var name = type.Name;
        var index = name.IndexOf('`');
        if (index > 0)
            name = name.Substring(0, index);
        var definition = @namespace.FindName(name, type.GetGenericArguments().Length, true);
        return definition as SD_Type;
    }

    public SymbolDefinition FindNamespace(string namespaceName)
    {
        SymbolDefinition result = GlobalNamespace;
        if (string.IsNullOrEmpty(namespaceName))
            return result;
        var start = 0;
        while (start < namespaceName.Length)
        {
            var dotPos = namespaceName.IndexOf('.', start);
            var ns = dotPos == -1 ? namespaceName.Substring(start) : namespaceName.Substring(start, dotPos - start);
            result = result.FindName(ns, 0, true) as SD_NameSpace;
            if (result == null)
                return unknownSymbol;
            start = dotPos == -1 ? int.MaxValue : dotPos + 1;
        }
        return result ?? unknownSymbol;
    }

    public SD_NameSpace FindSameNamespace(SD_NameSpace namespaceDefinition)
    {
        if (string.IsNullOrEmpty(namespaceDefinition.name))
            return GlobalNamespace;
        var parent = FindSameNamespace(namespaceDefinition.parentSymbol as SD_NameSpace);
        if (parent == null)
            return null;
        return parent.FindName(namespaceDefinition.name, 0, true) as SD_NameSpace;
    }

    public void ResolveInReferencedAssemblies(SyntaxTreeNode_Leaf leaf, SD_NameSpace namespaceDefinition, int numTypeArgs)
    {
        var leafText = DecodeId(leaf.token.text);

        foreach (var ra in referencedAssemblies)
        {
            var nsDef = ra.FindSameNamespace(namespaceDefinition);
            if (nsDef != null)
            {
                leaf.ResolvedSymbol = nsDef.FindName(leafText, numTypeArgs, true);
                if (leaf.ResolvedSymbol != null)
                    return;
            }
        }
    }

    public void ResolveAttributeInReferencedAssemblies(SyntaxTreeNode_Leaf leaf, SD_NameSpace namespaceDefinition)
    {
        var leafText = DecodeId(leaf.token.text);

        foreach (var ra in referencedAssemblies)
        {
            var nsDef = ra.FindSameNamespace(namespaceDefinition);
            if (nsDef != null)
            {
                leaf.ResolvedSymbol = nsDef.FindName(leafText, 0, true);
                if (leaf.ResolvedSymbol != null)
                    return;

                leaf.ResolvedSymbol = nsDef.FindName(leafText + "Attribute", 0, true);
                if (leaf.ResolvedSymbol != null)
                    return;
            }
        }
    }

    private static bool dontReEnter = false;

    public void GetMembersCompletionDataFromReferencedAssemblies(Dictionary<string, SymbolDefinition> data, SD_NameSpace namespaceDefinition)
    {
        if (dontReEnter)
            return;

        foreach (var ra in referencedAssemblies)
        {
            var nsDef = ra.FindSameNamespace(namespaceDefinition);
            if (nsDef != null)
            {
                dontReEnter = true;
                var accessLevelMask = ra.InternalsVisibleIn(this) ? AccessLevelMask.Public | AccessLevelMask.Internal : AccessLevelMask.Public;
                nsDef.GetMembersCompletionData(data, 0, accessLevelMask, this);
                dontReEnter = false;
            }
        }
    }

    public void GetTypesOnlyCompletionDataFromReferencedAssemblies(Dictionary<string, SymbolDefinition> data, SD_NameSpace namespaceDefinition)
    {
        if (dontReEnter)
            return;

        foreach (var ra in referencedAssemblies)
        {
            var nsDef = ra.FindSameNamespace(namespaceDefinition);
            if (nsDef != null)
            {
                dontReEnter = true;
                var accessLevelMask = ra.InternalsVisibleIn(this) ? AccessLevelMask.Public | AccessLevelMask.Internal : AccessLevelMask.Public;
                nsDef.GetTypesOnlyCompletionData(data, accessLevelMask, this);
                dontReEnter = false;
            }
        }
    }

    public void CollectExtensionMethods(
        SD_NameSpace namespaceDefinition,
        string id,
        SymbolReference[] typeArgs,
        TypeDefinitionBase extendedType,
        HashSet<MethodDefinition> extensionsMethods,
        Scope_Base context)
    {
        namespaceDefinition.CollectExtensionMethods(id, typeArgs, extendedType, extensionsMethods, context);

        foreach (var ra in referencedAssemblies)
        {
            var nsDef = ra.FindSameNamespace(namespaceDefinition);
            if (nsDef != null)
                nsDef.CollectExtensionMethods(id, typeArgs, extendedType, extensionsMethods, context);
        }
    }

    public void GetExtensionMethodsCompletionData(TypeDefinitionBase targetType, SD_NameSpace namespaceDefinition, Dictionary<string, SymbolDefinition> data)
    {
        namespaceDefinition.GetExtensionMethodsCompletionData(targetType, data, AccessLevelMask.Public | AccessLevelMask.Internal);

        foreach (var ra in referencedAssemblies)
        {
            var nsDef = ra.FindSameNamespace(namespaceDefinition);
            if (nsDef != null)
                nsDef.GetExtensionMethodsCompletionData(targetType, data, AccessLevelMask.Public | (ra.InternalsVisibleIn(this) ? AccessLevelMask.Internal : 0));
        }
    }

    public IEnumerable<TypeDefinitionBase> EnumAssignableTypesFor(TypeDefinitionBase type)
    {
        yield return type;
        //foreach (var derived in Assembly.EnumDerivedTypes(this))
        //	yield return derived;
    }
}

