using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EnterprisePOS.Shared.Services
{
    public class LicenseService
    {
        // In a real app, use a secure key manager or public/private key pairs.
        private const string SecretKey = "EnterprisePOS_SuperSecretKey_For_License_Generation_2026";
        private readonly SymmetricSecurityKey _securityKey;

        public LicenseService()
        {
            _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        }

        public string GenerateLicenseToken(string tenantId, DateTime expiryDate, bool isPaymentDue)
        {
            var claims = new[]
            {
                new Claim("TenantId", tenantId),
                new Claim("IsPaymentDue", isPaymentDue.ToString()),
                new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expiryDate).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);
            
            var token = new JwtSecurityToken(
                issuer: "EnterprisePOS.Server",
                audience: "EnterprisePOS.Client",
                claims: claims,
                expires: expiryDate,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (bool IsValid, string TenantId, bool IsPaymentDue, DateTime ExpiryDate) ValidateLicenseToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return (false, string.Empty, false, DateTime.MinValue);

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "EnterprisePOS.Server",
                ValidateAudience = true,
                ValidAudience = "EnterprisePOS.Client",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _securityKey,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                
                var tenantId = principal.FindFirst("TenantId")?.Value ?? string.Empty;
                var isPaymentDueStr = principal.FindFirst("IsPaymentDue")?.Value;
                bool isPaymentDue = bool.TryParse(isPaymentDueStr, out var b) && b;
                
                return (true, tenantId, isPaymentDue, validatedToken.ValidTo);
            }
            catch (Exception)
            {
                // Token is invalid or expired
                return (false, string.Empty, false, DateTime.MinValue);
            }
        }
    }
}
