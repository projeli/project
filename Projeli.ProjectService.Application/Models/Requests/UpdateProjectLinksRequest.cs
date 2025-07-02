using Projeli.ProjectService.Domain.Models;

namespace Projeli.ProjectService.Application.Models.Requests;

public class UpdateProjectLinksRequest
{
    public required IEnumerable<UpdateProjectLinkRequest> Links { get; set; }
    
    public class UpdateProjectLinkRequest
    {
        public required string Name { get; set; }
        public required string Url { get; set; }
        public required ProjectLinkType Type { get; set; }
        public required ushort Order { get; set; }
    }
}