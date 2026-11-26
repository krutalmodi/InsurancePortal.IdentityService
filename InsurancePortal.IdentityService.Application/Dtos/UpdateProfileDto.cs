using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InsurancePortal.IdentityService.Application.DTOs
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class UpdateProfileDto : IValidatableObject
    {
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FirstName == null && LastName == null)
            {
                yield return new ValidationResult("At least one name must be provided.");
            }

            if (FirstName != null && string.IsNullOrWhiteSpace(FirstName))
            {
                yield return new ValidationResult("First name cannot be blank.", new[] { nameof(FirstName) });
            }

            if (LastName != null && string.IsNullOrWhiteSpace(LastName))
            {
                yield return new ValidationResult("Last name cannot be blank.", new[] { nameof(LastName) });
            }
        }
    }
}
