using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Moodful.Models
{
    public class Review : Identifiable, IModifiable
    {
        public bool Secret { get; set; }

        [Required]
        public int Rating { get; set; }

        public string Description { get; set; }

        public IEnumerable<Guid> TagIds { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModified { get; set; }
    }
}
