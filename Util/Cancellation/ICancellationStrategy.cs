using System;
using System.Threading;

namespace Util.Cancellation
{
    public interface ICancellationStrategy
    {
        void ThrowIfCancelled();
    }

    public static class CancellationStrategy
    {
        public static ICancellationStrategy None => new NoneCancellationStrategy();

        public static ICancellationStrategy TimeoutAfter(TimeSpan time) => new TimeoutCancellationStrategy(time);

        public static ICancellationStrategy FromToken(CancellationToken token) => new TokenCancellationStrategy(token);
    }

    internal class NoneCancellationStrategy : ICancellationStrategy
    {
        public void ThrowIfCancelled() { }
    }

    internal class TimeoutCancellationStrategy : ICancellationStrategy
    {
        private readonly DateTime timeout;

        public TimeoutCancellationStrategy(TimeSpan timeout)
        {
            this.timeout = DateTime.UtcNow + timeout;
        }
        public void ThrowIfCancelled()
        {
            if (timeout < DateTime.UtcNow)
                throw new OperationCanceledException("Timeout!");
        }
    }

    internal class TokenCancellationStrategy : ICancellationStrategy
    {
        private readonly CancellationToken token;

        public TokenCancellationStrategy(CancellationToken token)
        {
            this.token = token;
        }

        public void ThrowIfCancelled()
        {
            token.ThrowIfCancellationRequested();
        }
    }
}
