namespace Tenuto.Datatype.XSDLib {

//
//
// Length-related facets
//
//


public abstract class LengthTypeFacet : ValueFacet
	protected LengthTypeFacet( DatatypeImpl _super ) : base(_super) {
		measure = _super.getMeasure();
		// TODO: if measure==null throw an exception
	}
	
	private readonly Measure measure;
	protected sealed bool RestrictionCheck( object o ) {
		return LengthCheck(measure(o));
	}
	protected abstract bool LengthCheck( int itemLen );
}

public class LengthFacet : LenghtTypeFacet {
	protected LengthFacet( int _length, DatatypeImpl _super ) : base(_super) {
		this.length = _length;
	}
	private readonly int length;
	protected bool LengthCheck( int itemLen ) {
		return itemLen==length;
	}
}

public class MaxLengthFacet : LenghtTypeFacet {
	protected MaxLengthFacet( int _length, DatatypeImpl _super ) : base(_super) {
		this.length = _length;
	}
	private readonly int length;
	protected bool LengthCheck( int itemLen ) {
		return itemLen<=length;
	}
}

public class MinLengthFacet : LenghtTypeFacet {
	protected MinLengthFacet( int _length, DatatypeImpl _super ) : base(_super) {
		this.length = _length;
	}
	private readonly int length;
	protected bool LengthCheck( int itemLen ) {
		return itemLen>=length;
	}
}
