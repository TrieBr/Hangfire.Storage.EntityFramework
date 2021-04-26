using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities
{
    internal class AggregatedCounter : ExpireEntity<int>
    {
        /// <summary>
        /// Key of the counter
        /// </summary>
        [StringLength(100)]
        public string Key { get; set; }

        /// <summary>
        /// Value associated with the key.
        /// </summary>
        public int Value { get; set; }

    }


}
