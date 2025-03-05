﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Projeli.ProjectService.Domain.Models;

[Index(nameof(Slug), IsUnique = true)]
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
    
    public string? Content { get; set; }
    
    [StringLength(128)]
    public string? ImageUrl { get; set; }
    
    public ProjectStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? PublishedAt { get; set; }
    
    public ProjectCategory Category { get; set; } 
        
    
    public List<ProjectMember> Members { get; set; } = [];
    public List<ProjectLink> Links { get; set; } = [];
    public List<ProjectTag> Tags { get; set; } = [];
}