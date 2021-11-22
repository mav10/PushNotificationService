using System;

namespace MccSoft.PushNotification.Http
{
    public class SignInException : Exception
    {
        public SignInException(string stringContent) : base(stringContent) { }
    }
}
