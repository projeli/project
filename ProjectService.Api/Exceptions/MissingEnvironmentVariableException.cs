using ProjectService.Domain.Exceptions;

namespace ProjectService.Api.Exceptions;

public class MissingEnvironmentVariableException(string missingEnvironmentVariable)
    : ProjectServiceException("Missing environment variable: " + missingEnvironmentVariable);