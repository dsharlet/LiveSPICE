using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ComputerAlgebra
{
    /// <summary>
    /// Recursively visit an expression.
    /// </summary>
    public class RecursiveExpressionVisitor : ExpressionVisitor<Expression>
    {
        protected override Expression VisitUnknown(Expression E) { return E; }

        protected virtual IEnumerable<Expression> VisitList(IEnumerable<Expression> List)
        {
            List<Expression> list = new List<Expression>();
            bool Equal = true;
            foreach (Expression i in List)
            {
                Expression Vi = Visit(i);
                if (ReferenceEquals(Vi, null)) return null;
                list.Add(Vi);

                Equal = Equal && ReferenceEquals(Vi, i);
            }
            return Equal ? List : list;
        }

        protected override Expression VisitBinary(Binary B)
        {
            Expression L = Visit(B.Left);
            Expression R = Visit(B.Right);
            if (ReferenceEquals(L, null) || ReferenceEquals(R, null)) return null;
            
            if (ReferenceEquals(L, B.Left) && ReferenceEquals(R, B.Right))
                return B;
            else
                return Binary.New(B.Operator, L, R);
        }

        protected override Expression VisitUnary(Unary U)
        {
            Expression O = Visit(U.Operand);
            if (ReferenceEquals(O, null)) return null;
            
            if (ReferenceEquals(O, U.Operand))
                return U;
            else
                return Unary.New(U.Operator, O);
        }

        protected override Expression VisitSum(Sum A)
        {
            IEnumerable<Expression> terms = VisitList(A.Terms);
            if (ReferenceEquals(terms, null)) return null;
            return ReferenceEquals(terms, A.Terms) ? A : Sum.New(terms);
        }

        protected override Expression VisitProduct(Product M)
        {
            IEnumerable<Expression> terms = VisitList(M.Terms);
            if (ReferenceEquals(terms, null)) return null;
            return ReferenceEquals(terms, M.Terms) ? M : Product.New(terms);
        }

        protected override Expression VisitSet(Set S)
        {
            IEnumerable<Expression> members = VisitList(S.Members);
            if (ReferenceEquals(members, null)) return null;
            return ReferenceEquals(members, S.Members) ? S : Set.New(members);
        }

        protected override Expression VisitCall(Call F)
        {
            IEnumerable<Expression> arguments = VisitList(F.Arguments);
            if (ReferenceEquals(arguments, null)) return null;
            return ReferenceEquals(arguments, F.Arguments) ? F : Call.New(F.Target, arguments);
        }
    }
}
