namespace Tenuto.Datatype {

using org.relaxng.datatype;

internal class DatatypeLibraryImpl : DatatypeLibrary {
	
	internal static DatatypeLibraryImpl theInstance =
		new DatatypeLibraryImpl();
	
	private DatatypeLibraryImpl() {}
	
	public DatatypeBuilder CreateDatatypeBuilder( string name ) {
		Datatype dt = CreateDatatype(name);
		if(dt==null)	throw new DatatypeException();
		return new DatatypeBuilderImpl(dt);
	}
	
	public Datatype CreateDatatype( string name ) {
		if(name=="string")	return StringType.theInstance;
		if(name=="token")	return TokenType.theInstance;
		throw new DatatypeException();
	}
}

internal class DatatypeBuilderImpl : DatatypeBuilder {
	
	private readonly Datatype type;
	
	internal DatatypeBuilderImpl( Datatype _type ) {
		this.type = _type;
	}
	
	public void AddParameter( string name, string value, ValidationContext context ) {
		throw new DatatypeException("no parameter is allowed");
	}
	
	public Datatype CreateDatatype() {
		return type;
	}
}

internal abstract class BuiltinType : Datatype {
	
	public bool IsValid( string literal, ValidationContext context ) {
		return true;
	}
	
	public void CheckValid( string literal, ValidationContext context ) {
	}
	
	public DatatypeStreamingValidator CreateStreamingValidator( ValidationContext context ) {
		return StreamingValidatorImpl.theInstance;
	}
	
	public int ValueHashCode( object value ) {
		return value.GetHashCode();
	}
	
	public bool SameValue( object value1, object value2 ) {
		return value1.Equals(value2);
	}
	
	public abstract object CreateValue( string literal, ValidationContext context );
}

internal class StringType : BuiltinType {
	
	internal static StringType theInstance = new StringType();
	
	private StringType(){}
	
	public override object CreateValue( string literal, ValidationContext context ) {
		return literal;
	}
}

internal class TokenType : BuiltinType {
	
	internal static TokenType theInstance = new TokenType();
	
	private TokenType(){}
	
	public override object CreateValue( string literal, ValidationContext context ) {
		return literal.Trim();
	}
}

internal class StreamingValidatorImpl : DatatypeStreamingValidator {
	
	internal static StreamingValidatorImpl theInstance = new StreamingValidatorImpl();
	
	private StreamingValidatorImpl() {}
	
	public void AddCharacters( char[] buf, int start, int len ) {}
	public bool IsValid() { return true; }
	public void CheckValid() {}
}


}
