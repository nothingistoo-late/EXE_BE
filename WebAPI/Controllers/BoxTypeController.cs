using DTOs.BoxType.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize]

public class BoxTypesController : ControllerBase
{
    private readonly IBoxTypeService _service;

    public BoxTypesController(IBoxTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("by-parent/{parentId}")]
    public async Task<IActionResult> GetByParent(Guid parentId)
    {
        var result = await _service.GetByParentIdAsync(parentId);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        var result = await _service.SearchByNameAsync(keyword);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // Update (ADMIN)
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBoxTypeRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _service.UpdateAsync(id, request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // Delete (ADMIN)
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
