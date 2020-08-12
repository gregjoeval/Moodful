using System;
using System.ComponentModel.DataAnnotations;

namespace Moodful.Models
{
    public class Tag
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Color { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? LastModified { get; set; } = null;
    }
}
