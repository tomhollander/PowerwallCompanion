using System;
using System.Collections.Generic;
using System.Text;

namespace PowerwallCompanion.Lib
{
    public class NoDataException : Exception
    {
        public NoDataException() : base() { }

        public NoDataException(string message) : base(message) { }

        public NoDataException(string message, Exception innerException) : base(message, innerException) { }
    }
}
