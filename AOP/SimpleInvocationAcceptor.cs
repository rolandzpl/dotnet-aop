using Castle.DynamicProxy;

namespace Lithium.AOP;

public class SimpleInvocationAcceptor : InterceptionAccetor
{
    public override bool Accept(IInvocation invocation)
    {
        return true;
    }
}