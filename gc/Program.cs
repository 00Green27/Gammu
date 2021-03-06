﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace gc
{
  class Program
  {
    static void Main(string[] args)
    {
      while (true)
      {
        System.Console.Write("> ");
        var line = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(line))
          return;

        var parser = new Parser(line);
        var expression = parser.Parse();
        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        PrettyPrint(expression);
        Console.ForegroundColor = color;
      }
    }
    static void PrettyPrint(SyntaxNode node, string indent = "", bool isLast = true)
    {
      // └──
      // ├──
      // │
      var marker = isLast ? "└──" : "├──";

      System.Console.Write(indent);
      System.Console.Write(marker);
      System.Console.Write(node.Kind);
      if (node is SyntaxToken t && t.Value != null)
      {
        System.Console.Write(" ");
        System.Console.Write(t.Value);
      }

      System.Console.WriteLine();

      indent += isLast ? "   " : "│   ";
      var lastChild = node.GetChildren().LastOrDefault();

      foreach (var child in node.GetChildren())
      {
        PrettyPrint(child, indent, child == lastChild);
      }
    }
  }

  enum SyntaxKind
  {
    NumberToken,
    WhitespaceToken,
    PlusToken,
    MinusToken,
    StarToken,
    SlashToken,
    OpenParenthesisToken,
    CloseParenthesisToken,
    BadToken,
    EndOfFileToken,
    BinaryExpression,
    NumberExpression
  }

  class SyntaxToken : SyntaxNode
  {
    public SyntaxToken(SyntaxKind kind, int positon, string text, object value)
    {
      Kind = kind;
      Positon = positon;
      Text = text;
      Value = value;
    }

    public override SyntaxKind Kind { get; }
    public int Positon { get; }
    public string Text { get; }
    public object Value { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
      return Enumerable.Empty<SyntaxNode>();
    }
  }

  class Lexer
  {
    private readonly string _text;
    private int _position;
    public Lexer(string text)
    {
      _text = text;
    }

    private char Current
    {
      get
      {
        if (_position >= _text.Length)
          return '\0';

        return _text[_position];
      }
    }

    private void Next()
    {
      _position++;
    }

    public SyntaxToken NextToken()
    {
      if (_position >= _text.Length)
      {
        return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, "\0", null);
      }

      if (char.IsDigit(Current))
      {
        var start = _position;

        while (char.IsDigit(Current))
          Next();

        var length = _position - start;
        var text = _text.Substring(start, length);

        int.TryParse(text, out var value);
        return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
      }

      if (char.IsWhiteSpace(Current))
      {
        var start = _position;

        while (char.IsWhiteSpace(Current))
          Next();

        var length = _position - start;
        var text = _text.Substring(start, length);

        return new SyntaxToken(SyntaxKind.WhitespaceToken, start, text, null);
      }

      if (Current == '+')
      {
        return new SyntaxToken(SyntaxKind.PlusToken, _position++, "+", null);
      }
      else if (Current == '-')
      {
        return new SyntaxToken(SyntaxKind.MinusToken, _position++, "-", null);
      }
      else if (Current == '*')
      {
        return new SyntaxToken(SyntaxKind.StarToken, _position++, "*", null);
      }
      else if (Current == '/')
      {
        return new SyntaxToken(SyntaxKind.SlashToken, _position++, "/", null);
      }
      else if (Current == '(')
      {
        return new SyntaxToken(SyntaxKind.OpenParenthesisToken, _position++, "(", null);
      }
      else if (Current == ')')
      {
        return new SyntaxToken(SyntaxKind.CloseParenthesisToken, _position++, ")", null);
      }

      return new SyntaxToken(SyntaxKind.BadToken, _position++, _text.Substring(_position - 1, 1), null);
    }
  }

  abstract class SyntaxNode
  {
    public abstract SyntaxKind Kind { get; }
    public abstract IEnumerable<SyntaxNode> GetChildren();
  }

  abstract class ExpressionSynax : SyntaxNode
  {

  }

  sealed class NumberExpressionSynax : ExpressionSynax
  {
    public NumberExpressionSynax(SyntaxToken numberToken)
    {
      NumberToken = numberToken;
    }

    public override SyntaxKind Kind => SyntaxKind.NumberExpression;

    public SyntaxToken NumberToken { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
      yield return NumberToken;
    }
  }

  sealed class BinaryExpressionSynax : ExpressionSynax
  {
    public BinaryExpressionSynax(
        ExpressionSynax left,
        SyntaxNode operatorToken,
        ExpressionSynax right)
    {
      Left = left;
      OperatorToken = operatorToken;
      Right = right;
    }

    public ExpressionSynax Left { get; }
    public SyntaxNode OperatorToken { get; }
    public ExpressionSynax Right { get; }

    public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
      yield return Left;
      yield return OperatorToken;
      yield return Right;
    }
  }

  class Parser
  {

    private readonly SyntaxToken[] _tokens;
    private int _position;

    public Parser(string text)
    {
      var tokens = new List<SyntaxToken>();
      var lexer = new Lexer(text);
      SyntaxToken token;
      do
      {
        token = lexer.NextToken();
        if (token.Kind != SyntaxKind.WhitespaceToken &&
            token.Kind != SyntaxKind.BadToken)
        {
          tokens.Add(token);
        }
      } while (token.Kind != SyntaxKind.EndOfFileToken);

      _tokens = tokens.ToArray();
    }

    private SyntaxToken Peek(int offset)
    {
      var index = _position + offset;
      if (index >= _tokens.Length)
      {
        return _tokens[_tokens.Length - 1];
      }
      return _tokens[index];
    }

    private SyntaxToken Current => Peek(0);


    private SyntaxToken NextToken()
    {
      var current = Current;
      _position++;
      return current;
    }

    private SyntaxToken Match(SyntaxKind kind)
    {
      if (Current.Kind == kind)
      {
        return NextToken();
      }

      return new SyntaxToken(kind, Current.Positon, null, null);
    }

    public ExpressionSynax Parse()
    {
      var left = ParsePrimaryExpression();
      while (Current.Kind == SyntaxKind.PlusToken ||
             Current.Kind == SyntaxKind.MinusToken)
      {
        var opreratorToken = NextToken();
        var right = ParsePrimaryExpression();
        left = new BinaryExpressionSynax(left, opreratorToken, right);
      }

      return left;
    }

    private ExpressionSynax ParsePrimaryExpression()
    {
      var numberToken = Match(SyntaxKind.NumberToken);
      return new NumberExpressionSynax(numberToken);
    }
  }
}
