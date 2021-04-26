using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities
{
    internal class Hash : ExpireEntity<int>
    {
        /// <summary>
        /// Key of the hash
        /// </summary>
        [StringLength(100)]
        public string Key { get; set; }

        /// <summary>
        /// Field Name
        /// </summary>
        [StringLength(40)]
        public string Field { get; set; }


        /// <summary>
        /// Value
        /// </summary>
        [StringLength(10000)]
        public string Value { get; set; }

    }

}
