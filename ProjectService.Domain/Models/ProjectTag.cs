using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ProjectService.Domain.Models;

[Index(nameof(Name), IsUnique = true)]
public class ProjectTag
{
    [Key]
    public Ulid Id { get; set; }
    
    [Required, MaxLength(24)]
    public string Name { get; set; }
    
    
    public List<Project> Projects { get; set; } = [];
}