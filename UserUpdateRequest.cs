using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sabio.Models.Requests.InterrogasUser
{
	public class UserUpdateRequest : UserBaseAddRequest, IModelIdentifier
	{
		[StringLength(1000, MinimumLength = 2)]
		public string FirstName { get; set; }

		[StringLength(1000, MinimumLength = 2)]
		public string LastName { get; set; }

		[StringLength(1000, MinimumLength = 2)]
		public string AvatarUrl { get; set; }
		
		[RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", ErrorMessage = "Must Contain 8 Characters, One Uppercase, One Lowercase, One Number and one special case Character")]
		[StringLength(100, MinimumLength = 10)]
		public string Password { get; set; }

		[StringLength(100, MinimumLength = 10)]
		[Compare("Password", ErrorMessage = "Password must match")]
		
		public string ConfirmPassword { get; set; }
		public int Id { get; set; }
	}
}
