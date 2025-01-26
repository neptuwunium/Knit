namespace Knit.TypeDefinitions;

[AttributeUsage(AttributeTargets.Property)]
public sealed class GrannyMemberAttribute(string? name = null) : Attribute {
	public string? Name { get; } = name;
}
