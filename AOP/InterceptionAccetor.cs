using Castle.DynamicProxy;

namespace Lithium.AOP;

public abstract class InterceptionAccetor
{
    public abstract bool Accept(IInvocation invocation);
}
