namespace Tenuto.Datatype.XSDLib {

using System.Text.RegularExpressions;


public class DatatypeProxy : DatatypeImpl {
	protected readonly super;
	
	protected DatatypeProxy( DatatypeImpl _super )
		: this(_super,_super.wsProcessor) {}
	
	protected DatatypeProxy( DatatypeImpl _super, WhitespaceNormalier.Processor proc )
		: base(proc) {
		this.super = _super;
	}
	
	protected override bool LexicalCheck( string s ) {
		return super.LexicalCheck(s);
	}
	protected override bool ValueCheck( string s, ValidationContext ctxt ) {
		return super.ValueCheck(s,ctxt);
	}
	protected object GetValue( string s, ValidationContext ctxt ) {
		return super.GetValue(s,ctxt);
	}
	public override int ValueHashCode( object o ) {
		return super.ValueHashCode(o);
	}
	public override bool SameValue( object o1, object o2 ) {
		return super.SameValue(o1,o2);
	}
	protected override Measure getMeasure() {
		return super.getMeasure();
	}
	protected override Comparator getComparator() {
		return super.getComparator();
	}
}

public abstract class Facet : DatatypeProxy {
	protected Facet( DatatypeImpl _super )
		: base(_super) {}
	
	protected Facet( DatatypeImpl _super, WhitespaceNormalier.Processor proc )
		: base(_super,proc) {}
}

public abstract class ValueFacet : Facet {
	protected ValueFacet( DatatypeImpl _super ) : base(_super) {}

	protected override bool ValueCheck( string s, ValidationContext ctxt ) {
		// there is no need to call base.ValueCheck/super.ValueCheck
		return GetValue(s,ctxt)!=null;
	}
	
	protected override object GetValue( string s, ValidationContext ctxt ) {
		object o = base.GetValue(s,ctxt);
		if(o==null)					return null;
		if(!RestrictionCheck(o))	return null;
		return o;
	}
	
	// LexicalCheck must be satisifed. return false if o violates
	// the restriction
	protected abstract bool RestrictionCheck( object o );
}

public class PatternFacet : Facet {
	// TODO: Is RegEx compatible with XML Schema?
	protected PatternFacet( string regexp, DatatypeImpl _super )
		: base(_super) {
		pattern = new RegEx('^'+regexp+'$');
	}
	
	private readonly RegEx pattern;
	protected override bool LexicalCheck( string s ) {
		if(!super.LexicalCheck(s))	return false;
		// TODO: probably we need to test the whole string match
		return pattern.IsMatch(s);
	}
}

}
