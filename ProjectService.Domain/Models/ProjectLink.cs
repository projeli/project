using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectService.Domain.Models;

public class ProjectLink
{
    [Key]
    public Ulid Id { get; set; }
    
    [ForeignKey(nameof(Project))]
    public Ulid ProjectId { get; set; }
    
    [Required, MaxLength(16)]
    public string Name { get; set; }
    
    [Required, MaxLength(256)]
    public string Url { get; set; }
    
    public ProjectLinkType Type { get; set; }
    
    
    public Project Project { get; set; }
}