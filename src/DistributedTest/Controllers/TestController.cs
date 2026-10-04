namespace DistributedTest.Controllers;

using DistributedTest.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly ItemsQueryService _itemsQueryService;

    public TestController(ItemsQueryService itemsQueryService)
    {
        _itemsQueryService = itemsQueryService;
    }

    [HttpGet]
    public async Task<IEnumerable<TestData>> Get(CancellationToken cancellationToken) 
        => await _itemsQueryService.GetItemsAsync(cancellationToken);
}