﻿using Antlr4.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Typemaker.Ast.Serialization;
using Typemaker.Parser;

namespace Typemaker.Ast
{
	abstract class SyntaxNode : SyntaxNodeBase, ISyntaxNode
	{
		public ISyntaxTree Tree { get; private set; }

		public ISyntaxNode Parent => parent;

		public IReadOnlyList<ISyntaxNode> Children => children;

		public IReadOnlyList<ICommentTrivia> Comments => ChildrenAs<ICommentTrivia>();

		public IReadOnlyList<IWhitespaceTrivia> Whitespace => ChildrenAs<IWhitespaceTrivia>();

		public ILocatable TriviaRestrictionViolation { get; private set; }

		readonly List<SyntaxNode> children;

		readonly int? startTokenIndex;
		readonly int? stopTokenIndex;

		int? antiTriviaStartTokenIndex;
		int? antiTriviaStopTokenIndex;

		SyntaxNode parent;

		static Location BuildLocation(IToken token, bool advanceOne) => new Location
		{
			Line = (ulong)token.Line,
			Column = (ulong)(advanceOne ? token.Column + 1 : token.Column)
		};

		SyntaxNode(SyntaxGraph graph, ISyntaxNodeDeserializer syntaxNodeFactory)
		{
			if (graph == null)
				throw new ArgumentNullException(nameof(graph));
			Trivia = graph.Trivia;
			Start = graph.Start;
			End = graph.End;
			children = graph.Children.Select(x => syntaxNodeFactory.CreateSyntaxNode(x)).ToList();
		}

		protected SyntaxNode(ParserRuleContext context, IEnumerable<SyntaxNode> children)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			this.children = children?.Select(x =>
			{
				if (x == null)
					throw new InvalidOperationException("Attempted to add null child node!");
				return x;
			}).ToList() ?? throw new ArgumentNullException(nameof(children));

			startTokenIndex = context.Start.TokenIndex;
			stopTokenIndex = context.Stop.TokenIndex;

			Start = BuildLocation(context.Start, false);
			End = BuildLocation(context.Stop, true);
		}

		protected SyntaxNode(SyntaxNode parent, ISyntaxTree tree, IToken token)
		{
			this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
			Tree = tree ?? throw new ArgumentNullException(nameof(tree));
			Start = BuildLocation(token, false);
			End = BuildLocation(token, true);

			Trivia = true;
		}

		SyntaxGraph BuildGraph(ISyntaxNodeDeserializer syntaxNodeFactory)
		{
			var graph = new SyntaxGraph
			{
				Start = Start,
				End = End,
				Trivia = Trivia,
				Children = children.Select(x => x.BuildGraph(syntaxNodeFactory)).ToList(),
				Properties = new Dictionary<string, object>(),
				NodeType = syntaxNodeFactory.GetNodeType(GetType())
			};
			//PopulateGraph(graph);
			return graph;
		}

		protected void LinkTree(SyntaxNode parent, ISyntaxTree tree, bool deserialize)
		{
			if (!deserialize && parent == null)
				throw new ArgumentNullException(nameof(parent));
			if (!deserialize && tree == null)
				throw new ArgumentNullException(nameof(tree));
			if (Tree != null)
				throw new InvalidOperationException("Tree has already been set!");
			if (Parent != null)
				throw new InvalidOperationException("Parent has already been set!");

			Tree = tree;
			this.parent = parent;

			foreach (var I in children)
				I.LinkTree(this, tree, deserialize);
		}

