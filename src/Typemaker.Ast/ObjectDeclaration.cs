﻿using System;
using System.Collections.Generic;
using System.Text;
using Typemaker.Ast.Statements;
using Typemaker.Ast.Statements.Expressions;
using Typemaker.Parser;

namespace Typemaker.Ast
{
	sealed class ObjectDeclaration : GenericDeclaration, IObjectDeclaration
	{
		public IEnumerable<IAssignment> VarOverrides => ChildrenAs<IAssignment>();

		public IEnumerable<IDecorator> Decorators => ChildrenAs<IDecorator>();

		public IEnumerable<ISetStatement> SetStatements => ChildrenAs<ISetStatement>();

		public IObjectPath DeclaredParentPath { get; }

		public ObjectDeclaration(TypemakerParser.Object_declarationContext context, IEnumerable<ITrivia> children) : base(ParseTreeFormatters.ExtractObjectPath(context.fully_extended_identifier(), false, out var baseType), context.declaration_block(), children)
		{
			DeclaredParentPath = baseType;
		}
	}
}
