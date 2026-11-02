using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class SelectedHeroChecks
{
    internal static void Run(string assembly)
    {
        using var stream = File.OpenRead(assembly);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var methods = new HashSet<string>();
        foreach (var handle in reader.MemberReferences)
        {
            var member = reader.GetMemberReference(handle);
            if (member.Parent.Kind != HandleKind.TypeReference) continue;
            var owner = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
            if (reader.GetString(owner.Namespace) == "" && reader.GetString(owner.Name) == "fqt")
                methods.Add(reader.GetString(member.Name));
        }
        // bnwc reads the selected hero; bnwa is a different hero reference.
        if (!methods.Contains("bnwc") || methods.Contains("bnwa"))
            throw new Exception("Compiled map hints must read the selected hero through fqt.bnwc");
        Console.WriteLine("PASS: compiled map hints use the selected hero");
    }
}
