using Microsoft.AspNetCore.Mvc;

namespace SocialMedia.Api.Application.Services;

public static class ServiceErrorExtensions
{
    public static IActionResult ToActionResult(this ControllerBase controller, ServiceError error)
    {
        return error.Type switch
        {
            ServiceErrorType.BadRequest => controller.BadRequest(error.Message),
            ServiceErrorType.Unauthorized => controller.Unauthorized(error.Message),
            ServiceErrorType.Forbidden => controller.Forbid(),
            ServiceErrorType.NotFound => controller.NotFound(error.Message),
            _ => controller.BadRequest(error.Message)
        };
    }
}

