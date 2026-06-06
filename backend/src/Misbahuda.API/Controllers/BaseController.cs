using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Misbahuda.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseController(IMediator mediator) : ControllerBase
{
    protected readonly IMediator Mediator = mediator;
}
