namespace Knit.Meta;

public interface IGranny2TypeDefinition {
	public Granny2MemberType Type { get; }
	public int NameOffset { get; }
	public int ChildrenOffset { get; }
	public int ArraySize { get; }
	public GrannyCustomTypeInfo CustomTypeInfo { get; }

	public SpanPointer GetTypeDefinition(Granny2File gr);
	public int GetSize(Granny2File gr);
	public int GetArraySize(Granny2File gr);
	public string GetName(Granny2File gr);
}
