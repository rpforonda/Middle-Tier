using Microsoft.AspNetCore.Hosting;
using Sabio.Data;
using Sabio.Data.Providers;
using Sabio.Models;
using Sabio.Models.Domain;
using Sabio.Models.Requests.InterrogasUser;
using Sabio.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sabio.Services
{
    public class UserService : IUserService
    {
        private IAuthenticationService<int> _authenticationService;
        private IDataProvider _dataProvider;
        private IEmailService _emailService;
        public UserService(IAuthenticationService<int> authSerice, IDataProvider dataProvider, IEmailService emailService)
        {
            _authenticationService = authSerice;
            _dataProvider = dataProvider;
            _emailService = emailService;
        }

        public async Task<bool> LogInAsync(string email, string password)
        {
            bool isSuccessful = false;

            IUserAuthData response = Get(email, password);
             
            if (response != null)
            {
                Claim fullName = new Claim("CustomClaim", "Sabio Bootcamp");
                await _authenticationService.LogInAsync(response, new Claim[]{ fullName});
                isSuccessful = true;
            }

            return isSuccessful;
        }

        public async Task<bool> LogInTest(string email, string password, int id, string[] roles = null)
        {
            bool isSuccessful = false;
            var testRoles = new[] { "User", "Super", "Content Manager" };

            var allRoles = roles == null ? testRoles : testRoles.Concat(roles);

            IUserAuthData response = new UserBase
            {
                Id = id
                ,
                Name = email
                ,
                Roles = allRoles
                ,
                TenantId = "Acme Corp UId"
            };

            Claim fullName = new Claim("CustomClaim", "Sabio Bootcamp");
            await _authenticationService.LogInAsync(response, new Claim[] { fullName });

            return isSuccessful;
        }


        public int Create(UserAddRequest userModel)
        {           
            int userId = 0;
            string password = userModel.Password;

            //Generate a salt and add it to a hashed password to be stored in the DB
            string salt = BCrypt.BCryptHelper.GenerateSalt();
            string hashedPassword = BCrypt.BCryptHelper.HashPassword(password, salt);

            //Generate a guid to act as a User Token 
            Guid newUserToken = Guid.NewGuid();
            string procName = "dbo.Users_Insert_Transaction";

            //DB provider call to create user and get us a user id
            _dataProvider.ExecuteNonQuery(procName,
                inputParamMapper: delegate (SqlParameterCollection col)
                {
                    MapParameters(userModel, col, hashedPassword, newUserToken);

                    SqlParameter idOut = new SqlParameter("@Id", SqlDbType.Int);
                    idOut.Direction = ParameterDirection.Output;

                    col.Add(idOut);
                }, returnParameters: delegate (SqlParameterCollection returnCollection)
                {
                    _emailService.SendConfirmationEmail(userModel, newUserToken.ToString());

                    object oId = returnCollection["@Id"].Value;

                    int.TryParse(oId.ToString(), out userId);
                    Console.WriteLine("");

                    
                });          
            return userId;
        }
        public void UpdateUser(UserUpdateRequest model)
        {

            string procName = "[dbo].[Users_Update_UserProfile]";

            _dataProvider.ExecuteNonQuery(procName, inputParamMapper: delegate (SqlParameterCollection col)
            {
                UserUpdateParameters(model, col);
                col.AddWithValue("@Id", model.Id);

            }, returnParameters: null);
        }
        public void UpdatePass(UserUpdateRequest model)
        {
            string password = model.Password;

            //Generate a salt and add it to a hashed password to be stored in the DB
            string salt = BCrypt.BCryptHelper.GenerateSalt();
            string hashedPassword = BCrypt.BCryptHelper.HashPassword(password, salt);
            string procName = "[dbo].[Users_Update_UserPass]";

            _dataProvider.ExecuteNonQuery(procName, inputParamMapper: delegate (SqlParameterCollection col)
            {
                UserPassParameters(model, hashedPassword, col);
                col.AddWithValue("@Id", model.Id);

            }, returnParameters: null);
        }
        public UserUpdateRequest GetById(int id)
        {
            string storedProc = "[dbo].[Users_Select_UserProfile]";
            UserUpdateRequest userProfile = null;

            _dataProvider.ExecuteCmd(storedProc, inputParamMapper: delegate (SqlParameterCollection col)
            {
                col.AddWithValue("@Id", id);
            }, singleRecordMapper: delegate (IDataReader reader, short set)
            {
                int startingIndex = 0;
                userProfile = MapProfile(reader, ref startingIndex);
            }, returnParameters: null);

            return userProfile;
        }
        public void ConfirmUserStatus (string token)
        {
            string procName = "[dbo].[Users_UpdateConfirmed_Transac]";
            _dataProvider.ExecuteNonQuery(procName,
                inputParamMapper: delegate (SqlParameterCollection col)
                {
                    col.AddWithValue("@token", token);
                });
        }
        
        private static void MapParameters (UserAddRequest model, SqlParameterCollection col, string hashedPassword, Guid token)
        {
                    col.AddWithValue("@Email", model.Email);
                    col.AddWithValue("@FirstName", model.FirstName);
                    col.AddWithValue("@LastName", model.LastName);
                    col.AddWithValue("@Password", hashedPassword);
                    col.AddWithValue("@Token", token);
        }

        private static void UserUpdateParameters(UserUpdateRequest model, SqlParameterCollection col)
        {
            col.AddWithValue("@FirstName", model.FirstName);
            col.AddWithValue("@LastName", model.LastName);
            col.AddWithValue("@AvatarUrl", model.AvatarUrl);
        }
        private static void UserPassParameters(UserUpdateRequest model, string hashedPassword, SqlParameterCollection col)
        {
            col.AddWithValue("@Password", hashedPassword);
        }
        private UserUpdateRequest MapProfile(IDataReader reader, ref int startingIndex)
        {
            UserUpdateRequest userProfile = new UserUpdateRequest();

            userProfile.Id = reader.GetSafeInt32(startingIndex++);
            userProfile.FirstName = reader.GetSafeString(startingIndex++);
            userProfile.LastName = reader.GetSafeString(startingIndex++);
            userProfile.AvatarUrl = reader.GetSafeString(startingIndex++);

            return userProfile;
        }
        private static UserBase MapUser(IDataReader reader, ref int idx)
        {
            UserBase user = new UserBase();
            user.Roles = new List<string>() { };
            List<Role> list = new List<Role>();
            List<string> strings = new List<string>();
                            
                       
            user.Id = reader.GetSafeInt32(idx++);
            user.Name = reader.GetString(idx++);
            user.TenantId = "InterrogasUser" + user.Id;

            string roles = reader.GetSafeString(idx++);

            if (!string.IsNullOrEmpty(roles))
            {
                list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Role>>(roles);
            }

            list.ForEach(role => strings.Add(role.role));

            user.Roles = strings;

            return user;
        }
        private IUserAuthData Get(string email, string password)
        {
            //create a container for db password
            string passwordFromDb = "";
            UserBase user = null;

            string procName = "dbo.Users_TempAuth_Transac";
            int idx = 0;

            //generate a GUID to be used as a token for user authentication
            Guid guid = Guid.NewGuid();

            //get user object from db;
            _dataProvider.ExecuteCmd(procName,
               inputParamMapper: delegate (SqlParameterCollection param)
               {
                   param.AddWithValue("@Email", email);
                   param.AddWithValue("@Token", guid.ToString());
               }
               , singleRecordMapper: delegate (IDataReader reader, short set)
               {

                   passwordFromDb = reader.GetSafeString(idx++);
                   //check if the password matches the hashed password from db

                   bool isValidCredentials = BCrypt.BCryptHelper.CheckPassword(password, passwordFromDb);

                   //store User info into UserBase and return it if credentials are valid
                   if (isValidCredentials)
                   {
                       user = MapUser(reader, ref idx);
                   }
                  
               });

            return user;
        }
        public BaseUser Map(IDataReader reader, ref int idx)
        {
            BaseUser user = new BaseUser();
            user.Id = reader.GetSafeInt32(idx++);
            user.Email = reader.GetSafeString(idx++);
            user.FirstName = reader.GetSafeString(idx++);
            user.LastName = reader.GetSafeString(idx++);
            user.AvatarUrl = reader.GetSafeString(idx++);

            return user;

        }
    }
}