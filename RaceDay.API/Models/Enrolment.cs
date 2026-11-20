using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class Enrolment
{
    [Key]
    public int EnrolmentId { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int ParticipantId { get; set; }
    [ForeignKey("ParticipantId")]
    public User? Participant { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(10)]
    public string Status { get; set; } = "Pending"; // 'Pending', 'Confirmed', 'Cancelled'

    // Navigation properties with Composite FK configuration (handled in DbContext)
    [ForeignKey("EventId")]
    public Event? Event { get; set; }

    [ForeignKey("EventId, CategoryId")]
    public Category? Category { get; set; }

    public Result? Result { get; set; }
}