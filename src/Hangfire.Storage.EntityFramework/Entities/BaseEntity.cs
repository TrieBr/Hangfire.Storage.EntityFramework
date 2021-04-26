using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities
{
    /// <summary>
    /// Basic entity with a primary key of type TKey
    /// </summary>
    /// <typeparam name="TKey">primary key type</typeparam>
    internal class BaseEntity<TKey>
    {
        [Key]
        public TKey Id { get; set; }
    }
}
