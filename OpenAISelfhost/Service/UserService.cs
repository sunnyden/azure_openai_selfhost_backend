using Microsoft.IdentityModel.Tokens;
using OpenAISelfhost.DatabaseContext;
using OpenAISelfhost.DataContracts.DataTables;
using OpenAISelfhost.DataContracts.Enums;
using OpenAISelfhost.Exceptions.Http;
using OpenAISelfhost.Service.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OpenAISelfhost.Service
{
    public class UserService : IUserService
    {
        private readonly ServiceDatabaseContext databaseContext;
        private readonly IConfiguration configuration;
        public UserService(ServiceDatabaseContext databaseContext, IConfiguration configuration)
        {
            this.databaseContext = databaseContext;
            this.configuration = configuration;
        }
        public void CreateUser(string userName, string password, bool isAdmin, double credit, double creditQuota)
        {
            User user = new User()
            {
                UserName = userName,
                Password = GetPasswordHash(userName, password),
                IsAdmin = isAdmin,
                CreditQuota = creditQuota,
                RemainingCredit = credit
            };
            databaseContext.Users.Add(user);
            databaseContext.SaveChanges();
        }

        public IEnumerable<User> GetUsers()
        {
            return databaseContext.Users;
        }

        public void DeleteUser(User user)
        {
            databaseContext.Users.Remove(user);
            databaseContext.SaveChanges();
        }

        public string GetAuthorizationToken(string userName, string password)
        {
            var user = databaseContext.Users.Where(user => user.UserName == userName && user.Password == GetPasswordHash(userName, password)).FirstOrDefault();
            if(user == null)
            {
                throw new AuthorizationException("Invalid username or password");
            }

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Upn, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.IsAdmin ? UserType.Admin : UserType.User)
            };

            return GenerateToken(authClaims);
        }

        public void UpdateUser(User user)
        {
            databaseContext.Users.Update(user);
            databaseContext.SaveChanges();
        }

        public static string GetPasswordHash(string userName, string password)
        {
            var bytes = Encoding.UTF8.GetBytes($"{userName}:{password}");
            var sha512 = SHA512.Create();
            var hash = sha512.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private string GenerateToken(IEnumerable<Claim> claims)
        {
            var secret = configuration["JWT:Secret"];
            if (string.IsNullOrEmpty(secret))
            {
                throw new NullReferenceException("JWT secret is not set");
            }
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = configuration["JWT:ValidIssuer"],
                Audience = configuration["JWT:ValidAudience"],
                Expires = DateTime.UtcNow.AddHours(3),
                SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256),
                Subject = new ClaimsIdentity(claims)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public User? GetUser(string userName)
        {
            return databaseContext.Users.Where(user => user.UserName == userName).FirstOrDefault();
        }

        public User? GetUser(int id)
        {
            return databaseContext.Users.Where(user => user.Id == id).FirstOrDefault();
        }

        public bool UserExists(int userId)
        {
            return databaseContext.Users.Where(user => user.Id == userId).Any();
        }
    }
}
