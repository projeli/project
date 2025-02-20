using Microsoft.AspNetCore.Mvc;
using ProjectService.Domain.Results;

namespace ProjectService.Api.Controllers;

public abstract class BaseController : ControllerBase
{
    protected IActionResult HandleResult<T>(IResult<T> result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        if (result.Errors.Count > 0)
        {
            var errors = result.Errors
                .ToDictionary(x => x.Key, x => x.Value);

            return ValidationProblem(new ValidationProblemDetails
            {
                Title = result.Message,
                Errors = errors
            });
        }

        return BadRequest(result);
    }
}