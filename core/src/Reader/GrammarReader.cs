namespace Tenuto.Reader {

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Xml;
using Tenuto.Grammar;
using org.relaxng.datatype;

/*

	TODO: there seems to be a bug in the "xml" namespace handling.
	XmlReader doesn't consider the "xml" prefix to be bounded to
	"http://www.w3.org/XML/1998/namespace".
*/
public class GrammarReader : ValidationContext {
	
	public Grammar parse( string sourceURL ) {
		
		try {
			new Uri(sourceURL);
		} catch( UriFormatException ) {
			sourceURL = Path.GetFullPath(sourceURL);
		}
		
		return parse( new XmlTextReader(sourceURL), sourceURL );
//			(Stream)new XmlUrlResolver().GetEntity( new Uri(sourceURL), null, typeof(Stream) 	}
	}
	
//	public Grammar parse( Stream source ) {
//		return parse( new XmlTextReader(source) );
//	}
	
	public Grammar parse( XmlReader reader, string systemId ) {
		
		XmlReader oldReader = this.reader;
		string oldSystemId = this.systemId;
		
		this.reader = reader;
		this.systemId = systemId;
		try {
			// skip xml declaration, DOCTYPE etc.
			while(!reader.IsStartElement())	reader.Read();
			
			HadError = false;
			Expression exp = ReadExp();
			if( HadError )	return null;	// there was an error.
			
			if( exp is Grammar )
				return (Grammar)exp;
			else {
				Grammar g = new Grammar(Builder);
				g.exp = exp;
				return g;
			}
		} finally {
			this.reader = oldReader;
			this.systemId = oldSystemId;
		}
	}
	
//
// publicly accessible parameters
//========================================
// these parameters affects various aspects of the parsing process.
//
	// this controller receives error/warning messages
	public GrammarReaderController Controller;
	// this object is used to construct the grammar.
	public ExpBuilder Builder;
	// this resolver is used to resolve external references.
	public XmlResolver Resolver = new XmlUrlResolver();
	// string messages are resolved through this object.
	public ResourceManager ResManager;
	
	
	public GrammarReader( GrammarReaderController controller )
		: this(controller, new ExpBuilder()) {}
	
	
	protected XmlReader reader;
	protected string systemId;
	protected Grammar grammar;
	
	// RELAX NG namespace
	public const string RELAXNGNamespace = "http://relaxng.org/ns/structure/1.0";
	
	public GrammarReader( GrammarReaderController controller, ExpBuilder builder ) {
		this.Controller = controller;
		this.Builder = builder;
//		this.ResManager = new ResourceManager(this.GetType());		
		this.ResManager = new ResourceManager("GrammarReader",this.GetType().Assembly);
		
		{
			// derived classes can set additional ExpReader directly to
			// the ExpReaders field.
			IDictionary dic = new Hashtable();
			dic["notAllowed"]	= new ExpReader(NotAllowed);
			dic["empty"]		= new ExpReader(Empty);
			dic["group"]		= new ExpReader(Group);
			dic["choice"]		= new ExpReader(Choice);
			dic["interleave"]	= new ExpReader(Interleave);
			dic["optional"]		= new ExpReader(Optional);
			dic["zeroOrMore"]	= new ExpReader(ZeroOrMore);
			dic["oneOrMore"]	= new ExpReader(OneOrMore);
			dic["mixed"]		= new ExpReader(Mixed);
			dic["list"]			= new ExpReader(List);
			dic["element"]		= new ExpReader(Element);
			dic["attribute"]	= new ExpReader(Attribute);
			dic["externalRef"]	= new ExpReader(ExternalRef);
			dic["ref"]			= new ExpReader(Ref);
			dic["parentRef"]	= new ExpReader(ParentRef);
			dic["grammar"]		= new ExpReader(GrammarElm);
			dic["data"]			= new ExpReader(Data);
			dic["value"]		= new ExpReader(Value);
			dic["text"]			= new ExpReader(Text);
			ExpReaders = dic;
		}
		{
			IDictionary dic = new Hashtable();
			dic["choice"]	= new NCReader(ChoiceName);
			dic["name"]		= new NCReader(SimpleName);
			dic["nsName"]	= new NCReader(NsName);
			dic["anyName"]	= new NCReader(AnyName);
			NCReaders = dic;
		}
		
		nsStack.Push("");
		dtLibURIStack.Push("");
		xmlBaseStack.Push(null);
	}
	
//
// XmlReader-related low level utility methods
//================================================
// To handle "ns" and "datatypeLibrary" attributes,
// reader.ReadStartElement and reader.ReadEndElement methods may not be
// directly called. Instead, methods defined in this class should be called.
	private readonly Stack nsStack = new Stack();
	private readonly Stack dtLibURIStack = new Stack();
	
	// at least with Beta2 SDK, XmlReader.BaseURI doesn't seem to work
	private readonly Stack xmlBaseStack = new Stack();
	
	/**
	 * return false if this element is an empty element
	 */
	protected bool ReadStartElement() {
		Trace.WriteLine("read <"+reader.Name+">");
		
		string ns = reader.GetAttribute("ns");
		if(ns==null)		ns=(string)nsStack.Peek();
		nsStack.Push(ns);
		
		bool isEmpty = reader.IsEmptyElement;
		
		string dtLibURI = reader.GetAttribute("datatypeLibrary");
		string xmlBase = reader.GetAttribute("xml:base");
		if(dtLibURI==null)
			dtLibURI=(string)dtLibURIStack.Peek();
		if(xmlBase==null)
			xmlBase=(string)xmlBaseStack.Peek();
			
		if(!isEmpty) {
			dtLibURIStack.Push(dtLibURI);
			xmlBaseStack.Push(xmlBase);
		}
		reader.ReadStartElement();
		return !isEmpty;
	}
	
	protected void ReadEndElement() {
		Trace.WriteLine("read </"+reader.Name+">");
		
		nsStack.Pop();
		dtLibURIStack.Pop();
		xmlBaseStack.Pop();
		reader.ReadEndElement();
	}
	
	// gets the propagated value of the ns attribute.
	protected string ns {
		get {
			string ns = reader.GetAttribute("ns");
			if(ns!=null)	return ns;
			else			return (string)nsStack.Peek();
		}
	}
	protected string xmlBase {
		get {
			string v = reader.GetAttribute("xml:base");
			if(v!=null)		return v;
			else			return (string)xmlBaseStack.Peek();
		}
	}
	protected string datatypeLibraryURI {
		get {
			string uri = reader.GetAttribute("datatypeLibrary");
			if(uri!=null)	return uri;
			else			return (string)dtLibURIStack.Peek();
		}
	}
	protected DatatypeLibrary datatypeLibrary {
		get {
			// TODO: use a map to cache DatatypeLibrary object
			return ResolveDatatypeLibrary(datatypeLibraryURI);
		}
	}
	
	// resolves the datatypeLibrary attribute value to a DatatypeLibrary object.
	protected virtual DatatypeLibrary ResolveDatatypeLibrary( string uri ) {
		if(uri.Equals(""))
			return Tenuto.Datatype.DatatypeLibraryImpl.theInstance;
		throw new Exception(uri);
	}
	
	private class InvalidQNameException : Exception {}
	// converts a QName to an XmlName.
	// throws an InvalidQNameException when qname contains undeclared prefix.
	protected XmlName ProcessQName( string qname ) {
		int idx = qname.IndexOf(':');
		if(idx<0)	return new XmlName(ns,qname);	// no prefix
		
		string uri = reader.LookupNamespace(qname.Substring(0,idx));
		if(uri==null)	throw new InvalidQNameException();
		return new XmlName(uri,qname.Substring(idx+1));
	}
	
	
	
	// skips any foreign elements
	// return true if XmlReader is on the start tag.
	// return false if XmlReader is on the end tag.
	protected bool SkipForeignElements() {
		while(reader.IsStartElement()) {
			if(reader.NamespaceURI==RELAXNGNamespace)
				return true;	// found a RELAX NG element.
			Trace.WriteLine("skip an element {"+reader.NamespaceURI+"}"+reader.LocalName );
			// foreign element. skip it.
			reader.Skip();
		}
		// found an end tag.
		return false;
	}
	
	// makes sure that the current element has no RELAX NG child elements.
	protected void EmptyContent() {
		Trace.WriteLine("EmptyContent()");
		if(!ReadStartElement()) {
			Trace.WriteLine("</>");
			return;	// empty content
		}
		// skip any elements from forein namespace
		while(SkipForeignElements()) {
			// elements from RELAX NG namespace. Error.
			ReportError( ERR_UNEXPECTED_ELEMENT, reader.Name );
			reader.Skip();
		}
		
		// TODO: how can we detect literal strings?
		ReadEndElement();
	}
	
	/**
	 * reads text inside an element and returns it.
	 * 
	 * the caller should call the ReadStartElement before
	 * calling this method. null is returned when an error occurs.
	 */
	protected string ReadPCDATA() {
		string s  = "";
		bool hadError = false;
		while(true) {
			switch(reader.NodeType) {
			case XmlNodeType.CDATA:
			case XmlNodeType.Text:
			case XmlNodeType.SignificantWhitespace:
			case XmlNodeType.Whitespace:
				s += reader.Value;
				reader.Read();
				break;
			
			case XmlNodeType.EndElement:
				if(hadError)	return null;
				else			return s;
			
			case XmlNodeType.Element:
				ReportError( ERR_UNEXPECTED_ELEMENT, reader.Name );
				hadError = true;
				reader.Skip();
				break;
			default:
				throw new Exception();
			}
		}
	}
	
	protected string GetRequiredAttribute( string attrName ) {
		string r = reader.GetAttribute(attrName);
		if(r!=null)		return r;
		
		ReportError( ERR_MISSING_ATTRIBUTE, reader.Name, attrName );
		return null;
	}
	
	
//
// Expression Element Reader
//===================================================
// Each method parses one element (and its descendants) and returns an Expression.
// These methods assume that XmlReader is on a start tag when it's called.
// When the method returns, XmlReader is on a start tag of the next element.
	
	// XmlReader is on a start tag. Parses it and returns the result.
	protected delegate Expression ExpReader();
	protected readonly IDictionary ExpReaders;
	
	
	// XmlReader is on a start tag. Parses it and returns the result.
	protected virtual Expression ReadExp() {
		Trace.WriteLine("dispatching :"+reader.LocalName);
		ExpReader expreader = (ExpReader)ExpReaders[reader.LocalName];
		if(expreader==null) {
			// error: unknown element name
			ReportError( ERR_EXPRESSION_EXPECTED, reader.Name );
			reader.Skip();
			return Expression.NotAllowed;	// recover
		}
		
		return expreader();	// dispatch the reader.
	}
	
	
	protected virtual Expression Text() {
		EmptyContent();
		return Expression.Text;
	}
	protected virtual Expression Empty() {
		EmptyContent();
		return Expression.Empty;
	}
	protected virtual Expression NotAllowed() {
		EmptyContent();
		return Expression.NotAllowed;
	}
	protected virtual Expression ExternalRef() {
		XmlReader previous = reader;
		string href = GetRequiredAttribute("href");
		
		// this method has to be called while we are at the start element.
		XmlReader newReader = ResolveEntity(href);
		
		EmptyContent();
		if(href==null)
			return Expression.NotAllowed;
		
		reader = newReader;
		try {
			// skip XML declarations, DOCTYPE, etc.
			while(!reader.IsStartElement())	reader.Read();
			return ReadExp();	// parse it.
		} finally {
			reader.Close();
			reader = previous;
		}
	}
	protected virtual Expression Ref() {
		string name = GetRequiredAttribute("name");
		EmptyContent();
		if(name==null)
			// error: missing attribute
			return Expression.NotAllowed;
		
		if(grammar==null) {
			// error: no-enclosing grammar
			ReportError( ERR_NO_GRAMMAR );
			return Expression.NotAllowed;
		}
		ReferenceExp exp = grammar.GetOrCreate(name);
		MemorizeReference(exp);
		return exp;
	}
	protected virtual Expression ParentRef() {
		string name = GetRequiredAttribute("name");
		EmptyContent();
		if(name==null)
			// error: missing attribute
			return Expression.NotAllowed;
		
		if(grammar==null || grammar.Parent==null) {
			// error: no-enclosing grammar
			ReportError( ERR_NO_GRAMMAR );
			return Expression.NotAllowed;
		}
		ReferenceExp exp = grammar.Parent.GetOrCreate(name);
		MemorizeReference(exp);
		return exp;
	}
	protected virtual Expression Group() {
		return ReadContainerExp( new ExpCombinator(Builder.CreateSequence) );
	}
	protected virtual Expression Interleave() {
		return ReadContainerExp( new ExpCombinator(Builder.CreateInterleave) );
	}
	protected virtual Expression Choice() {
		return ReadContainerExp( new ExpCombinator(Builder.CreateChoice) );
	}
	protected virtual Expression ReadContainerExp( ExpCombinator combinator ) {
		string name = reader.Name;
		if(ReadStartElement())
			return ReadChildExps( combinator );
		// no children. error
		ReportError( ERR_EXPRESSION_EXPECTED, name );
		return Expression.NotAllowed;
	}
	protected virtual Expression OneOrMore() {
		return Builder.CreateOneOrMore(Group());
	}
	protected virtual Expression ZeroOrMore() {
		return Builder.CreateZeroOrMore(Group());
	}
	protected virtual Expression Optional() {
		return Builder.CreateOptional(Group());
	}
	protected virtual Expression List() {
		return Builder.CreateList(Group());
	}
	protected virtual Expression Mixed() {
		return Builder.CreateMixed(Group());
	}
	protected virtual Expression Element() {
		bool isEmpty;
		NameClass nc = ReadNameClassOrNameAttr(out isEmpty);
		if(isEmpty) {
			ReportError( ERR_EXPRESSION_EXPECTED );
			return Expression.NotAllowed;
		}
		Expression contents = ReadChildExps( new ExpCombinator(Builder.CreateSequence) );
		return new ElementExp(nc,contents);
	}
	protected virtual Expression Attribute() {
		bool isEmpty;
		NameClass nc = ReadNameClassOrNameAttr(out isEmpty);
		Expression contents = Expression.Text;
		
		if(!isEmpty) {
			if( SkipForeignElements() )
				contents = ReadChildExps( new ExpCombinator(Builder.CreateSequence) );
		}
		return new AttributeExp(nc,contents);
	}
	protected virtual NameClass ReadNameClassOrNameAttr( out bool isEmpty ) {
		string name = reader.GetAttribute("name");
		isEmpty = !ReadStartElement();
		if(name!=null) {
			return new SimpleNameClass(ProcessQName(name));
		}
		return ReadNameClass();
	}

	protected virtual Expression Value() {
		Datatype dt;
		string type = reader.GetAttribute("type");
		if(type==null)
			dt = Tenuto.Datatype.TokenType.theInstance;
		else {
			try {
				dt = datatypeLibrary.CreateDatatype(type);
			} catch( DatatypeException e ) {
				ReportError( ERR_UNDEFINED_TYPENAME, type, e.Message );
				reader.Skip();
				return Expression.NotAllowed;
			}
		}
		
		try {
			object value;
			
			if(!ReadStartElement())
				value = dt.CreateValue("",this);
			else {
				string s = ReadPCDATA();
				ReadEndElement();
				
				if(s==null)		return Expression.NotAllowed;
				value = dt.CreateValue(s,this);
			}
		
			return Builder.CreateValue(dt,value);
		} catch( DatatypeException ) {
			ReportError( ERR_BAD_VALUE_FOR_TYPE );
			return Expression.NotAllowed;
		}
	}

	protected virtual Expression Data() {
		string type = reader.GetAttribute("type");
		if(type==null) {
			ReportError( ERR_MISSING_ATTRIBUTE, reader.Name, "type" );
			reader.Skip();
			return Expression.NotAllowed;
		}
		
		DatatypeBuilder builder;
		try {
			builder = datatypeLibrary.CreateDatatypeBuilder(type);
		} catch( DatatypeException e ) {
			ReportError( ERR_UNDEFINED_TYPENAME, type, e.Message );
			reader.Skip();
			return Expression.NotAllowed;
		}
		
		Expression except=null;
		
		if(ReadStartElement()) {
			// if the element has contents, parse them.
			while(SkipForeignElements()) {
				string name = reader.LocalName;
				if(name=="param")
					DataParam(builder);
				else
				if(name=="except") {
					if( except!=null ) {
						// only one "except" clause is allowed
						ReportError( ERR_MULTIPLE_EXCEPT );
						reader.Skip();
					} else {
						except = Choice();
					}
				} else {
					// error: unexpected element
					ReportError( ERR_EXCEPT_EXPECTED, reader.Name );
					reader.Skip();
				}
			}
			ReadEndElement();
		}
		
		try {
			// derive a type
			return Builder.CreateData(
				builder.CreateDatatype(), except, type );
		} catch( DatatypeException e ) {
			ReportError( ERR_DATATYPE_ERROR, type, e.Message );
			return Expression.NotAllowed;
		}
	}
	protected virtual void DataParam( DatatypeBuilder builder ) {
		string name = GetRequiredAttribute("name");
		if(name==null) {
			EmptyContent();
			return;
		}
		
		try {
			string value = ReadPCDATA();
			if(value==null)	return;
			builder.AddParameter(name,value,this);
		} catch( DatatypeException e ) {
			ReportError( ERR_BAD_DATATYPE_PARAMETER, name, e.Message );
		}
	}
	
	protected virtual Expression GrammarElm() {
		Grammar n = new Grammar(grammar,Builder);
		grammar = n;
		DivInGrammar();
		grammar = grammar.Parent;
		return n;
	}
	protected virtual void DivInGrammar() {
		if(!ReadStartElement())		return;
		
		while(SkipForeignElements()) {
			string name = reader.LocalName;
			if(name=="div")		DivInGrammar();
			else
			if(name=="start")	Start();
			else
			if(name=="define")	Define();
			else
			if(name=="include")	MergeGrammar();
			else {
				// error: unexpected element
				ReportError( ERR_UNEXPECTED_ELEMENT, reader.Name );
				reader.Skip();
			}
		}
		ReadEndElement();
	}
	protected virtual ReferenceExp Start() {
		string combine = reader.GetAttribute("combine");
		Expression exp = null;
		if(ReadStartElement()) {
			while(SkipForeignElements()) {
				if(exp==null)	exp = ReadExp();
				else {
					// error: unexpected element. Only one child is allowed.
					ReportError( ERR_UNEXPECTED_ELEMENT, reader.Name );
					reader.Skip();
				}
			}
			ReadEndElement();
		} else {
			ReportError( ERR_EXPRESSION_EXPECTED );
			exp = Expression.NotAllowed;
		}
		CombineReferenceExp(grammar,exp,combine);
		return grammar;
	}
	protected virtual ReferenceExp Define() {
		string combine = reader.GetAttribute("combine");
		string name = reader.GetAttribute("name");
		if(name==null) {
			// error: missing attribute
			ReportError( ERR_MISSING_ATTRIBUTE, reader.Name, "name" );
			reader.Skip();
			return null;
		}
		Expression body = Group();	// parse the body.
		
		ReferenceExp exp = grammar.GetOrCreate(name);
		CombineReferenceExp(exp,body,combine);
		return exp;
	}
	
	private readonly Hashtable refParseInfos = new Hashtable();
	private class RefParseInfo {
		public string Combine;
		public bool HeadDefined;
		
		// this location specifies one of the referer to this expression
		public int LineNumber = -1;
		public int LinePosition = -1;
		public string SourceFile = null;
		public void MemorizeReference( XmlReader reader ) {
			if( reader is XmlTextReader ) {
				// source information is available only when we are using XmlTextReader
				XmlTextReader r = (XmlTextReader)reader;
				LineNumber = r.LineNumber;
				LinePosition = r.LinePosition;
				SourceFile = r.BaseURI;	// TODO: is this correct?
			}
		}
	}
	protected void MemorizeReference( ReferenceExp exp ) {
		RefParseInfo pi = (RefParseInfo)refParseInfos[exp];
		if(pi==null)
			refParseInfos[exp] = pi = new RefParseInfo();
		pi.MemorizeReference(reader);
	}
	
	protected virtual void CombineReferenceExp(
				ReferenceExp r, Expression body, string combine ) {
		if( redefiningRefExps.ContainsKey(r) ) {
			// this pattern is currently being redefined.
			redefiningRefExps[r] = true;
			return;
		}
		
		if(combine!=null)		combine = combine.Trim();
		
		RefParseInfo pi = (RefParseInfo)refParseInfos[r];
		if(pi==null)	refParseInfos[r] = pi = new RefParseInfo();
		
		if( pi.Combine!=null && pi.Combine==combine ) {
			// error: inconsistent combine method
			ReportError( ERR_INCONSISTENT_COMBINE, r.name );
			pi.Combine = null;
			return;
		}
		if( combine==null ) {
			if( pi.HeadDefined )
				// error: multiple heads
				ReportError( ERR_MULTIPLE_HEADS, r.name );
			pi.HeadDefined = true;
			combine = pi.Combine;
		} else {
			pi.Combine = combine;
		}
		
		if( r.exp==null )
			r.exp = body;
		else {
			if(combine=="interleave")	r.exp = Builder.CreateInterleave( r.exp, body );
			else
			if(combine=="choice")		r.exp = Builder.CreateChoice( r.exp, body );
			else {
				// error: invalid combine value
				ReportError( ERR_INVALID_COMBINE, combine );
			}
		}
	}
	
	// A set of ReferenceExps which are designated as being redefined.
	// This set is prepared by the MergeGrammar method, to check that
	// redefined expressions are in fact defined in the merged grammar.
	private Hashtable redefiningRefExps = new Hashtable();
	
	protected virtual void MergeGrammar() {
		string href = reader.GetAttribute("href");
		if(href==null) {
			// error: missing attribute
			ReportError( ERR_MISSING_ATTRIBUTE, reader.Name, "href" );
			reader.Skip();
			return;
		}
		
		// collect redefined patterns.
		DivInInclude();
		
		XmlReader previous = reader;
		reader = ResolveEntity(href);
		Hashtable old = redefiningRefExps;
		try {
			// skip XML declarations, DOCTYPE, etc.
			while(!reader.IsStartElement())	reader.Read();
			
			if(reader.LocalName!="grammar") {
				// error: unexpected tag name
				ReportError( ERR_GRAMMAR_EXPECTED, reader.Name );
				return;
			}
			
			redefiningRefExps = new Hashtable();
			
			// parse included pattern.
			DivInGrammar();
			
			// make sure that there were definitions for redefined exps.
			foreach( ReferenceExp exp in redefiningRefExps.Keys )
				if( (bool)redefiningRefExps[exp] == false ) {
					// error: this pattern was not defined.
					if( exp is Grammar )
						ReportError( ERR_REDEFINING_UNDEFINED_START );
					else
						ReportError( ERR_REDEFINING_UNDEFINED, exp.name );
				}
		} finally {
			reader.Close();
			reader = previous;
			redefiningRefExps = old;
		}
	}
	protected virtual void DivInInclude() {
		if(!ReadStartElement())		return;
		while(SkipForeignElements()) {
			string name = reader.LocalName;
			if(name=="div")		DivInInclude();
			else
			if(name=="start")	redefiningRefExps[Start()] = false;
			else
			if(name=="define")	redefiningRefExps[Define()] = false;
			else {
				// error: unexpected element
				ReportError( ERR_UNEXPECTED_ELEMENT, reader.Name );
				reader.Skip();
			}
		}
		ReadEndElement();
	}
	
	
	// the function object that combines two expressions into one.
	protected delegate Expression ExpCombinator( Expression exp1, Expression exp2 );
	
	// reads child expressions and combines them by using the specified
	// combinator.
	protected virtual Expression ReadChildExps( ExpCombinator comb ) {
		Expression exp = null;
		
		while(SkipForeignElements()) {
			Expression child=ReadExp();
			if(exp==null)	exp=child;
			else
				exp = comb( exp, child );
		}
		ReadEndElement();
		if(exp==null) {
			// error: no children
			ReportError( ERR_EXPRESSION_EXPECTED );
			exp = Expression.NotAllowed;	// recovery.
		}
		return exp;
	}
	
	
	
	// resolves the "href" value and obtains XmlReader that reads that source.
	protected virtual XmlReader ResolveEntity( string href ) {
		
		// at least with SDK beta2, XmlReader.BaseUri is not working.
		Trace.WriteLine(systemId);
		Uri uri = new Uri(systemId);
		string xmlBase = this.xmlBase;
		if(xmlBase!=null)
			uri = Resolver.ResolveUri( uri, xmlBase );
		
		uri = Resolver.ResolveUri( uri, href );
		
		Trace.WriteLine(string.Format(
			"SystemId:{0} base:{1} href:{2}\nentity uri:{3}",
			systemId,xmlBase,href,uri));
		
		return new XmlTextReader((Stream)Resolver.GetEntity(
			uri, null, typeof(Stream)));
	}
	
	
	
//
// Name Class Element Reader
//===================================================
// Each method parses one element (and its descendants) and returns a NameClass.
// These methods assume that XmlReader is on a start tag when it's called.
// When the method returns, XmlReader is on a start tag of the next element.
	
	protected delegate NameClass NCReader();
	protected readonly IDictionary NCReaders;
	
	// XmlReader is on a start tag. Parses it and returns the result.
	protected virtual NameClass ReadNameClass() {
		if(SkipForeignElements()) {
			Trace.WriteLine("dispatching NC: "+reader.LocalName+")");
			
			NCReader ncreader = (NCReader)NCReaders[reader.LocalName];
			if(ncreader==null) {
				// error: unknown element name
				ReportError( ERR_NAMECLASS_EXPECTED, reader.Name );
				reader.Skip();
				return new SimpleNameClass("foo","bar");	// recover
			}
			
			return ncreader();	// dispatch the reader.
		} else {
			// there is no child element
			ReportError( ERR_MISSING_ATTRIBUTE, reader.Name, "name" );
			return AnyNameClass.theInstance;
		}
	}
	
	
	
	protected virtual NameClass AnyName() {
		return new AnyNameClass(ReadExceptName());
	}
	protected virtual NameClass NsName() {
		return new NsNameClass(ns,ReadExceptName());
	}
	protected virtual NameClass SimpleName() {
		string name = ReadPCDATA();
		if(name==null)	name="undefined";
		try {
			return new SimpleNameClass(ProcessQName(name));
		} catch( InvalidQNameException ) {
			// error: invalid prefix
			ReportError( ERR_USING_UNDECLARED_PREFIX, name );
			return new SimpleNameClass("foo","bar");	// recover
		}
	}
	protected virtual NameClass ChoiceName() {
		NameClass nc=null;
		
		if(ReadStartElement()) {
			while(SkipForeignElements()) {
				NameClass child = ReadNameClass();
				if(nc==null)	nc=child;
				else			nc=new ChoiceNameClass(nc,child);
			}
			ReadEndElement();
		}
		if(nc==null) {
			// error: no children
			ReportError( ERR_NO_CHILD_NAMECLASS );
			nc = new SimpleNameClass("foo","bar");	// recover
		}
		return nc;
	}
	
	// reads <except> clause if if exists. Otherwise returns null.
	// This method skips any foreign elements.
	protected virtual NameClass ReadExceptName() {
		NameClass nc = null;
		
		if(ReadStartElement()) {
			while(SkipForeignElements()) {
				if(reader.LocalName=="except") {
					if(nc!=null)
						// error: multiple except
						ReportError( ERR_MULTIPLE_EXCEPT );
					nc = ChoiceName();
				} else {
					// error: unexpected elements
					ReportError( ERR_EXCEPT_EXPECTED, reader.Name );
					reader.Skip();
				}
			}
		}
		return nc;	// return null if <except> was not found.
	}

//
// ValidationContext implementation
//==============================
//
	public string ResolveNamespacePrefix( string prefix ) {
		return reader.LookupNamespace(prefix);
	}
	public bool IsUnparsedEntity( string str ) {
		// TODO: proper implementation
		return true;
	}
	public bool IsNotation( string str ) {
		// TODO: proper implementation
		return true;
	}

//
// Error message handling
//==============================
//
	protected bool HadError;
	
	protected void ReportError( string propKey, params object[] args ) {
		HadError = true;
		Controller.error( string.Format( ResManager.GetString(propKey), args ), reader );
	}
	
	protected const string ERR_MISSING_ATTRIBUTE = // arg:2
		"GrammarReader.MissingAttribute";
	protected const string ERR_NO_GRAMMAR =
		"GrammarReader.NoGrammar";
	protected const string ERR_UNEXPECTED_ELEMENT =
		"GrammarReader.UnexpectedElement";
	protected const string ERR_EXPRESSION_EXPECTED =
		"GrammarReader.ExpressionExpected";
	protected const string ERR_INCONSISTENT_COMBINE =
		"GrammarReader.InconsistentCombine";
	protected const string ERR_MULTIPLE_HEADS =
		"GrammarReader.MultipleHeads";
	protected const string ERR_INVALID_COMBINE =
		"GrammarReader.InvalidCombine";
	protected const string ERR_GRAMMAR_EXPECTED =
		"GrammarReader.GrammarExpected";
	protected const string ERR_REDEFINING_UNDEFINED_START =
		"GrammarReader.RedefiningUndefinedStart";
	protected const string ERR_REDEFINING_UNDEFINED =
		"GrammarReader.RedefiningUndefined";
	protected const string ERR_NAMECLASS_EXPECTED =
		"GrammarReader.NameClassExpected";
	protected const string ERR_USING_UNDECLARED_PREFIX =
		"GrammarReader.UsingUndeclaredPrefix";
	protected const string ERR_NO_CHILD_NAMECLASS =
		"GrammarReader.NoChildNameClass";
	protected const string ERR_MULTIPLE_EXCEPT =
		"GrammarReader.MultipleExcept";
	protected const string ERR_EXCEPT_EXPECTED =
		"GrammarReader.ExceptExpected";
	protected const string ERR_UNDEFINED_TYPENAME =
		"GrammarReader.UndefinedTypeName";
	protected const string ERR_BAD_VALUE_FOR_TYPE =
		"GrammarReader.BadValueForType";
	protected const string ERR_DATATYPE_ERROR =
		"GrammarReader.DatatypeError";
	protected const string ERR_BAD_DATATYPE_PARAMETER =
		"GrammarReader.BadDatatypeParameter";
}


public interface GrammarReaderController {
	void error( string msg, XmlReader reader );
	void warning( string msg, XmlReader reader );
}

}
