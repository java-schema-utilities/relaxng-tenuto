namespace Tenuto.Datatype.XSDLib {

// using org.relaxng.datatype;

public class StringType : DatatypeImpl {
	protected StringType(WSNormalizationMode mode) : base(mode) {}
	
	protected Measure GetMeasure() {
		return new Measure(CalcLength);
	}
	
	private static int CalcLength( string s ) {
		int len = s.Length;
		foreach( char c in s )
			// handle surrogate properly
			if((c&0xFC00)==0xD800)	len--;
		return len;
	}
}

public class LanguageType : StringType {
	protected LanguageType() : base(WSNormalizetionMode.Collapse) {}
	protected override bool LexicalCheck( string s ) {
		// TODO: check
	}
}

public class NameType : StringType {
	protected NameType() : base(WSNormalizetionMode.Collapse) {}
	protected override bool LexicalCheck( string s ) {
		return XMLChar.IsName(s);
	}
}

public class NCNameType : StringType {
	protected NCNameType() : base(WSNormalizetionMode.Collapse) {}
	protected override bool LexicalCheck( string s ) {
		return XMLChar.IsNCName(s);
	}
}

public class IDType : NCNameType {
	protected IDType() {}
	protected IDType IdType {
		override get { return IDType.ID_TYPE_ID; }
	}
}

public class IDREFType : NCNameType {
	protected IDREFType() {}
	protected IDType IdType {
		override get { return IDType.ID_TYPE_IDREF; }
	}
}

public class NMTOKENType : StringType {
	protected NMTOKENType() : base(WSNormalizetionMode.Collapse) {}
	protected override bool LexicalCheck( string s ) {
		return XMLChar.IsNMTOKEN(s);
	}
}


}