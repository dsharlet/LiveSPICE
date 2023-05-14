using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Util.Cancellation;

namespace Circuit
{
    internal class FindVariablesUsedOnce : ExpressionVisitor
    {
        private readonly Dictionary<string, int> usage = new Dictionary<string, int>();
        private readonly ICancellationStrategy cancellationStrategy;

        public ISet<string> Usage => new HashSet<string>(usage.Where(u => u.Value == 3).Select(u => u.Key));

        public FindVariablesUsedOnce(ICancellationStrategy cancellationStrategy)
        {
            this.cancellationStrategy = cancellationStrategy;
        }

        public override Expression Visit(Expression node)
        {
            cancellationStrategy.ThrowIfCancelled();
            return base.Visit(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (usage.ContainsKey(node.Name))
                usage[node.Name] += 1;
            else
                usage[node.Name] = 1;
            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Visit(node.Body);
            return node;
        }
    }
}
