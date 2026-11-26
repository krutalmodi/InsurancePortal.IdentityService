using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InsurancePortal.IdentityService.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace InsurancePortal.IdentityService.UnitTest.Security;

public class HttpCurrentUserContextTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Current_ReadsValidatedJwtClaims_WithOrWithoutInboundMapping(bool mapInboundClaims)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-only-signing-key-with-at-least-32-bytes"));
        var jwt = new JwtSecurityToken(claims: new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("tenant", tenantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "user@example.com"),
            new Claim("given_name", "Alex"),
            new Claim("family_name", "Morgan"),
            new Claim("role", "Registered"),
            new Claim("permission", "quote.read.own")
        }, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = mapInboundClaims };
        var principal = handler.ValidateToken(handler.WriteToken(jwt), new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true
        }, out _);
        var http = new DefaultHttpContext { User = principal };
        var context = new HttpCurrentUserContext(new HttpContextAccessor { HttpContext = http }).Current;

        Assert.NotNull(context);
        Assert.Equal(userId, context.UserId);
        Assert.Equal(tenantId, context.TenantId);
        Assert.Equal("user@example.com", context.Email);
        Assert.Equal("Alex", context.FirstName);
        Assert.Equal("Morgan", context.LastName);
        Assert.Contains("Registered", context.Roles);
        Assert.Contains("quote.read.own", context.Permissions);
    }

    [Fact]
    public void Current_RequiresAuthenticatedIdentity()
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        var context = new HttpCurrentUserContext(new HttpContextAccessor { HttpContext = http }).Current;
        Assert.Null(context);
    }
}
