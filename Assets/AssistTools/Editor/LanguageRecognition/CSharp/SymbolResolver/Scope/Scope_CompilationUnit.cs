public class Scope_CompilationUnit : Scope_Namespace
{
    public string path;

    public SD_Assembly assembly;

    private int numAnonymousSymbols;

    public Scope_CompilationUnit() : base(null) { }

    public override string CreateAnonymousName()
    {
        return ".Anonymous_" + numAnonymousSymbols++;
    }
}

