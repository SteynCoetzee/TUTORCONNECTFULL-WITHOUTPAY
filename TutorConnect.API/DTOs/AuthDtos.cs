using System.Text.Json.Serialization;

namespace TutorConnect.API.DTOs
{
    public class UserRegisterDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; } = 3;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Bio { get; set; }
    }

    public class UserLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserProfileDto
    {
        [JsonPropertyName("user_ID")]
        public int User_ID { get; set; }
        
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;
        
        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;
        
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
        
        [JsonPropertyName("address")]
        public string? Address { get; set; }
        
        [JsonPropertyName("bio")]
        public string? Bio { get; set; }
        
        [JsonPropertyName("user_Role_ID")]
        public int User_Role_ID { get; set; }

        [JsonPropertyName("roleName")]
        public string? RoleName { get; set; }
    }

    public class UserProfileUpdateDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Bio { get; set; }
    }

    public class StudentProfileDto
    {
        public string? Student_Number { get; set; }
        public string? Faculty { get; set; }
        public int? Year_Of_Study { get; set; }
        public string? Degree_Program { get; set; }
    }

    public class TutorProfileDto
    {
        public string? Qualifications { get; set; }
        public string? Specialization { get; set; }
        public int? Years_Of_Experience { get; set; }
    }

    public class AdminProfileDto
    {
        public string? Job_Title { get; set; }
    }

    public class AdminUserUpdateDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Bio { get; set; }
        public int RoleId { get; set; }
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string ResetCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ChangeRoleDto
    {
        public int RoleId { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}