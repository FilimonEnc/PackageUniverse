using System.ComponentModel.DataAnnotations;

namespace PackageUniverse.Core.Entities
{
    public class Entity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime DateCreated { get; set; }

        [Required]
        public DateTime DateUpdated { get; set; }


    }
}
