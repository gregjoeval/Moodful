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
    }
}
