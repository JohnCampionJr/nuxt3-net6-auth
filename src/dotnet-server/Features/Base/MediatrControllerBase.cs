﻿namespace Features.Base;

[ApiController]
[Route("api/[controller]/[action]")]
public class MediatrControllerBase : ControllerBase
{

    protected readonly ISender Sender;

    public MediatrControllerBase(ISender sender)
    {
        Sender = sender;
    }

    protected async Task<IActionResult> Send<T>(IRequest<T> request)
    {
        var result = await Sender.Send(request);
        if (result is BaseResult baseResult)
        {
            if (!baseResult.IsSuccess)
            {
                return BadRequest(result);
            }
        }
        return Ok(result);
    }
}
