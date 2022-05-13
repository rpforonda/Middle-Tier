using Int.Models;
using Int.Models.Domain;
using Int.Models.Requests.InterrogasUser;
using System.Data;
using System.Threading.Tasks;

namespace Int.Services
{
    public interface IUserService
    {
        int Create(UserAddRequest userModel);

        Task<bool> LogInAsync(string email, string password);

        Task<bool> LogInTest(string email, string password, int id, string[] roles = null);

        void ConfirmUserStatus(string token);
        void UpdatePass(UserUpdateRequest model);
        void UpdateUser(UserUpdateRequest model);

        BaseUser Map(IDataReader reader, ref int idx);
        UserUpdateRequest GetById(int id);

    }
}
