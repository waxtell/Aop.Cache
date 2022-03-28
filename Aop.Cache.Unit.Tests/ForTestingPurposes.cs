using System;
using System.Threading.Tasks;

namespace Aop.Cache.Unit.Tests;

public interface IForTestingPurposes
{
    string MethodCall(int arg1, string arg2);

    string Member { get; set; }

    string VirtualMethodCall(int arg1, string arg2);

    Task<string> AsyncMethodCall(int arg1, string arg2);

    Task AsyncAction(int arg1, int arg2, string arg3);

    int ThrowsException(int arg1);
    Task<int> ThrowsExceptionAsync(int arg1);
}

public class ForTestingPurposes : IForTestingPurposes
{
    public uint ThrowExceptionAsyncInvocationCount { get; private set; }
    public uint ThrowExceptionInvocationCount { get; private set; }
    public uint MemberGetInvocationCount { get; private set; }
    public uint MemberSetInvocationCount { get; private set; }
    public uint MethodCallInvocationCount { get; private set; }
    public uint VirtualMethodCallInvocationCount { get; private set; }
    public uint AsyncMethodCallInvocationCount { get; private set; }
    public uint AsyncActionCallInvocationCount { get; private set; }

    private string _member;

    public string Member
    {
        get
        {
            MemberGetInvocationCount++;
            return _member;
        }

        set
        {
            MemberSetInvocationCount++;
            _member = value;
        }
    }

    public string MethodCall(int arg1, string arg2)
    {
        MethodCallInvocationCount++;
        return arg1 + arg2;
    }

    public virtual string VirtualMethodCall(int arg1, string arg2)
    {
        VirtualMethodCallInvocationCount++;
        return arg1 + arg2;
    }

    public ForTestingPurposes()
    {
        ThrowExceptionAsyncInvocationCount = 0;
        ThrowExceptionInvocationCount = 0;
        MemberGetInvocationCount = 0;
        MemberSetInvocationCount = 0;
        MethodCallInvocationCount = 0;
        AsyncMethodCallInvocationCount = 0;
        AsyncActionCallInvocationCount = 0;
    }

    public async Task<string> AsyncMethodCall(int arg1, string arg2)
    {
        AsyncMethodCallInvocationCount++;

        await Task.Delay(0);

        return arg1 + arg2;
    }

    public async Task AsyncAction(int arg1, int arg2, string arg3)
    {
        AsyncActionCallInvocationCount++;

        await Task.Delay(0);
    }

    public int ThrowsException(int arg1)
    {
        ThrowExceptionInvocationCount++;
        throw new Exception("This is an exception");
    }

    public Task<int> ThrowsExceptionAsync(int arg1)
    {
        ThrowExceptionAsyncInvocationCount++;
        throw new Exception("This is an exception");
    }
}