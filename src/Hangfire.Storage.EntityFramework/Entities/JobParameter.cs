using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities
{
    internal class JobParameter : BaseEntity<int>
    {

        /// <summary>
        /// Job the parameter is for
        /// </summary>
   
        public Job Job { get; set; }

        /// <summary>
        /// Name of the parameter
        /// </summary>
        [StringLength(40)]
        public string Name { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [StringLength(10000)]
        public string Value { get; set; }

    }

}
