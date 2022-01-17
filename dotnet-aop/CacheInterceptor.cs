using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Dotnet.AOP.Tests;

public class CacheInterceptor : IInterceptor
{
    private readonly TaskWrapper wrapper = new TaskWrapper();
    private readonly ICache cache;
    private TaskFromResultFactory factory = new TaskFromResultFactory();

    public CacheInterceptor(ICache cache)
    {
        this.cache = cache;
    }

    public void Intercept(IInvocation invocation)
    {
        string key = BuildKeyFromInvocation(invocation); // Build key based on parameters passed to invocation...
        if (cache.HasKey(key))
        {
            if (IsReturningTask(invocation))
            {
                invocation.ReturnValue = CreateTaskFromResult(
                    GetGenericTaskArgumentType(invocation.Method.ReturnType),
                    cache.GetValue(key)
                );
            }
            else
            {
                invocation.ReturnValue = cache.GetValue(key);
            }
        }
        else
        {
            if (IsReturningTask(invocation))
            {
                invocation.ReturnValue = WrapTask(
                    invocation.Method.ReturnType,
                    key,
                    CallTarget(invocation));
            }
            else
            {
                var value = CallTarget(invocation);
                cache.AddValue(key, value);
                invocation.ReturnValue = value;
            }
        }
    }

    private string BuildKeyFromInvocation(IInvocation invocation) => "aaaaaaaa";

    private object WrapTask(Type returnType, string key, object value) =>
        wrapper.GetWrapperCreator(returnType)(
            (Task)value,
            null,
            async t => cache.AddValue(key, t.GetTaskResult(returnType)),
            (t, ex) => { });

    private Type GetGenericTaskArgumentType(Type type) => type.GenericTypeArguments[0];

    private object CreateTaskFromResult(Type returnType, object value) => factory.Create(returnType, value);

    private object CallTarget(IInvocation invocation/* , we need call any next interceptor if exists */)
    {
        invocation.Proceed();
        return invocation.ReturnValue;
    }

    private bool IsReturningTask(IInvocation invocation) => typeof(Task).IsAssignableFrom(invocation.Method.ReturnType);
}

public interface ICache
{
    bool HasKey(string key);
    object GetValue(string key);
    void AddValue(string key, object value);
}

class TaskWrapper
{
    private readonly ConcurrentDictionary<Type, Func<Task, IInvocation, Action<Task>, Action<Task, Exception>, Task>> wrapperCreators =
        new ConcurrentDictionary<Type, Func<Task, IInvocation, Action<Task>, Action<Task, Exception>, Task>>();

    public Func<Task, IInvocation, Action<Task>, Action<Task, Exception>, Task> GetWrapperCreator(Type taskType) =>
        wrapperCreators.GetOrAdd(
            taskType,
            (Type t) =>
            {
                if (t == typeof(Task))
                {
                    return CreateWrapperTask;
                }
                else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    return (Func<Task, IInvocation, Action<Task>, Action<Task, Exception>, Task>)GetType()
                        .GetMethod("CreateWrapperTask", BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(new Type[] { t.GenericTypeArguments[0] })
                        .CreateDelegate(typeof(Func<Task, IInvocation, Action<Task>, Action<Task, Exception>, Task>), this);
                }
                else
                {
                    return (task, _1, _2, _3) => task;
                }
            }
        );

    private async Task CreateWrapperTask(Task task, IInvocation input, Action<Task> onTaskCompleted, Action<Task, Exception> onError)
    {
        try
        {
            await task.ConfigureAwait(false);
            onTaskCompleted(task);
        }
        catch (Exception e)
        {
            onError(task, e);
            throw;
        }
    }

    private Task CreateGenericWrapperTask<T>(Task task, IInvocation input, Action<Task> onTaskCompleted, Action<Task, Exception> onError) =>
        DoCreateGenericWrapperTask<T>((Task<T>)task, input, onTaskCompleted, onError);

    private async Task<T> DoCreateGenericWrapperTask<T>(Task<T> task, IInvocation input, Action<Task> onTaskCompleted, Action<Task, Exception> onError)
    {
        try
        {
            T value = await task.ConfigureAwait(false);
            onTaskCompleted(task);
            return value;
        }
        catch (Exception e)
        {
            onError(task, e);
            throw;
        }
    }
}

class TaskFromResultFactory
{
    public object? Create(Type resultType, Object result) =>
        typeof(Task)
            ?.GetMethod("FromResult")
            ?.MakeGenericMethod(resultType)
            ?.Invoke(null, new Object[] { result });
}

static class TaskExtensions
{
    public static Object? GetTaskResult(this Task _this, Type type) =>
        type.GetProperty("Result")?.GetValue(_this);
}
