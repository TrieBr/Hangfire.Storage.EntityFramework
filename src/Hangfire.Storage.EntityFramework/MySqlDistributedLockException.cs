using System;

namespace Hangfire.Storage.EntityFramework
{
    public class MySqlDistributedLockException : Exception
    {
        public MySqlDistributedLockException(string message) : base(message)
        {
        }
    }
}