		protected void BuildTrivia(SyntaxNode left, SyntaxNode right, IList<IToken> tokens)
		{
			if (tokens == null)
				throw new ArgumentNullException(nameof(tokens));
			if (Trivia)
				throw new InvalidOperationException("Trivia cannot build trivia!");
			SyntaxNode childLeft = left;
			for (var I = 0; I < children.Count; ++I)
			{
				var child = children[I];
				var childRight = I < children.Count - 1 ? children[I + 1] : right;
				child.BuildTrivia(childLeft, childRight, tokens);
				childLeft = child;
			}
			
			var leftmost = (left?.stopTokenIndex ?? -1) + 1;
			var rightmost = (right?.startTokenIndex ?? tokens.Count);
			var offset = 0;

			int RecheckSkipRange() => offset >= children.Count ? rightmost : children[offset].startTokenIndex.Value;
			var check = RecheckSkipRange();

			SyntaxNode BuildTriviaNode(IToken token)
			{
				switch (token.Type)
				{
					case TypemakerLexer.NEWLINES:
						return new WhitespaceTrivia(WhitespaceType.Newlines, this, Tree, token);
					case TypemakerLexer.WINDOWS_NEWLINES:
						return new WhitespaceTrivia(WhitespaceType.WindowsNewlines, this, Tree, token);
					case TypemakerLexer.TABS:
						return new WhitespaceTrivia(WhitespaceType.Tabs, this, Tree, token);
					case TypemakerLexer.SPACES:
						return new WhitespaceTrivia(WhitespaceType.Spaces, this, Tree, token);
					case TypemakerLexer.SINGLE_LINE_COMMENT:
						return new CommentTrivia(this, Tree, token, false);
					case TypemakerLexer.DELIMITED_COMMENT:
						return new CommentTrivia(this, Tree, token, true);
					default:
						return null;
				}
			}

			for(var I = leftmost; I < rightmost; ++I)
			{
				if(I == check)
				{
					I = children[offset].stopTokenIndex.Value + 1;
					++offset;
					check = RecheckSkipRange();
					continue;
				}

				var token = tokens[I];
				var newNode = BuildTriviaNode(token);
				if(newNode == null)
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid trivia token {0} ({1})!", token.Type, token.Text));

				children.Insert(offset, newNode);
				++offset;
			}

			if (antiTriviaStartTokenIndex.HasValue)
			{
				int? startLoc = null, endLoc = null;
				for (var I = antiTriviaStartTokenIndex.Value; I < antiTriviaStopTokenIndex.Value; ++I)
					if (BuildTriviaNode(tokens[I]) != null)
					{
						if (!startLoc.HasValue)
							startLoc = I;
						endLoc = I;
					}
					else if (endLoc.HasValue)
						break;
				if (startLoc.HasValue)
					TriviaRestrictionViolation = new Highlight
					{
						Start = BuildLocation(tokens[startLoc.Value], false),
						End = BuildLocation(tokens[endLoc.Value], true)
					};
			}
		}

		protected void AntiTriviaContext(ParserRuleContext startContext, ParserRuleContext endContext = null)
		{
			if (startContext == null)
				throw new ArgumentNullException(nameof(startContext));
			if (antiTriviaStartTokenIndex.HasValue)
				throw new InvalidOperationException("Anti trivia range already set!");
			antiTriviaStartTokenIndex = startContext.Start.TokenIndex;
			antiTriviaStopTokenIndex = (endContext ?? startContext).Stop.TokenIndex;
		}

		protected TChildNode ChildAs<TChildNode>(int index = 0) where TChildNode : ISyntaxNode => SelectChildren<TChildNode>().ElementAt(index);
		protected IReadOnlyList<TChildNode> ChildrenAs<TChildNode>() where TChildNode : ISyntaxNode => SelectChildren<TChildNode>().ToList();

		public SyntaxGraph Serialize() => BuildGraph(new SyntaxNodeDeserializer());

		public IEnumerable<TChildNode> SelectChildren<TChildNode>() where TChildNode : ISyntaxNode => children.Where(x => x is TChildNode).Select(x => (TChildNode)(object)x);

		public void Transform(IEnumerable<SyntaxGraph> replacements)
		{
			if (replacements == null)
				throw new ArgumentNullException(nameof(replacements));
			if (parent == null)
				throw new InvalidOperationException("Cannot transform a parentless syntax node!");

			var parentChildren = parent.children;
			var ourIndex = parentChildren.IndexOf(this);
			parentChildren.RemoveAt(ourIndex);

			var deserializer = new SyntaxNodeDeserializer();

			parentChildren.InsertRange(ourIndex, replacements.Select(x => deserializer.CreateSyntaxNode(x)));
		}
	}
}