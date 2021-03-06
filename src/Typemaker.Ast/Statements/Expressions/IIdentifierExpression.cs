﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Typemaker.Ast.Statements.Expressions
{
	public interface IIdentifierExpression : IExpression, IIdentifiable
	{
		IdentifierType Type { get; }
	}
}
