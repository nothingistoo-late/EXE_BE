using DTOs.DiscountDTOs.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize]

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


    [Authorize(Roles = "ADMIN")]

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DiscountCreateDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _discountService.CreateDiscountAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
    [Authorize(Roles = "ADMIN")]

    // PUT: api/discounts/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DiscountUpdateDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _discountService.UpdateDiscountAsync(id, dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
    [Authorize(Roles = "ADMIN")]

    // DELETE: api/discounts/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _discountService.DeleteDiscountAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // GET: api/discounts/validate/{code}
    [HttpGet("validate/{code}")]
    public async Task<IActionResult> ValidateCode(string code)
    {
        var result = await _discountService.ValidateDiscountCodeAsync(code);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
