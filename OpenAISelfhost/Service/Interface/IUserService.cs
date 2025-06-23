using OpenAISelfhost.DataContracts.DataTables;

namespace OpenAISelfhost.Service.Interface
{
    public interface IUserService
    {
        public void UpdateUser(User user);
        public string GetAuthorizationToken(string userName, string password);
        public void CreateUser(string userName, string password, bool isAdmin, double credit, double creditQuota);
        public User? GetUser(string userName);
        public User? GetUser(int id);
        public IEnumerable<User> GetUsers();
        public void DeleteUser(User user);
        public bool UserExists(int userId);
        public bool ValidateToken(string token);
    }
}
