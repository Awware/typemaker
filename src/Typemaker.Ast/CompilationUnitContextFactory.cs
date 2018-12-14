﻿using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using Typemaker.Parser;

namespace Typemaker.Ast
{
	class CompilationUnitContextFactory : ICompilationUnitContextFactory
	{
		public TypemakerParser.Compilation_unitContext CreateCompilationUnitContext(Stream input, out Func<IList<IToken>> tokensAccessor, out IReadOnlyList<ParseError> parseErrors)
		{
			var inputStream = new AntlrInputStream(input);
			var lexer = new TypemakerLexer(inputStream);

			var errors = new List<ParseError>();
			var errorListener = new ReportingErrorListener(errors, lexer.Vocabulary);
			lexer.AddErrorListener(errorListener);

			var tokenStream = new CommonTokenStream(lexer);
			var parser = new TypemakerParser(tokenStream);
			parser.AddErrorListener(errorListener);

			var compilationUnitContext = parser.compilation_unit();
			parseErrors = errors;
			tokensAccessor = tokenStream.GetTokens;
			return compilationUnitContext;
		}
	}
}