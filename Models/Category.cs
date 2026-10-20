using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required]
    public int EventId { get; set; }
    [ForeignKey("EventId")]
    public Event? Event { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string CategoryType { get; set; } = string.Empty; // 'Age' or 'Distance'

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal? DistanceKm { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}