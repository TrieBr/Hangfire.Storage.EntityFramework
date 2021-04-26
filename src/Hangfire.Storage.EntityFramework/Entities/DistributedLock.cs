using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities
{

    /// <summary>
    /// Distributed lock. Primary key is a string which represents the resource
    /// </summary>
    internal class DistributedLock : BaseEntity<string>
    {

        /// <summary>
        /// Date/Time the lock was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

    }


}
