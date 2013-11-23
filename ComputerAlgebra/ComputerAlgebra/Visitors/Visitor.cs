using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Visits an expression.
    /// </summary>
    /// <typeparam name="T">Result type of the visitor.</typeparam>
    public abstract class ExpressionVisitor<T>
    {
        protected abstract T VisitUnknown(Expression E);

        protected virtual T VisitSum(Sum A) { return VisitUnknown(A); }
        protected virtual T VisitProduct(Product M) { return VisitUnknown(M); }
        protected virtual T VisitConstant(Constant C) { return VisitUnknown(C); }
        protected virtual T VisitVariable(Variable V) { return VisitUnknown(V); }
        protected virtual T VisitSet(Set S) { return VisitUnknown(S); }
        protected virtual T VisitBinary(Binary B) { return VisitUnknown(B); }
        protected virtual T VisitUnary(Unary U) { return VisitUnknown(U); }
        protected virtual T VisitPower(Power P) { return VisitBinary(P); }
        protected virtual T VisitCall(Call F) { return VisitUnknown(F); }

        public virtual T Visit(Expression E)
        {
            if (E is Sum) return VisitSum(E as Sum);
            else if (E is Product) return VisitProduct(E as Product);
            else if (E is Constant) return VisitConstant(E as Constant);
            else if (E is Variable) return VisitVariable(E as Variable);
            else if (E is Set) return VisitSet(E as Set);
            else if (E is Power) return VisitPower(E as Power);
            else if (E is Binary) return VisitBinary(E as Binary);
            else if (E is Unary) return VisitUnary(E as Unary);
            else if (E is Call) return VisitCall(E as Call);
            else return VisitUnknown(E);
        }
    }
}
