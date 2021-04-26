using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities
{
    /// <summary>
    /// Basic entity with a primary key of type TKey and an expiry column.
    /// </summary>
    /// <typeparam name="TKey">primary key type</typeparam>
    internal class ExpireEntity<TKey> : BaseEntity<TKey>
    {
        /// <summary>
        /// Date/Time the job expires.
        /// </summary>
        public DateTime? ExpireAt { get; set; }
    }
}
