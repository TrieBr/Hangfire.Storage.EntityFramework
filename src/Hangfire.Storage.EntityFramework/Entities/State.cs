using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities
{
    internal class State : BaseEntity<int>
    {

        /// <summary>
        /// Job the parameter is for
        /// </summary>
   
        public Job Job { get; set; }

        /// <summary>
        /// Name of the state
        /// </summary>
        [StringLength(20)]
        public string Name { get; set; }

        /// <summary>
        /// Reason for the state
        /// </summary>
        [StringLength(100)]
        public string Reason { get; set; }

        /// <summary>
        /// Date/Time the job was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Raw Data.
        /// </summary>
        [StringLength(10000)]
        public string Data { get; set; }

    }

}
