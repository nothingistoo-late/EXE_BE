using DTOs.DiscountDTOs.Request;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountService _discountService;

    public DiscountsController(IDiscountService discountService)
    {
        _discountService = discountService;
    }

    // GET: api/discounts
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _discountService.GetAllDiscountsAsync();
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // GET: api/discounts/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _discountService.GetDiscountByIdAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/discounts
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DiscountCreateDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _discountService.CreateDiscountAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // PUT: api/discounts/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DiscountUpdateDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _discountService.UpdateDiscountAsync(id, dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/discounts/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _discountService.DeleteDiscountAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
