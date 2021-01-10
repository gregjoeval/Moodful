using System;
using System.ComponentModel.DataAnnotations;

namespace Moodful.Models
{
    public class Tag : Identifiable, IModifiable
    {
        [Required]
        public string Title { get; set; }

        public string Color { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModified { get; set; }
    }
}
