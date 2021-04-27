using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities
{
    internal class Job : ExpireEntity<int>
    {

        /// <summary>
        /// Text version of the state.
        /// </summary>
        [StringLength(20)]
        public string StateName { get; set; }

        /// <summary>
        /// Serialized invocation data.
        /// </summary>
        [StringLength(10000)]
        public string InvocationData { get; set; }

        /// <summary>
        /// Serialized argument data.
        /// </summary>
        [StringLength(10000)]
        public string Arguments { get; set; }

        /// <summary>
        /// Date/Time the job was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Parameters for the job.
        /// </summary>
        public ICollection<JobParameter> Parameters { get; set; }

        /// <summary>
        /// State history of the job.
        /// </summary>
        public ICollection<JobState> StateHistory { get; set; }

    }


}
