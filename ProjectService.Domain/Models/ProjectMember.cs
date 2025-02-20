using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectService.Domain.Models;

public class ProjectMember
{
    [Key]
    public Ulid Id { get; set; }
    
    [ForeignKey(nameof(Project))]
    public Ulid ProjectId { get; set; }
    
    [StringLength(32)]
    public string UserId { get; set; }
    
    public bool IsOwner { get; set; }
    
    [StringLength(16)]
    public string Role { get; set; }
    
    public ProjectMemberPermissions Permissions { get; set; }
}