using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities
{
    internal class JobQueue : BaseEntity<int>
    {

        /// <summary>
        /// Job the parameter is for
        /// </summary>
   
        public Job Job { get; set; }

        /// <summary>
        /// Last Fetch Time
        /// </summary>
        public DateTime? FetchedAt { get; set; }

        /// <summary>
        /// Name of the Queue
        /// </summary>
        [StringLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// Fetch token of the queue
        /// </summary>
        public Guid FetchToken { get; set; }

    }

}
