using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using InsurancePortal.IdentityService.Api.Controllers;
using InsurancePortal.IdentityService.Application.DTOs;
using InsurancePortal.IdentityService.Application.Interfaces;
using InsurancePortal.IdentityService.Application.Queries;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace InsurancePortal.IdentityService.UnitTest.Controllers
{
    /// <summary>
    /// Unit tests for HomeController
    /// </summary>
    public class HomeControllerTests
    {
        private readonly HomeController _controller;
        private readonly Mock<ICurrentUserContext> _currentUser = new();
        private readonly Mock<IUserQueries> _queries = new();

        public HomeControllerTests()
        {
            _controller = new HomeController();
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentUser.Setup(x => x.Current).Returns(new CurrentUserContext(userId, "test@example.com", null, null, null,
                tenantId, null, Array.Empty<string>(), Array.Empty<string>(), false, true));
            _queries.Setup(x => x.GetCurrentUserProfileAsync(userId)).ReturnsAsync(new CurrentUserProfileDto(
                userId, tenantId, null, "test@example.com", "jdoe", "Jane", "Doe", false, true, true, Array.Empty<string>()));
        }

        /// <summary>
        /// Test: Index method should return 200 OK with welcome message
        /// </summary>
        [Fact]
        public async Task Index_WithValidUser_ShouldReturnOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userEmail = "test@example.com";

            var claims = new List<Claim>
            {
                new Claim("email", userEmail),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(hc => hc.User).Returns(claimsPrincipal);

            var controllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
            };

            _controller.ControllerContext = controllerContext;

            // Act
            var result = await _controller.Index(_currentUser.Object, _queries.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var response = Assert.IsType<AuthenticatedUserResponseDto>(okResult.Value);
            Assert.Contains("Welcome", response.Message);
            Assert.Equal(userEmail, response.AuthenticatedUser);
            Assert.Equal("Jane", response.FirstName);
            Assert.Equal("Doe", response.LastName);
            Assert.Equal("jdoe", response.Username);
        }

        /// <summary>
        /// Test: Index method should return 200 OK with email from JWT claims
        /// </summary>
        [Fact]
        public async Task Index_ShouldExtractEmailFromJwtClaims()
        {
            // Arrange
            var userEmail = "authenticated@example.com";

            var claims = new List<Claim>
            {
                new Claim("email", userEmail),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(hc => hc.User).Returns(claimsPrincipal);

            var controllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
            };

            _controller.ControllerContext = controllerContext;

            // Act
            var result = await _controller.Index(_currentUser.Object, _queries.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var response = Assert.IsType<AuthenticatedUserResponseDto>(okResult.Value);
            Assert.Equal(userEmail, response.AuthenticatedUser);
            Assert.Contains("Welcome", response.Message);
            Assert.NotNull(response.Timestamp);
            Assert.NotNull(response.Description);
        }

        [Fact]
        public async Task Index_WithoutCurrentUser_ReturnsUnauthorized()
        {
            _currentUser.Setup(x => x.Current).Returns((CurrentUserContext?)null);

            var result = await _controller.Index(_currentUser.Object, _queries.Object);

            Assert.IsType<UnauthorizedResult>(result);
            _queries.Verify(x => x.GetCurrentUserProfileAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Index_WithDifferentTenant_ReturnsNotFound()
        {
            var current = _currentUser.Object.Current!;
            _queries.Setup(x => x.GetCurrentUserProfileAsync(current.UserId)).ReturnsAsync(new CurrentUserProfileDto(
                current.UserId, Guid.NewGuid(), null, "test@example.com", "jdoe", "Jane", "Doe", false, true, true, Array.Empty<string>()));

            var result = await _controller.Index(_currentUser.Object, _queries.Object);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
