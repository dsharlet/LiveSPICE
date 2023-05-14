using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Util.Cancellation;

namespace Circuit
{
    internal class RemoveVariablesUsedOnce : ExpressionVisitor
    {
        private readonly ISet<string> toRemove;
        private readonly ICancellationStrategy cancellationStrategy;
        private readonly Dictionary<string, Expression> variables = new Dictionary<string, Expression>();

        public ISet<string> removed = new HashSet<string>();

        public RemoveVariablesUsedOnce(ISet<string> toRemove, ICancellationStrategy cancellationStrategy)
        {
            this.toRemove = toRemove;
            this.cancellationStrategy = cancellationStrategy;
        }

        public override Expression Visit(Expression node)
        {
            cancellationStrategy.ThrowIfCancelled();
            return base.Visit(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (variables.TryGetValue(node.Name, out var expression))
            {
                removed.Add(node.Name);
                return Visit(expression);
            }
            return node;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            var replaced = node.Expressions.Select(Visit).Where(e => e != null).ToArray();
            return Expression.Block(node.Variables.Where(v => !removed.Contains(v.Name)), replaced);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Assign && node.Left is ParameterExpression p && toRemove.Contains(p.Name))
            {
                variables.Add(p.Name, node.Right);
                return null;
            }
            return base.VisitBinary(node);
        }

    }
}
