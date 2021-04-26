using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities
{
    internal class Set : ExpireEntity<int>
    {
        /// <summary>
        /// Key of the hash
        /// </summary>
        [StringLength(100)]
        public string Key { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [StringLength(256)]
        public string Value { get; set; }

        /// <summary>
        /// Score
        /// </summary>
        public float Score { get; set; }

    }

}
