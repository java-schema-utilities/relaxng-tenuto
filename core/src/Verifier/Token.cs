namespace Tenuto.Verifier {

using System;
using Tenuto.Grammar;
using org.relaxng.datatype;

public interface Token {
	bool Accepts( ListExp exp );
//	bool Accepts( KeyExp exp );
	bool Accepts( ElementExp exp );
	bool Accepts( AttributeExp exp );
	bool Accepts( DataExp exp );
	bool Accepts( ValueExp exp );
	bool AcceptsText();
}

internal class TokenImpl : Token {
	public virtual bool Accepts( ListExp exp ) { return false; }
//	public virtual bool Accepts( KeyExp exp ) { return false; }
	public virtual bool Accepts( DataExp exp ) { return false; }
	public virtual bool Accepts( ValueExp exp ) { return false; }
	public virtual bool AcceptsText() { return false; }
	public virtual bool Accepts( ElementExp exp ) { return false; }
	public virtual bool Accepts( AttributeExp exp ) { return false; }
}

internal class StringToken : TokenImpl {

	internal StringToken( string literal, ExpBuilder builder, ValidationContext context ) {
		this.literal = literal;
		this.builder = builder;
		this.context = context;
	}
	
	private readonly string literal;
	private StringToken[] listItems = null;
	
	private readonly ExpBuilder builder;
	private readonly ValidationContext context;
	
	public override bool Accepts( ListExp exp ) {
		if( listItems==null ) {
			// split the literal by whitespace.
			string[] tokens = literal.Split(null);
			listItems = new StringToken[tokens.Length];
			for( int i=0; i<tokens.Length; i++ )
				listItems[i] = new StringToken(tokens[i],builder,context);
		}
		
		Expression body = exp.exp;
		for( int i=0; i<listItems.Length; i++ ) {
			body = Residual.Calc( body, listItems[i], builder );
			if( body==Expression.NotAllowed )	return false;
		}
		return body.IsNullable;
	}
	
/*	public override bool Accepts( KeyExp exp ) {
		if( literal.Trim().Length==0 )	return false;
		// reporting the key should be done by the caller.
		return Residual.Calc( exp.exp, this, builder ).IsEpsilonReducible;
	}
*/	
	public override bool Accepts( DataExp exp ) {
		return exp.dt.IsValid(literal,context);
	}
	
	public override bool Accepts( ValueExp exp ) {
		object o = exp.dt.CreateValue(literal,context);
		if(o==null)		return false;
		return exp.dt.SameValue(o,exp.value);
	}
	
	public override bool AcceptsText() {
		return true;
	}
}

internal class ElementToken : TokenImpl {
	
	internal ElementToken( ElementExp[] matched, int len ) {
		this.matched = matched;
		this.len = len;
	}
	
	private readonly ElementExp[] matched;
	private readonly int len;
	
	public override bool Accepts( ElementExp exp ) {
		return Array.IndexOf(matched,exp,0,len)>=0;
	}
}

internal class AttributeToken : TokenImpl {
	
	internal AttributeToken( string uri, string localName, string value, ValidationContext context, ExpBuilder builder ) {
		this.uri = uri;
		this.localName = localName;
		this.value = value;
		this.context = context;
		this.builder = builder;
	}
	
	private readonly string uri;
	private readonly string localName;
	private readonly string value;
	private readonly ValidationContext context;
	private readonly ExpBuilder builder;
	private StringToken valueToken = null;
	
	public override bool Accepts( AttributeExp exp ) {
		if( !exp.Name.Contains(uri,localName) )		return false;
		
		if( valueToken==null )
			valueToken = new StringToken( value, builder, context );
		
		return Residual.Calc( exp.exp, valueToken, builder ).IsNullable;
	}
}



}
