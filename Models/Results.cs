using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class Result
{
    [Key]
    public int ResultId { get; set; }

    [Required]
    public int EnrolmentId { get; set; }
    [ForeignKey("EnrolmentId")]
    public Enrolment? Enrolment { get; set; }

    public TimeSpan FinishTime { get; set; }

    public int FinishPosition { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}