namespace Aop.Cache.Unit.Tests
{
    public interface IForTestingPurposes
    {
        string MethodCall(int arg1, string arg2);

        string Member { get; set; }
    }

    public class ForTestingPurposes : IForTestingPurposes
    {
        public uint MemberGetInvocationCount { get; private set; }
        public uint MemberSetInvocationCount { get; private set; }
        public uint MethodCallInvocationCount { get; private set; }

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

        public ForTestingPurposes()
        {
            MemberGetInvocationCount = 0;
            MemberSetInvocationCount = 0;
            MethodCallInvocationCount = 0;
        }
    }
 }