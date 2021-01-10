using System;
using System.ComponentModel.DataAnnotations;

namespace Moodful.Models
{
    public abstract class Identifiable
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
