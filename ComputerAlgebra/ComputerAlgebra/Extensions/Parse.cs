using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ComputerAlgebra
{
    /// <summary>
    /// Exception thrown when an error occurs in parsing an expression.
    /// </summary>
    public class ParseException : Exception
    {
        public ParseException(string m) : base(m) { }
    }

    /// <summary>
    /// A sequence of tokens.
    /// </summary>
    class TokenStream
    {
        static string Escape(params string [] s) 
        {
            StringBuilder S = new StringBuilder();
            foreach (string i in s)
            {
                S.Append("|(");
                foreach (char j in i)
                    S.Append("\\" + j);
                S.Append(")");
            }
            return S.ToString();
        }

        static string Name = @"[a-zA-Z_]\w*";
        static string Literal = @"[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?";
        static Regex token = new Regex(
            "(" + Name + ")|(" + Literal + ")" +
            Escape("==", "=", "!=", ">=", ">", "<=", "<", "~=", "->",
            "+", "-", "*", "/", "^", "'",
            "!", "&", "|", ":",
            ",", "[", "]", "(", ")", "{", "}", "\u221E"));

        List<string> tokens = new List<string>();

        public TokenStream(string s) 
        {
            MatchCollection matches = token.Matches(s);
            foreach (Match m in matches)
                tokens.Add(m.ToString());
        }

        public string Tok { get { if (tokens.Count > 0) return tokens.First(); else return "";  } }
        public string Consume() { string tok = Tok;  tokens.RemoveAt(0); return tok; }
        public void Expect(string s) { if (Tok == s) Consume(); else throw new ParseException("Expected " + s); }
        public void ExpectEnd() { if (tokens.Count != 0) throw new ParseException("Expected end"); }
    }
    
    /// <summary>
    /// Implements "precedence climbing": http://www.engr.mun.ca/~theo/Misc/exp_parsing.htm#classic
    /// </summary>
    class Parser
    {
        private Namespace context;
        private TokenStream tokens;

        static bool IsBinaryOperator(string tok, ref Operator op)
        {
            switch (tok)
            {
                case "+": op = Operator.Add; return true;
                case "-": op = Operator.Subtract; return true;
                case "*": op = Operator.Multiply; return true;
                case "/": op = Operator.Divide; return true;
                case "^": op = Operator.Power; return true;
                case "&": op = Operator.And; return true;
                case "|": op = Operator.Or; return true;
                case ":": op = Operator.Substitute; return true;
                case "->": op = Operator.Arrow; return true;
                case "==": op = Operator.Equal; return true;
                case "!=": op = Operator.NotEqual; return true;
                case ">": op = Operator.Greater; return true;
                case "<": op = Operator.Less; return true;
                case ">=": op = Operator.GreaterEqual; return true;
                case "<=": op = Operator.LessEqual; return true;
                case "~=": op = Operator.ApproxEqual; return true;
                default: return false;
            }
        }

        static bool IsUnaryPreOperator(string tok, ref Operator op)
        {
            switch (tok)
            {
                case "-": op = Operator.Negate; return true;
                case "!": op = Operator.Not; return true;
                default: return false;
            }
        }

        static bool IsUnaryPostOperator(string tok, ref Operator op)
        {
            switch (tok)
            {
                case "'": op = Operator.Prime; return true;
                default: return false;
            }
        }

        public static int Precedence(Expression x)
        {
            if (x is Sum)
                return Precedence(Operator.Add);
            else if (x is Product)
                return Precedence(Operator.Multiply);
            else if (x is Binary)
                return Precedence(((Binary)x).Operator);
            else if (x is Unary)
                return Precedence(((Unary)x).Operator);
            else if (x is Atom)
                return 100;
            return Precedence(Operator.Equal);
        }

        public static int Precedence(Operator op)
        {
            switch (op)
            {
                case Operator.And:
                case Operator.Not:
                    return 3;
                case Operator.Or: 
                    return 4;
                case Operator.Add:
                case Operator.Subtract: 
                    return 5;
                case Operator.Negate: 
                    return 6;
                case Operator.Multiply:
                case Operator.Divide: 
                    return 7;
                case Operator.Power: 
                    return 8;
                case Operator.Prime:
                    return 9;
                case Operator.Arrow:
                    return 2;
                default: 
                    return 1;
            }
        }

        private static bool IsLeftAssociative(Operator op)
        {
            switch (op)
            {
                case Operator.Power: return false;
                default: return true;
            }
        }

        public Parser(Namespace Context, string s) { context = Context; tokens = new TokenStream(s); }
        
        //Eparser is
        //   var t : Tree
        //   t := Exp( 0 )
        //   expect( end )
        //   return t
        public Expression Parse()
        {
            Expression t = Exp(0);
            tokens.ExpectEnd();
            return t;            
        }
        
        //Exp( p ) is
        //    var t : Tree
        //    t := P
        //    while next is a binary operator and prec(binary(next)) >= p
        //       const op := binary(next)
        //       consume
        //       const q := case associativity(op)
        //                  of Right: prec( op )
        //                     Left:  1+prec( op )
        //       const t1 := Exp( q )
        //       t := mkNode( op, t, t1)
        //    return t
        private Expression Exp(int p)
        {
            Expression l = P();

            Operator op = new Operator();
            while (true)
            {
                if (IsBinaryOperator(tokens.Tok, ref op) && Precedence(op) >= p)
                {
                    tokens.Consume();

                    int q = Precedence(op) + (IsLeftAssociative(op) ? 1 : 0);
                    Expression r = Exp(q);
                    l = Binary.New(op, l, r);
                }
                else if (IsUnaryPostOperator(tokens.Tok, ref op) && Precedence(op) >= p)
                {
                    tokens.Consume();
                    l = Unary.New(op, l);
                }
                else
                {
                    break;
                }
            }
            return l;            
        }

        // Parse a list of expressions.
        private List<Expression> L(string Delim, string Term)
        {
            List<Expression> exprs = new List<Expression>();
            while (true)
            {
                if (tokens.Tok == Term) break;
                exprs.Add(Exp(0));
                if (tokens.Tok == Term) break;
                tokens.Expect(Delim);
            }
            tokens.Expect(Term);
            return exprs;
        }

        //P is
        //    if next is a unary operator
        //         const op := unary(next)
        //         consume
        //         q := prec( op )
        //         const t := Exp( q )
        //         return mkNode( op, t )
        //    else if next = "("
        //         consume
        //         const t := Exp( 0 )
        //         expect ")"
        //         return t
        //    else if next is a v
        //         const t := mkLeaf( next )
        //         consume
        //         return t
        //    else
        //         error
        private Expression P()
        {
            Operator op = new Operator();
            if (tokens.Tok == "+")
            {
                // Skip unary +.
                tokens.Consume();
                return P();
            }
            else if (IsUnaryPreOperator(tokens.Tok, ref op))
            {
                tokens.Consume();
                Expression t = Exp(Precedence(op));
                return Unary.New(op, t);
            }
            else if (tokens.Tok == "(")
            {
                tokens.Consume();
                Expression t = Exp(0);
                tokens.Expect(")");
                return t;
            }
            else
            {
                string tok = tokens.Consume();

                decimal dec = 0;
                double dbl = 0.0;
                if (decimal.TryParse(tok, out dec))
                    return Constant.New(dec);
                else if (double.TryParse(tok, out dbl))
                    return Constant.New(dbl);
                else if (tok == "True")
                    return Constant.New(true);
                else if (tok == "False")
                    return Constant.New(false);
                else if (tok == "\u221E" || tok == "oo")
                    return Constant.New(Real.Infinity);
                else if (tok == "{")
                    return Set.New(L(",", "}"));
                else if (tokens.Tok == "[")
                {
                    tokens.Consume();
                    List<Expression> args = L(",", "]");
                    return Call.New(Resolve(tok, args), args);
                }
                else if (tokens.Tok == "(")
                {
                    tokens.Consume();
                    List<Expression> args = L(",", ")");
                    return Call.New(Resolve(tok, args), args);
                }
                else
                {
                    return Resolve(tok);
                }
            }
        }

        private Function Resolve(string Token, IEnumerable<Expression> Args)
        {
            try
            {
                return context.Resolve(Token, Args);
            }
            catch (UnresolvedName)
            {
                // If the token is unresolved, make a new undefined function.
                return ExprFunction.New(Token, Args.Select(i => Variable.New("")));
            }
        }

        private Expression Resolve(string Token)
        {
            try
            {
                return context.Resolve(Token);
            }
            catch (UnresolvedName)
            {
                // If the token is unresolved, make a new variable.
                return Variable.New(Token);
            }
        }
    }
}
