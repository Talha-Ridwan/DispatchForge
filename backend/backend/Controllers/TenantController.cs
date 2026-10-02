using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateTenant(TenantRequestDto tenantRequestDto)
    {
        TenantResponseDto created = await _tenantService.CreateTenant(tenantRequestDto);
        return CreatedAtAction(nameof(GetTenant), new { id = created.Id }, created);
    }

    [HttpGet("getAll")]
    public async Task<IActionResult> GetTenants()
    {
        List<TenantResponseDto> tenants = await _tenantService.GetTenants();
        return Ok(tenants);
    }

    [HttpGet("get/{id:guid}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        TenantResponseDto? tenant = await _tenantService.GetTenant(id);
        return tenant == null ? NotFound() : Ok(tenant);
    }

    [HttpPut("update/{id:guid}")]
    public async Task<IActionResult> UpdateTenant(Guid id, TenantRequestDto tenantRequestDto)
    {
        TenantResponseDto? updated = await _tenantService.UpdateTenant(id, tenantRequestDto);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpDelete("delete/{id:guid}")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        var what = await _tenantService.DeleteTenant(id);
        return what ? Accepted(): NotFound();
    }
}
