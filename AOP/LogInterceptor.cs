using Castle.DynamicProxy;

namespace Lithium.AOP;

public class LogInterceptor : IAsyncInterceptor
{
    private readonly IEnumerable<InterceptionAccetor> acceptors;
    private readonly ILoggerFactory loggerFactory;

    public LogInterceptor(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
    }

    public void InterceptSynchronous(IInvocation invocation)
    {
        var intercept = acceptors.Any(_ => _.Accept(invocation));
        if (intercept)
        {
            var logger = loggerFactory.CreateLogger(invocation.TargetType);
            try
            {
                // Step 1. Do something prior to invocation.
                logger.LogDebug($"Calling {invocation.Method.Name}");
                invocation.Proceed();
                // Step 2. Do something after invocation.
                logger.LogDebug($"Call to {invocation.Method.Name} completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Call to {invocation.Method.Name} failed");
            }
        }
        else
        {
            invocation.Proceed();
        }
    }

    public void InterceptAsynchronous(IInvocation invocation)
    {
        invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
    }

    private async Task InternalInterceptAsynchronous(IInvocation invocation)
    {
        // Step 1. Do something prior to invocation.

        invocation.Proceed();
        var task = (Task)invocation.ReturnValue;
        await task;

        // Step 2. Do something after invocation.
    }

    public void InterceptAsynchronous<TResult>(IInvocation invocation)
    {
        invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
    }

    private async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
    {
        // Step 1. Do something prior to invocation.

        invocation.Proceed();
        var task = (Task<TResult>)invocation.ReturnValue;
        TResult result = await task;

        // Step 2. Do something after invocation.

        return result;
    }
}
