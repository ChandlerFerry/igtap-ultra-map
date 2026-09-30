using Mono.Cecil;
using Mono.Cecil.Cil;

static class Prep
{
    public const string ShimName = "IGTAP.UnityShim";

    public static bool Replaced(string name) =>
        name.StartsWith("UnityEngine", StringComparison.Ordinal) || name.StartsWith("Unity.", StringComparison.Ordinal)
        || name is "com.rlabrecque.steamworks.net" or "DOTween" or "Assembly-CSharp-firstpass";

    static int Main(string[] args)
    {
        try
        {
            switch (args.FirstOrDefault())
            {
                case "retarget": return Retarget(args[1], args[2], args[3], args[4], args[5]);
                case "monofloat": return MonoFloatTypes(args[1], args.Skip(2).ToArray());
                default:
                    Console.Error.WriteLine("usage: retarget <Assembly-CSharp.dll> <outDir> <cuts.txt> <hooks.txt> <shim.dll> | monofloat <dll> <types...>");
                    return 2;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    static IEnumerable<string> Lines(string path) =>
        File.ReadAllLines(path).Select(l => l.Split('#')[0].Trim()).Where(l => l.Length > 0);

    static bool Matches(HashSet<string> set, MethodDefinition m) =>
        set.Contains(m.DeclaringType.FullName + "::" + m.Name) || set.Contains(m.DeclaringType.FullName + "::" + m.Name + "(" + m.Parameters.Count + ")");

    static int Retarget(string gamePath, string outDir, string cutsPath, string hooksPath, string shimPath)
    {
        var cuts = Lines(cutsPath).ToHashSet();
        var used = new HashSet<string>();
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(gamePath)));
        AssemblyDefinition game = AssemblyDefinition.ReadAssembly(gamePath, new ReaderParameters { AssemblyResolver = resolver, ReadSymbols = false });
        ModuleDefinition module = game.MainModule;
        TypeDefinition hookType = ModuleDefinition.ReadModule(shimPath).GetType("IGTAP.EngineSim.Hooks")
            ?? throw new InvalidOperationException("IGTAP.EngineSim.Hooks is missing from " + shimPath);
        var hooks = Lines(hooksPath).Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList();
        var shim = new AssemblyNameReference(ShimName, new Version(1, 0, 0, 0));
        module.AssemblyReferences.Add(shim);
        foreach (TypeReference t in module.GetTypeReferences())
            if (t.Scope is AssemblyNameReference a && Replaced(a.Name)) t.Scope = shim;
        foreach (AssemblyNameReference a in module.AssemblyReferences.Where(a => Replaced(a.Name)).ToList())
            module.AssemblyReferences.Remove(a);
        foreach (TypeDefinition type in module.GetTypes())
        {
            StripEngineAttributes(type.CustomAttributes);
            foreach (FieldDefinition f in type.Fields) StripEngineAttributes(f.CustomAttributes);
            foreach (PropertyDefinition p in type.Properties) StripEngineAttributes(p.CustomAttributes);
            foreach (MethodDefinition m in type.Methods)
            {
                StripEngineAttributes(m.CustomAttributes);
                if (!m.HasBody || !Matches(cuts, m)) continue;
                used.Add(m.DeclaringType.FullName + "::" + m.Name);
                Empty(m);
            }
        }
        foreach (string[] hook in hooks)
        {
            string[] target = hook[1].Split("::");
            MethodDefinition method = module.GetType(target[0])?.Methods.SingleOrDefault(m => m.Name == target[1])
                ?? throw new InvalidOperationException("hooks.txt: no single method " + hook[1]);
            MethodDefinition handler = hookType.Methods.SingleOrDefault(m => m.Name == hook[2])
                ?? throw new InvalidOperationException("hooks.txt: no hook " + hook[2]);
            if (handler.Parameters.Count != method.Parameters.Count + 1)
                throw new InvalidOperationException("hooks.txt: " + hook[2] + " must take (self, " + method.Parameters.Count + " args)");
            InjectHook(method, module.ImportReference(handler), hook[0] == "replace");
        }
        int widened = 0;
        foreach (TypeDefinition type in module.GetTypes())
            foreach (MethodDefinition m in type.Methods) widened += MonoFloat.Apply(m);
        StripEngineAttributes(game.CustomAttributes);
        StripEngineAttributes(module.CustomAttributes);
        var unused = cuts.Where(c => !used.Contains(c.Split('(')[0])).ToList();
        if (unused.Count > 0) throw new InvalidOperationException("cuts.txt names methods that do not exist: " + string.Join(", ", unused));
        Directory.CreateDirectory(outDir);
        game.Write(Path.Combine(outDir, "Assembly-CSharp.dll"));
        Console.WriteLine("Retargeted Assembly-CSharp -> " + outDir + " (" + used.Count + " methods cut, " + widened + " float loads widened)");
        return 0;
    }

    static int MonoFloatTypes(string dll, string[] typeNames)
    {
        string temp = dll + ".tmp";
        int widened = 0;
        using (AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(dll, new ReaderParameters { ReadSymbols = false }))
        {
            if (asm.MainModule.GetType("IGTAP.MonoFloatApplied") != null) return 0;
            foreach (string name in typeNames)
            {
                TypeDefinition type = asm.MainModule.GetType(name) ?? throw new InvalidOperationException("No type " + name + " in " + dll);
                foreach (MethodDefinition m in type.Methods) widened += MonoFloat.Apply(m);
            }
            asm.MainModule.Types.Add(new TypeDefinition("IGTAP", "MonoFloatApplied", TypeAttributes.NotPublic | TypeAttributes.Abstract | TypeAttributes.Sealed,
                asm.MainModule.TypeSystem.Object));
            asm.Write(temp);
        }
        File.Copy(temp, dll, true);
        File.Delete(temp);
        Console.WriteLine("Mono float semantics applied to " + typeNames.Length + " types in " + Path.GetFileName(dll) + " (" + widened + " float loads widened)");
        return 0;
    }

    static void StripEngineAttributes(Mono.Collections.Generic.Collection<CustomAttribute> attributes)
    {
        for (int i = attributes.Count - 1; i >= 0; i--)
        {
            TypeReference t = attributes[i].AttributeType;
            if (t.Scope is AssemblyNameReference a && (Replaced(a.Name) || a.Name == ShimName)) attributes.RemoveAt(i);
            else if (attributes[i].HasConstructorArguments && attributes[i].ConstructorArguments.Any(arg => arg.Type.Scope is AssemblyNameReference s && (Replaced(s.Name) || s.Name == ShimName)))
                attributes.RemoveAt(i);
        }
    }

    static void InjectHook(MethodDefinition method, MethodReference hook, bool replace)
    {
        if (replace)
        {
            if (method.ReturnType.MetadataType != MetadataType.Void) throw new InvalidOperationException("replace needs a void method: " + method.FullName);
            method.Body = new MethodBody(method);
            method.Body.GetILProcessor().Emit(OpCodes.Ret);
        }
        ILProcessor il = method.Body.GetILProcessor();
        Instruction first = method.Body.Instructions[0];
        for (int i = 0; i <= method.Parameters.Count; i++)
        {
            Instruction load = il.Create(OpCodes.Ldarg, i == 0 ? method.Body.ThisParameter : method.Parameters[i - 1]);
            il.InsertBefore(first, load);
            if (i > 0 && method.Parameters[i - 1].ParameterType.IsValueType != hook.Parameters[i].ParameterType.IsValueType)
                throw new InvalidOperationException("hook parameter kinds differ for " + method.FullName);
        }
        il.InsertBefore(first, il.Create(OpCodes.Call, hook));
    }

    static void Empty(MethodDefinition m)
    {
        var body = new MethodBody(m);
        ILProcessor il = body.GetILProcessor();
        TypeReference ret = m.ReturnType;
        if (ret.MetadataType != MetadataType.Void)
        {
            var local = new VariableDefinition(ret);
            body.Variables.Add(local);
            body.InitLocals = true;
            il.Emit(OpCodes.Ldloc, local);
        }
        il.Emit(OpCodes.Ret);
        m.Body = body;
    }
}
