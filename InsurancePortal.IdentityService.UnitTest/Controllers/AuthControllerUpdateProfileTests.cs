using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using InsurancePortal.IdentityService.Api.Controllers;
using InsurancePortal.IdentityService.Application.DTOs;
using InsurancePortal.IdentityService.Application.Interfaces;
using InsurancePortal.IdentityService.Domain.Entities;
using InsurancePortal.IdentityService.UnitTest.Fixtures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InsurancePortal.IdentityService.UnitTest.Controllers
{
    public class AuthControllerUpdateProfileTests : AuthControllerTestBase
    {
        private readonly AuthController _controller;
        private readonly Mock<ICurrentUserContext> _currentUser = new();
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();

        public AuthControllerUpdateProfileTests()
        {
            _controller = new AuthController(MockUserRepository.Object, MockTokenService.Object, MockPasswordHasher.Object,
                MockHttpClientFactory.Object, MockConfiguration.Object);
            _currentUser.Setup(x => x.Current).Returns(new CurrentUserContext(_userId, "user@example.com", "username",
                "Old", "Name", _tenantId, null, Array.Empty<string>(), Array.Empty<string>(), false, true));
        }

        [Fact]
        public async Task UpdateCurrentUser_UpdatesOnlyProvidedNameForCurrentUser()
        {
            var user = new UserIdentity(_tenantId, "user@example.com")
            {
                Id = _userId, FirstName = "Old", LastName = "Name", Username = "username"
            };
            MockUserRepository.Setup(x => x.GetUserByIdAsync(_userId)).ReturnsAsync(user);

            var result = await _controller.UpdateCurrentUser(new UpdateProfileDto { FirstName = "Jane" }, _currentUser.Object);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal("Jane", user.FirstName);
            Assert.Equal("Name", user.LastName);
            Assert.Equal("username", user.Username);
            Assert.Equal("user@example.com", user.Email);
            MockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateCurrentUser_UpdatesBothNames()
        {
            var user = new UserIdentity(_tenantId, "user@example.com") { Id = _userId };
            MockUserRepository.Setup(x => x.GetUserByIdAsync(_userId)).ReturnsAsync(user);

            var result = await _controller.UpdateCurrentUser(new UpdateProfileDto { FirstName = "Jane", LastName = "Doe" }, _currentUser.Object);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal("Jane", user.FirstName);
            Assert.Equal("Doe", user.LastName);
            MockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateCurrentUser_WithoutCurrentUser_IsUnauthorized()
        {
            _currentUser.Setup(x => x.Current).Returns((CurrentUserContext?)null);
            var result = await _controller.UpdateCurrentUser(new UpdateProfileDto { FirstName = "Jane" }, _currentUser.Object);
            Assert.IsType<UnauthorizedResult>(result);
            MockUserRepository.Verify(x => x.GetUserByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateCurrentUser_MissingOrDifferentTenant_DoesNotSave(bool missing)
        {
            UserIdentity? user = missing ? null : new UserIdentity(Guid.NewGuid(), "user@example.com") { Id = _userId };
            MockUserRepository.Setup(x => x.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            var result = await _controller.UpdateCurrentUser(new UpdateProfileDto { LastName = "Doe" }, _currentUser.Object);
            Assert.IsType<NotFoundResult>(result);
            MockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(" ", null)]
        [InlineData(null, "")]
        public void UpdateProfile_InvalidNames_AreRejected(string? firstName, string? lastName)
        {
            var dto = new UpdateProfileDto { FirstName = firstName, LastName = lastName };
            var errors = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true));
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void UpdateProfile_LengthAndUnknownFields_AreRejected()
        {
            var dto = new UpdateProfileDto { FirstName = new string('a', 101) };
            var errors = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true));
            Assert.Contains(errors, error => error.MemberNames.Contains(nameof(UpdateProfileDto.FirstName)));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<UpdateProfileDto>(
                "{\"firstName\":\"Jane\",\"email\":\"other@example.com\"}", new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }

        [Fact]
        public void UpdateCurrentUser_RequiresBearerAuthorization()
        {
            var method = typeof(AuthController).GetMethod(nameof(AuthController.UpdateCurrentUser))!;
            var authorize = Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>());
            Assert.Equal("Bearer", authorize.AuthenticationSchemes);
        }
    }
}
