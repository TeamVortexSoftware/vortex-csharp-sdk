using System;

namespace TeamVortexSoftware.VortexSDK
{
    /// <summary>
    /// Exception thrown when Vortex API operations fail
    /// </summary>
    public class VortexException : Exception
    {
        public VortexException(string message) : base(message)
        {
        }

        public VortexException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
