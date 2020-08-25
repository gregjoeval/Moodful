using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Moodful.Models
{
    public class Review
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // TODO: put this on db model
        //public Guid UserId { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? LastModified { get; set; } = null;

        public bool Secret { get; set; }

        [Required]
        public int Rating { get; set; }

        public string Description { get; set; }

        public IEnumerable<Guid> TagIds { get; set; }
    }
}
