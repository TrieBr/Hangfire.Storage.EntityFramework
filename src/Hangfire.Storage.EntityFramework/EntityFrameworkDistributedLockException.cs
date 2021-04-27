using System;

namespace Hangfire.Storage.EntityFramework
{
    public class EntityFrameworkDistributedLockException : Exception
    {
        public EntityFrameworkDistributedLockException(string message) : base(message)
        {
        }
    }
}
