using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Int.Models;
using Int.Models.Domain;
using Int.Models.Requests.InterrogasUser;
using Int.Services;
using Int.Web.Controllers;
using Int.Web.Models.Responses;
using System;
using System.Threading.Tasks;

namespace Int.Web.Api.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserAuthApiController : BaseApiController
    {
        IUserService _userService = null;
        private IAuthenticationService<int> _authService;
        public UserAuthApiController(IUserService service, IAuthenticationService<int> authService, ILogger<UserAuthApiController> logger) : base(logger)
        {
            _userService = service;
            _authService = authService;
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult<ItemResponse<int>> Create(UserAddRequest model)
        {

            ObjectResult result = null;

            try
            {
                int id = _userService.Create(model);
                ItemResponse<int> response = new ItemResponse<int>() { Item = id };
                result = Created201(response);
            }
            catch (Exception ex)
            {

                Logger.LogError(ex.ToString());
                ErrorResponse response = new ErrorResponse(ex.Message);
                result = StatusCode(500, response);
            }

            return result;
        }

        [HttpPut("confirm")]
        [AllowAnonymous]
        public ActionResult<SuccessResponse> Update(string token)
        {
            int code = 200;
            BaseResponse response = null;
            try
            {
                _userService.ConfirmUserStatus(token);
                response = new SuccessResponse();
            }
            catch (Exception ex)
            {

                code = 500;
                response = new ErrorResponse($"{ex.Message}");
            }


            return StatusCode(code, response);
        }





        [HttpPost("login")]
        [AllowAnonymous]
        public ActionResult<SuccessResponse> LogInAsync(UserAuthRequest model)
        {

            int code = 200;
            BaseResponse response = null;

            try
            {
                bool isUser = _userService.LogInAsync(model.Email, model.Password).Result;
                if (isUser)
                {
                    response = new SuccessResponse();
                }

                else
                {
                    code = 400;
                    response = new ErrorResponse("Invalid Credentials");
                }

            }
            catch (Exception ex)
            {

                code = 500;
                response = new ErrorResponse($"{ex.Message}");
            }
            return StatusCode(code, response);
        }

        [HttpGet("current")]
        [AllowAnonymous]
        public ActionResult<ItemResponse<IUserAuthData>> GetCurrrent()
        {


            int code = 200;
            BaseResponse response = null;

            try
            {
                IUserAuthData user = _authService.GetCurrentUser();

                if (user == null)
                {
                    code = 404;
                    response = new ErrorResponse("Resource not found");
                }

                else
                {
                    response = new ItemResponse<IUserAuthData> { Item = user };
                }


            }
            catch (Exception ex)
            {

                code = 500;
                response = new ErrorResponse($"Generic Error: {ex.Message}");
            }

            return StatusCode(code, response);
        }

        [HttpGet("logout")]
        public async Task<ActionResult<SuccessResponse>> LogutAsync()
        {

            int code = 200;
            BaseResponse response = null;

            try
            {
                await _authService.LogOutAsync();
                response = new SuccessResponse();

            }
            catch (Exception ex)
            {

                code = 500;
                response = new ErrorResponse($"Generic Error: {ex.Message}");
            }
            return StatusCode(code, response);
        }

        [HttpPut("profile/edit/{id:int}")]
        public ActionResult<SuccessResponse> UpdateProfile(UserUpdateRequest model)
        {
            int code = 200;
            BaseResponse response = null;

            try
            {
                int userId = _authService.GetCurrentUserId();
                _userService.UpdateUser(model);

                response = new SuccessResponse();

                return Ok(response);
            }
            catch (Exception ex)
            {
                code = 500;
                response = new ErrorResponse(ex.Message);
            }

            return StatusCode(code, response);
        }
        [HttpPut("pass/{id:int}")]
        public ActionResult<SuccessResponse> UpdatePass(UserUpdateRequest model)
        {
            int code = 200;
            BaseResponse response = null;

            try
            {
                int userId = _authService.GetCurrentUserId();
                _userService.UpdatePass(model);

                response = new SuccessResponse();

                return Ok(response);
            }
            catch (Exception ex)
            {
                code = 500;
                response = new ErrorResponse(ex.Message);
            }

            return StatusCode(code, response);
        }
        [HttpGet("{id:int}")]
        public ActionResult<ItemResponse<UserUpdateRequest>> GetById(int id)
        {
            int code = 200;
            BaseResponse response;
            try
            {
                UserUpdateRequest userProfile = _userService.GetById(id);
                if (userProfile == null)
                {
                    code = 404;
                    response = new ErrorResponse("Application Resource not found.");
                }
                else
                {
                    response = new ItemResponse<UserUpdateRequest> { Item = userProfile };
                }
            }
            catch (Exception ex)
            {
                code = 500;
                Logger.LogError(ex.ToString());
                response = new ErrorResponse(ex.Message);
            }
            return StatusCode(code, response);
        }
    }
}
