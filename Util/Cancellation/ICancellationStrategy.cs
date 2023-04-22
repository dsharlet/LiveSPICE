using System;

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
}
