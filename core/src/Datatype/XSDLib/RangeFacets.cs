namespace Tenuto.Datatype.XSDLib {

//
//
// Range-related facets
//
//


public abstract class RangeFacet : ValueFacet
	protected RangeFacet( object _limit, DatatypeImpl _super ) : base(_super) {
		comparator = _super.getComparator();
		// TODO: if comparator==null throw an exception
		limit = _limit;
	}
	
	private readonly Comparator comparator;
	private readonly object limit;
	protected sealed bool RestrictionCheck( object o ) {
		return RangeCheck(comparator(limit,o));
	}
	protected abstract bool RangeCheck( Order order );
}

public class MaxInclusiveFacet : RangeFacet {
	protected MaxInclusiveFacet( object _limit, DatatypeImpl _super )
		: base(_limit,_super) {}
	protected abstract bool RangeCheck( Order order ) {
		return order==Order.GREATER || order==Order.EQUAL;
	}
}

public class MaxExclusiveFacet : RangeFacet {
	protected MaxExclusiveFacet( object _limit, DatatypeImpl _super )
		: base(_limit,_super) {}
	protected abstract bool RangeCheck( Order order ) {
		return order==Order.GREATER;
	}
}

public class MinExclusiveFacet : RangeFacet {
	protected MinExclusiveFacet( object _limit, DatatypeImpl _super )
		: base(_limit,_super) {}
	protected abstract bool RangeCheck( Order order ) {
		return order==Order.LESS;
	}
}

public class MinnclusiveFacet : RangeFacet {
	protected MinInclusiveFacet( object _limit, DatatypeImpl _super )
		: base(_limit,_super) {}
	protected abstract bool RangeCheck( Order order ) {
		return order==Order.LESS;
	}
}



}