using System;

namespace SlackCoffee
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string msg) : base(msg)
        { }
    }
}
