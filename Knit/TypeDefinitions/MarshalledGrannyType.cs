using Knit.Meta;

namespace Knit.TypeDefinitions;

public abstract class MarshalledGrannyType {
	[GrannyMember] public MarshalledGrannyType? ExtendedData { get; set; }

	public virtual bool Visit(Granny2File file, string name, IGranny2TypeDefinition typeInfo, SpanPointer objectLocation) => false;
}
