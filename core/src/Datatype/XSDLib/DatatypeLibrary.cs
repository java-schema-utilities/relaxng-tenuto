namespace Tenuto.Datatype.XSDLib {

using org.relaxng.datatype;
using System.Text;

public class XMLSchemaDatatypeLibrary : DatatypeLibrary {
	
	public static XMLSchemaDatatypeLibrary theInstance =
		new XMLSchemaDatatypeLibrary();
	
	private XMLSchemaDatatypeLibrary() {}
	
	public DatatypeBuilder CreateDatatypeBuilder( string name ) {
		Datatype dt = CreateDatatype(name);
		if(dt==null)	return null;
		return new DatatypeBuilderImpl(dt);
	}
	
	public Datatype CreateDatatype( string name ) {
		if(name=="string")	return StringType.theInstance;
		if(name=="token")	return TokenType.theInstance;
		return null;
	}
}

internal class DatatypeBuilderImpl : DatatypeBuilder {
	
	private readonly Datatype type;
	
	internal DatatypeBuilderImpl( Datatype _type ) {
		this.type = _type;
	}
	
	public void AddParameter( string name, string value, ValidationContext context ) {
		throw new DatatypeException();
	}
	
	public Datatype CreateDatatype() {
		return type;
	}
}

public abstract class BuiltinType : Datatype {
	
	public bool IsValid( string literal, ValidationContext context ) {
		return true;
	}
	
	public void CheckValid( string literal, ValidationContext context ) {
	}
	
	public DatatypeStreamingValidator CreateStreamingValidator( ValidationContext context ) {
		return AlwaysValidStreamingValidator.theInstance;
	}
	
	public int ValueHashCode( object value ) {
		return value.GetHashCode();
	}
	
	public bool SameValue( object value1, object value2 ) {
		return value1==value2;
	}
	
	public abstract object CreateValue( string literal, ValidationContext context );

    public IDType IdType {
        get { return IDType.ID_TYPE_NULL; }
    }

    public bool IsContextDependent {
        get { return false; }
    }
}

public class StringType : BuiltinType {
	
	public static StringType theInstance = new StringType();
	
	private StringType(){}
	
	public override object CreateValue( string literal, ValidationContext context ) {
		return literal;
	}
}

public class TokenType : BuiltinType {
	
	public static TokenType theInstance = new TokenType();
	
	private TokenType(){}
	
	private static string collapse( string s ) {
		StringBuilder buf = new StringBuilder();
		bool inWhiteSpace = true;
		
		foreach( char c in s ) {
			if( char.IsWhiteSpace(c) ) {
				if(!inWhiteSpace)	buf.Append(' ');
				inWhiteSpace = true;
			} else {
				buf.Append(c);
				inWhiteSpace = false;
			}
		}
		// remove trailing whitespace if any.
		// (there must be at most one)
		if(inWhiteSpace && buf.Length>0)	buf.Length--;
		
		return buf.ToString();
	}
	
	public override object CreateValue( string literal, ValidationContext context ) {
		return collapse(literal);
	}
}


internal class AlwaysValidStreamingValidator : DatatypeStreamingValidator {
	
	internal static AlwaysValidStreamingValidator theInstance =
		new AlwaysValidStreamingValidator();
	
	private AlwaysValidStreamingValidator() {}
	
	public void AddCharacters( char[] chs, int start, int len ) {}
	public bool IsValid() { return true; }
	public void CheckValid() {}
}

}
