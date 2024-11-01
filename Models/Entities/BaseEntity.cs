using System.ComponentModel.DataAnnotations;

namespace UserAuthentication_ASPNET.Models.Entities
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; init; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}