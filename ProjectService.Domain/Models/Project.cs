using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectService.Domain.Models;

public class Project
{
    [Key]
    public Ulid Id { get; set; }
    
    [Required, MaxLength(32)]
    public string Name { get; set; }
    
    [Required, MaxLength(32)]
    public string Slug { get; set; }
    
    [StringLength(256)]
    public string? Summary { get; set; }
    
    public string? Description { get; set; }
}