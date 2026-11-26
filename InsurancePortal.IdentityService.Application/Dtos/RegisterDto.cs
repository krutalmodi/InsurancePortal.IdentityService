using System.ComponentModel.DataAnnotations;

namespace InsurancePortal.IdentityService.Application.DTOs
{
    public record RegisterDto(
        string Email,
        string Password,
        [StringLength(100)] string? FirstName = null,
        [StringLength(100)] string? LastName = null,
        [StringLength(100)] string? Username = null);
}
