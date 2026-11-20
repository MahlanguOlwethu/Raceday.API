using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Role
{
    [Key]
    public int RoleId { get; set; }

    [Required, MaxLength(20)]
    public string RoleName { get; set; } = string.Empty; // 'Organiser' or 'Participant'

    public ICollection<User> Users { get; set; } = new List<User>();
}