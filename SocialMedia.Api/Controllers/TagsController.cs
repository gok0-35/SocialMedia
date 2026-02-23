using Microsoft.AspNetCore.Mvc;
using SocialMedia.Api.Application.Services;
using SocialMedia.Api.Application.Services.Abstractions;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTags([FromQuery] int skip = 0, [FromQuery] int take = 20, [FromQuery] string? q = null)
    {
        var result = await _tagService.GetTagsAsync(skip, take, q);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending([FromQuery] int take = 10, [FromQuery] int days = 7)
    {
        var result = await _tagService.GetTrendingAsync(take, days);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("{tagName}/posts")]
    public async Task<IActionResult> GetPostsByTag([FromRoute] string tagName, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _tagService.GetPostsByTagAsync(tagName, skip, take);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }
}
