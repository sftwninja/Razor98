// Roslyn merges string suffixes in #Strings ("All" points into "SelectAll").
// the 2.0 runtime is fine with that but 2.0's ngen dies with E_INVALIDARG.
// rewrite with dnlib, Create()ing every name up front so its StringsHeap
// doesn't merge them.
// usage: UnshareStrings <assembly> [snk]   (in place)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Writer;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            Console.Error.WriteLine("usage: UnshareStrings <assembly> [snk]");
            return 2;
        }

        string path = args[0];
        byte[] rewritten = Rewrite(File.ReadAllBytes(path), args.Length > 1 ? args[1] : null);

        int shared = CountSharedNames(rewritten, out string example);
        if (shared != 0)
        {
            Console.Error.WriteLine($"{path}: {shared} names still share #Strings entries (e.g. '{example}')");
            return 1;
        }

        File.WriteAllBytes(path, rewritten);
        Console.WriteLine($"{Path.GetFileName(path)}: #Strings unshared");
        return 0;
    }

    static byte[] Rewrite(byte[] image, string snk)
    {
        using ModuleDefMD mod = ModuleDefMD.Load(image);
        var opts = new ModuleWriterOptions(mod);
        if (snk != null)
            opts.InitializeStrongNameSigning(mod, new StrongNameKey(snk));

        var names = new List<UTF8String>();
        void Add(UTF8String s)
        {
            if (!UTF8String.IsNullOrEmpty(s))
                names.Add(s);
        }

        Add(mod.Name);
        if (mod.Assembly != null)
        {
            Add(mod.Assembly.Name);
            Add(mod.Assembly.Culture);
        }
        foreach (TypeDef t in mod.GetTypes())
        {
            Add(t.Name);
            Add(t.Namespace);
            foreach (GenericParam g in t.GenericParameters) Add(g.Name);
            foreach (FieldDef f in t.Fields) Add(f.Name);
            foreach (MethodDef m in t.Methods)
            {
                Add(m.Name);
                foreach (ParamDef p in m.ParamDefs) Add(p.Name);
                foreach (GenericParam g in m.GenericParameters) Add(g.Name);
                if (m.ImplMap != null)
                {
                    Add(m.ImplMap.Name);
                    Add(m.ImplMap.Module?.Name);
                }
            }
            foreach (PropertyDef p in t.Properties) Add(p.Name);
            foreach (EventDef e in t.Events) Add(e.Name);
        }
        foreach (TypeRef r in mod.GetTypeRefs())
        {
            Add(r.Name);
            Add(r.Namespace);
        }
        foreach (MemberRef r in mod.GetMemberRefs()) Add(r.Name);
        foreach (AssemblyRef r in mod.GetAssemblyRefs())
        {
            Add(r.Name);
            Add(r.Culture);
        }
        foreach (ModuleRef r in mod.GetModuleRefs()) Add(r.Name);
        foreach (Resource r in mod.Resources) Add(r.Name);

        opts.WriterEvent += (sender, e) =>
        {
            if (e.Event != ModuleWriterEvent.MDBeginCreateTables)
                return;
            StringsHeap heap = e.Writer.Metadata.StringsHeap;
            var seen = new HashSet<UTF8String>();
            foreach (UTF8String s in names)
                if (seen.Add(s))
                    heap.Create(s);
        };

        using var ms = new MemoryStream();
        mod.Write(ms, opts);
        return ms.ToArray();
    }

    static unsafe int CountSharedNames(byte[] image, out string example)
    {
        example = null;
        using var pe = new PEReader(new MemoryStream(image));
        MetadataReader md = pe.GetMetadataReader();
        PEMemoryBlock block = pe.GetMetadata();
        byte* heap = block.Pointer + md.GetHeapMetadataOffset(HeapIndex.String);

        var handles = new List<StringHandle>();
        ModuleDefinition module = md.GetModuleDefinition();
        handles.Add(module.Name);
        AssemblyDefinition asm = md.GetAssemblyDefinition();
        handles.Add(asm.Name);
        handles.Add(asm.Culture);
        foreach (TypeDefinitionHandle th in md.TypeDefinitions)
        {
            TypeDefinition t = md.GetTypeDefinition(th);
            handles.Add(t.Name);
            handles.Add(t.Namespace);
            foreach (GenericParameterHandle g in t.GetGenericParameters()) handles.Add(md.GetGenericParameter(g).Name);
            foreach (FieldDefinitionHandle f in t.GetFields()) handles.Add(md.GetFieldDefinition(f).Name);
            foreach (MethodDefinitionHandle mh in t.GetMethods())
            {
                MethodDefinition m = md.GetMethodDefinition(mh);
                handles.Add(m.Name);
                foreach (ParameterHandle p in m.GetParameters()) handles.Add(md.GetParameter(p).Name);
                foreach (GenericParameterHandle g in m.GetGenericParameters()) handles.Add(md.GetGenericParameter(g).Name);
                MethodImport imp = m.GetImport();
                if (!imp.Module.IsNil)
                {
                    handles.Add(imp.Name);
                    handles.Add(md.GetModuleReference(imp.Module).Name);
                }
            }
            foreach (PropertyDefinitionHandle p in t.GetProperties()) handles.Add(md.GetPropertyDefinition(p).Name);
            foreach (EventDefinitionHandle e in t.GetEvents()) handles.Add(md.GetEventDefinition(e).Name);
        }
        foreach (TypeReferenceHandle r in md.TypeReferences)
        {
            handles.Add(md.GetTypeReference(r).Name);
            handles.Add(md.GetTypeReference(r).Namespace);
        }
        foreach (MemberReferenceHandle r in md.MemberReferences) handles.Add(md.GetMemberReference(r).Name);
        foreach (AssemblyReferenceHandle r in md.AssemblyReferences)
        {
            handles.Add(md.GetAssemblyReference(r).Name);
            handles.Add(md.GetAssemblyReference(r).Culture);
        }
        foreach (ManifestResourceHandle r in md.ManifestResources) handles.Add(md.GetManifestResource(r).Name);

        int shared = 0;
        foreach (StringHandle h in handles)
        {
            if (h.IsNil)
                continue;
            int offset = MetadataTokens.GetHeapOffset(h);
            if (offset > 0 && heap[offset - 1] != 0)
            {
                shared++;
                example ??= md.GetString(h);
            }
        }
        return shared;
    }
}
