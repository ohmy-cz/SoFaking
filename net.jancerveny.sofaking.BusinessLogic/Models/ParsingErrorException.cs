using System;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
    public class ParsingErrorException : Exception
    {
        public ParsingErrorException(string message) : base(message)
        {
        }
    }
}
