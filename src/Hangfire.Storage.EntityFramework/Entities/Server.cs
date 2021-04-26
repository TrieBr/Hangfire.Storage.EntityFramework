using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities
{
    internal class Server : BaseEntity<int>
    {

        /// <summary>
        /// Date/Time of Last Heart beat from the server
        /// </summary>
        public DateTime? LastHeartBeat { get; set; }

        /// <summary>
        /// Raw Data.
        /// </summary>
        [StringLength(10000)]
        public string Data { get; set; }

    }

}
