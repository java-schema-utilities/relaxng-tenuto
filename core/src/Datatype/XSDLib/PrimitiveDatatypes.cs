namespace Tenuto.Datatype.XSDLib {

using org.relaxng.datatype;
using org.relaxng.datatype.helpers;
using Tenuto;


// length calculator
internal delegate int Measure( object o );
// order comparison
internal enum Order { LESS, EQUAL, GREATER, UNDECIDABLE };
internal delegate Order Comparator( object o1, object o2 );

// base implementation
internal abstract class DatatypeImpl : Datatype {
	
	protected DatatypeImpl( WSNormalizationMode mode ) {
		wsProcessor = WhitespaceNormalizer.GetProcessor(mode);
	}
	protected DatatypeImpl( WhitespaceNormalizer.Processor wsproc ) {
		wsProcessor = wsproc;
	}
	
	
	// methods implemented by derived classes
	protected virtual bool LexicalCheck( string normalizedStr ) {
		return true;
	}
	protected virtual bool ValueCheck( string normalizedStr, ValidationContext ctxt ) {
		return true;
	}
	protected abstract object GetValue( string normalizedStr, ValidationContext ctxt );
	
	public sealed bool IsValid( string str, ValidationContext ctxt ) {
		str = wsProcessor(str);
		return LexicalCheck(str) && ValueCheck(str,ctxt);
	}
	
	public sealed void CheckValid( string str, ValidationContext ctxt ) {
		if(!IsValid(str,ctxt))
			throw new DatatypeException();
	}
	
	public sealed object CreateValue( string str, ValidationContext ctxt ) {
		str = wsProcessor(str);
		if(!LexicalCheck(str))	return null;
		return GetValue(str,ctxt);
	}
	
	public bool IsContextDependent {
		get { return false; }
	}
	
	public IDType IdType {
		get { return IDType.ID_TYPE_NULL; }
	}
	
	public int ValueHashCode( object o ) {
		return o.GetHashCode();
	}
	
	public bool SameValue( object o1, object o2 ) {
		return o1.Equals(o2);
	}
	
	public sealed DatatypeStreamingValidator CreateStreamingValidator( ValidationContext ctxt ) {
		return new StreamingValidatorImpl(this,ctxt);
	}
	
	
	//
	// whitespace handling
	//
	
	public readonly WhitespaceNormalizer.Processor wsProcessor;
	
	
	protected virtual Measure getMeasure() { return null; }
	protected virtual Comparator getComparator() { return null; }
}



public class BooleanType : DatatypeImpl {
	
	public static BooleanType theInstance = new BooleanType();
	
	protected BooleanType() : base(WSNormalizationMode.Collapse) {}
	
	public bool LexicalCheck( string s ) {
		return s=="true" || s=="false" || s=="0" || s=="1";
	}
	
	public object GetValue( string s, ValidationContext ctxt ) {
		char ch = s.CharAt(0);
		if(ch=='t' || ch=='1')	return true;
		else					return false;
	}
}



public class QNameType : DatatypeImpl {
	public static QNameType theInstance = new QNameType();
	
	protected QNameType() : base(WSNormalizedMode.Collapse) {}
	
	protected bool LexicalCheck( string s ) {
		return XmlChar.IsQName(s);
	}
	
	protected bool ValueCheck( string s, ValidationContext ctxt ) {
		int idx = s.IndexOf(':');
		if(idx<0)	return true;
		return ctxt.ResolveNamespacePrefix(s.Substring(0,idx))!=null;
	}
	protected object GetValue( string s, ValidationContext ctxt ) {
		int idx = s.IndexOf(':');
		if(idx<0) {
			string uri = ctxt.ResolveNamespacePrefix("");
			if(uri==null)	uri="";
			return new XmlName(uri,s);
		} else {
			string uri = ctxt.ResolveNamespacePrefix(s.Substring(0,idx));
			if(uri==null)	return null;
			return new XmlName(uri,s.Substring(idx+1));
		}
	}
}

}
