﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolProject.Data.Entities.Identities;
using SchoolProject.Data.Helpers;
using SchoolProject.Data.Responses;
using SchoolProject.Infrustructure.Data;
using SchoolProject.Infrustructure.Interfaces;
using SchoolProject.Service.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SchoolProject.Service.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        #region Fields
        private readonly JwtSettings _jwtSettings;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly UserManager<User> _userManager;
        private readonly IEmailsService _emailsService;
        private readonly AppDbContext _dbContext;
        #endregion

        #region Constructors
        public AuthenticationService(JwtSettings jwtSettings,
                                     IRefreshTokenRepository refreshTokenRepository,
                                     UserManager<User> userManager,
                                     IEmailsService emailsService,
                                     AppDbContext dbContext)
        {
            _jwtSettings = jwtSettings;
            _refreshTokenRepository = refreshTokenRepository;
            _userManager = userManager;
            _emailsService = emailsService;
            _dbContext = dbContext;
        }


        #endregion

        #region Handle Functions
        public async Task<JwtAuthResponse> GetJWTToken(User user)
        {
            // Generate Access Token
            var (jwtToken, accessToken) = await GenerateJWTToken(user);

            // Generate Refresh Token && Store It In Database
            var refreshToken = GetRefreshToken(user.UserName);

            var refreshTokenToStore = new UserRefreshToken
            {
                UserId = user.Id,
                RefreshToken = refreshToken.TokenString,
                AccessToken = accessToken,
                JwtId = jwtToken.Id,
                IsUsed = true,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMonths(_jwtSettings.RefreshTokenExpireDate),
            };

            // Store Refresh Token To Database
            await _refreshTokenRepository.AddAsync(refreshTokenToStore);

            // Return JwtAuthResponse
            return new JwtAuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            };


        }

        private async Task<(JwtSecurityToken, string)> GenerateJWTToken(User user)
        {
            // Get User Claims
            var claims = await GetClaims(user);

            // JWT Object
            var jwtToken = new JwtSecurityToken(
                _jwtSettings.Issuer,
                _jwtSettings.Audience,
                claims,
                expires: DateTime.Now.AddDays(_jwtSettings.AccessTokenExpireDate),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Secret)), SecurityAlgorithms.HmacSha256Signature));

            // Converts the JWT object to a string representation (access token).
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            // Returns a tuple containing both the JWT object and the access token string.
            return (jwtToken, accessToken);
        }

        private RefreshToken GetRefreshToken(string userName)
        {
            // Return a new RefreshToken object
            return new RefreshToken
            {
                UserName = userName,
                ExpierdAt = DateTime.UtcNow.AddMonths(_jwtSettings.RefreshTokenExpireDate),
                TokenString = GenerateRefreshToken()  // Generate a random string for the refresh token

            };
        }

        public async Task<JwtAuthResponse> GetRefreshToken(User user, JwtSecurityToken jwtToken, DateTime? expireDate, string refreshToken)
        {
            // Generate New JWT Token and Deconstruct the result 
            var (jwtSecurityToken, newToken) = await GenerateJWTToken(user);

            // Create an object of JwtAuthResponse and one of the RefreshToken
            var refreshTokenResult = new RefreshToken();
            refreshTokenResult.UserName = jwtToken.Claims.FirstOrDefault(x => x.Type == nameof(UserClaimModel.UserName)).Value;
            refreshTokenResult.TokenString = refreshToken;
            refreshTokenResult.ExpierdAt = (DateTime)expireDate;

            var response = new JwtAuthResponse();
            response.AccessToken = newToken;
            response.RefreshToken = refreshTokenResult;

            return response;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            var randomNumberGenerator = RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<List<Claim>> GetClaims(User user)
        {
            // Get User's Roles
            var userRoles = await _userManager.GetRolesAsync(user);

            // Create claims 
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name,user.UserName),
                new Claim(ClaimTypes.Email,user.Email),
                new Claim(nameof(UserClaimModel.PhoneNumber), user.PhoneNumber),
                new Claim(nameof(UserClaimModel.Id), user.Id.ToString())
            };

            // Add User's Roles to the claims list
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Get User's Claims
            var userClaims = await _userManager.GetClaimsAsync(user);

            // Add User's Claims to claims list
            claims.AddRange(userClaims);

            // Return the claims list that will be added to the token when generate it
            return claims;
        }

        public JwtSecurityToken ReadJWTToken(string accessToken)
        {
            // Check If The Access Token Null Or Empty
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            // Parse the access token string into a JwtSecurityToken object
            var response = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

            // Return response object that contains the claims and other information encoded within the token.
            return response;

        }

        public async Task<string> ValidateToken(string accessToken)
        {
            // Specify the validation rules
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = _jwtSettings.ValidateIssuer,
                ValidIssuers = new[] { _jwtSettings.Issuer },
                ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Secret)),
                ValidAudience = _jwtSettings.Audience,
                ValidateAudience = _jwtSettings.ValidateAudience,
                ValidateLifetime = _jwtSettings.ValidateLifeTime,
            };


            try
            {
                // Validate the access token against the specified parameters
                var validator = new JwtSecurityTokenHandler().ValidateToken(accessToken, parameters, out SecurityToken validatedToken);

                // checks if the token is valid
                if (validator == null)
                {
                    return "InvalidToken";
                }

                return "NotExpired";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<(string, DateTime?)> ValidateDetails(JwtSecurityToken jwtToken, string accessToken, string refreshToken)
        {
            // Check on the jwt Token if its valid and the algorithm used is the same algorithm in the header
            if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature))
            {
                return ("AlgorithmIsWrong", null);
            }

            // Check if JWT Token is not expired
            if (jwtToken.ValidTo > DateTime.UtcNow)
            {
                return ("TokenIsNotExpired", null);
            }

            // Extracts the user ID from the claims within the token.
            var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == nameof(UserClaimModel.Id)).Value;

            // Searches for a matching refresh token record
            var userRefreshToken = await _refreshTokenRepository.GetTableNoTracking()
                                             .FirstOrDefaultAsync(x => x.AccessToken == accessToken &&
                                                                       x.RefreshToken == refreshToken &&
                                                                       x.UserId == int.Parse(userId));

            // Check If no matching record
            if (userRefreshToken == null)
            {
                return ("RefreshTokenIsNotFound", null);
            }

            // Check its expiry date
            if (userRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                userRefreshToken.IsRevoked = true;
                userRefreshToken.IsUsed = false;
                await _refreshTokenRepository.UpdateAsync(userRefreshToken);
                return ("RefreshTokenIsExpired", null);
            }

            var expirydate = userRefreshToken.ExpiresAt;
            return (userId, expirydate);
        }

        public async Task<string> ConfirmEmail(int userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            var confirmEmail = await _userManager.ConfirmEmailAsync(user, code);

            if (!confirmEmail.Succeeded)
                return "ErrorWhenConfirmEmail";
            return "Success";
        }

        public async Task<string> SendResetPasswordCode(string email)
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Get the user you want to send a code to him by email
                var user = await _userManager.FindByEmailAsync(email);

                // Check if the user is exist
                if (user is null) return "NotFound";

                // Generate a random number => (code)
                Random generator = new Random();
                string randomNumber = generator.Next(0, 1000000).ToString("D6");

                // Save (Update) the code in user table in the database
                user.Code = randomNumber;
                var updateResult = await _userManager.UpdateAsync(user);

                // Check on the result of updating the user table
                if (!updateResult.Succeeded) return "FaildToUpdateTheUser";

                // Send the code you generated to the user by using "SendEmail" Service
                var message = "Code To Reset Passsword : " + user.Code;
                await _emailsService.SendEmail(email, message, "Reset Password Code");
                await transaction.CommitAsync();

                // Success To Send Reset Password Code
                return "Success";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return "Failed";
            }


        }

        public async Task<string> ConfirmResetPassword(string code, string email)
        {
            // Get the user who will confirm the reseting password 
            var user = await _userManager.FindByEmailAsync(email);
            // Check if the user is exist
            if (user is null) return "NotFound";

            // Get the code that saved in the user table to confirm reset password
            var userCode = user.Code;

            // Compare between the code saved in user table and the code that the user entered in the endpoint
            if (userCode == code) return "Success";

            // If not equals
            return "Failed";

        }

        public async Task<string> ResetPassword(string email, string password)
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Get the user who will reset his password 
                var user = await _userManager.FindByEmailAsync(email);
                // Check if the user is exist
                if (user is null) return "NotFound";

                // Delete old password
                await _userManager.RemovePasswordAsync(user);
                // Add new password
                if (!await _userManager.HasPasswordAsync(user))
                {
                    await _userManager.AddPasswordAsync(user, password);
                }
                await transaction.CommitAsync();
                // Success
                return "Success";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return "Failed";
            }
        }




        #endregion
    }
}
