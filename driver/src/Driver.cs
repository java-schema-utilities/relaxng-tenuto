#define	TRACE

namespace Tenuto {

using Tenuto.Reader;
using System;
using System.Diagnostics;
using System.Xml;
using System.Collections;

public class Driver {
	
	public static int Main( string[] args ) {
		
		Console.WriteLine("Tenuto Verifier");
		
		if(args.Length==0) {
			Console.WriteLine(
				"Usage: Tenuto <schema> <document1> [<document2> ...]");
			return -1;
		}

		string grammarName=null;
		ArrayList documents = new ArrayList();
		// parse command line
		for( int i=0; i<args.Length; i++ ) {
			if(args[i][0]!='-') {
				if(grammarName==null)
					grammarName = args[i];
				else
					documents.Add(args[i]);
			} else {
				// options
				if(args[i]=="-debug") {
					Trace.Listeners.Add( new TextWriterTraceListener(Console.Out) );
					continue;
				}

				Console.WriteLine("unrecognized option: "+args[i]);
				return -1;
			}
		}
		
		Console.WriteLine("loading "+grammarName);
		Grammar.Grammar grammar = new GrammarReader(
			new ConsoleController()).parse(grammarName);
		if(grammar==null) {
			Console.WriteLine("bailing out");
			return -1;
		}
//		Console.WriteLine( Grammar.ExpPrinter.printGrammar(grammar) );
		
		bool noError = true;
		
		for( int i=0; i<documents.Count; i++ ) {
			string url = (string)documents[i];
			Console.WriteLine("validating "+url);
			
			bool wasValid = Verifier.Verifier.Verify(
				new XmlTextReader(url),grammar,new ConsoleErrorReporter());
			noError &= wasValid;

			if(wasValid)	Console.WriteLine("document is valid");
			else			Console.WriteLine("document is not valid");
		}
		
		return noError?0:1;
	}
}

public class ConsoleController : GrammarReaderController {
	
	public void error( string msg, IXmlLineInfo loc ) {
		Console.WriteLine("Error: "+msg);
		PrintLocation(loc);
	}
	
	public void warning( string msg, IXmlLineInfo loc ) {
		Console.WriteLine("Warning: "+msg);
		PrintLocation(loc);
	}
	
	private void PrintLocation( IXmlLineInfo loc ) {
		if( loc!=null ) {
			Console.WriteLine("{0}({1}:{2})",
				((XmlReader)loc).BaseURI, loc.LineNumber, loc.LinePosition );
		}
	}
}

public class ConsoleErrorReporter : Tenuto.Verifier.ErrorHandler {
	public void Error(string msg) {
		Console.WriteLine(msg);
	}
}

}
