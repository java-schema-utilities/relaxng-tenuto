namespace Tenuto {

using System;

public struct XmlName {
	public XmlName( String uri, String local ) {
		this.uri = uri;
		this.local = local;
	}
	public readonly String uri;
	public readonly String local;

	public override bool Equals( object rhs ) {
		if(!(rhs is XmlName))	return false;
		return this==((XmlName)rhs);
	}
	public override int GetHashCode() {
		return uri.GetHashCode() ^ local.GetHashCode();
	}
	
	public static bool operator == ( XmlName lhs, XmlName rhs ) {
		return lhs.uri==rhs.uri && lhs.local==rhs.local;
	}
	public static bool operator != ( XmlName lhs, XmlName rhs ) {
		return !(lhs==rhs);
	}
}

}
