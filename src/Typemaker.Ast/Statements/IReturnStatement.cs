﻿using System;
using System.Collections.Generic;
using System.Text;
using Typemaker.Ast.Statements.Expressions;

namespace Typemaker.Ast.Statements
{
	public interface IReturnStatement : IStatement
	{
		IExpression Result { get; }
	}
}
