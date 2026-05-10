using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using TutorConnect.API.Data;
using TutorConnect.API.DTOs;
using TutorConnect.API.Models;
using TutorConnect.API.Services;

namespace TutorConnect.API.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // AUTH CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly AuditService _audit;

        public AuthController(AppDbContext context, IConfiguration configuration, EmailService emailService, AuditService audit)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _audit = audit;
        }

        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(UserRegisterDto request)
        {
            // 1. Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("User already exists.");
            }

            // 2. Hash the password securely
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Create the new user
            // New tutor registrations start as AW-Tutor (role 4) until admin promotes them
            int assignedRoleId = request.RoleId == 2 ? 4 : request.RoleId;

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = passwordHash,
                User_Role_ID = assignedRoleId,
                Phone = request.Phone,
                Address = request.Address,
                Bio = request.Bio
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(user.User_ID, "User Registered", $"Email: {user.Email}, Role: {assignedRoleId}");

            return Ok("User successfully registered.");
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserLoginDto request)
        {
            var user = await _context.Users
                .Include(u => u.User_Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Wrong password.");
            }

            string token = CreateToken(user);
            await _audit.LogAsync(user.User_ID, "User Login", $"Email: {user.Email}");
            return Ok(token);
        }

        [HttpPut("change-password/{userId}")]
        public async Task<ActionResult<string>> ChangePassword(int userId, ChangePasswordDto request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest("New passwords do not match.");

            if (request.NewPassword.Length < 6)
                return BadRequest("New password must be at least 6 characters.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return BadRequest("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Password updated successfully.");
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<object>> ForgotPassword(ForgotPasswordDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Generate a 6-digit reset code
            var resetCode = new Random().Next(100000, 999999).ToString();
            user.PasswordResetCode = resetCode;
            user.PasswordResetCodeExpiration = DateTime.Now.AddMinutes(15); // Code valid for 15 minutes

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await _emailService.SendResetCodeAsync(user.Email, resetCode);
            return Ok(new { message = "Password reset code sent to your email." });
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<string>> ResetPassword(ResetPasswordDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Validate reset code and expiration
            if (user.PasswordResetCode != request.ResetCode)
            {
                return BadRequest("Invalid reset code.");
            }

            if (user.PasswordResetCodeExpiration == null || DateTime.Now > user.PasswordResetCodeExpiration)
            {
                return BadRequest("Reset code has expired.");
            }

            // Hash the new password and update
            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordHash = newPasswordHash;
            user.PasswordResetCode = null;
            user.PasswordResetCodeExpiration = null;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Password successfully reset.");
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.User_ID.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.User_Role?.User_Role_Name ?? "Student")
            };

            var keyString = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT:Key is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT:Issuer is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                issuer: issuer,
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // USERS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        public UsersController(AppDbContext context, AuditService audit) { _context = context; _audit = audit; }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserProfileDto>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.User_Role)
                .FirstOrDefaultAsync(u => u.User_ID == id);

            if (user == null) return NotFound("User not found.");

            return Ok(new UserProfileDto
            {
                User_ID      = user.User_ID,
                FirstName    = user.FirstName,
                LastName     = user.LastName,
                Email        = user.Email,
                Phone        = user.Phone,
                Address      = user.Address,
                Bio          = user.Bio,
                User_Role_ID = user.User_Role_ID,
                RoleName     = user.User_Role?.User_Role_Name
            });
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserProfileUpdateDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Phone = request.Phone;
            user.Address = request.Address;
            user.Bio = request.Bio;

            await _context.SaveChangesAsync();
            return Ok("Profile updated successfully.");
        }

        // GET: api/Users (Admin only - list all users)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetAllUsers()
        {
            var users = await _context.Users
                .Include(u => u.User_Role)
                .ToListAsync();

            return Ok(users.Select(u => new UserProfileDto
            {
                User_ID      = u.User_ID,
                FirstName    = u.FirstName,
                LastName     = u.LastName,
                Email        = u.Email,
                Phone        = u.Phone,
                Address      = u.Address,
                Bio          = u.Bio,
                User_Role_ID = u.User_Role_ID,
                RoleName     = u.User_Role?.User_Role_Name
            }));
        }

        // PUT: api/Users/{id}/admin (Admin only - update full profile + role)
        [HttpPut("{id}/admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUpdateUser(int id, AdminUserUpdateDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            var roleExists = await _context.User_Roles.AnyAsync(r => r.User_Role_ID == request.RoleId);
            if (!roleExists) return BadRequest("Invalid role ID.");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Phone = request.Phone;
            user.Address = request.Address;
            user.Bio = request.Bio;
            user.User_Role_ID = request.RoleId;

            await _context.SaveChangesAsync();
            return Ok("User updated successfully.");
        }

        // PUT: api/Users/{id}/role (Admin only - change user role)
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] ChangeRoleDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            var roleExists = await _context.User_Roles.AnyAsync(r => r.User_Role_ID == request.RoleId);
            if (!roleExists) return BadRequest("Invalid role ID.");

            user.User_Role_ID = request.RoleId;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(id, "Role Changed", $"User_ID: {id}, New_Role_ID: {request.RoleId}");
            return Ok("User role updated successfully.");
        }

        // GET: api/Users/{id}/role-profile
        [HttpGet("{id}/role-profile")]
        public async Task<IActionResult> GetRoleProfile(int id)
        {
            var user = await _context.Users.Include(u => u.User_Role).FirstOrDefaultAsync(u => u.User_ID == id);
            if (user == null) return NotFound("User not found.");
            var role = user.User_Role?.User_Role_Name ?? "";

            if (role == "Student")
                return Ok(await _context.Student_Profiles.FirstOrDefaultAsync(p => p.User_ID == id));
            if (role == "Tutor" || role == "AW-Tutor")
                return Ok(await _context.Tutor_Profiles.FirstOrDefaultAsync(p => p.User_ID == id));
            if (role == "Admin")
                return Ok(await _context.Admin_Profiles.FirstOrDefaultAsync(p => p.User_ID == id));
            return Ok(null);
        }

        // PUT: api/Users/{id}/student-profile
        [HttpPut("{id}/student-profile")]
        public async Task<IActionResult> UpdateStudentProfile(int id, [FromBody] StudentProfileDto dto)
        {
            var profile = await _context.Student_Profiles.FirstOrDefaultAsync(p => p.User_ID == id);
            if (profile == null)
                _context.Student_Profiles.Add(new Student_Profile {
                    User_ID = id, Student_Number = dto.Student_Number,
                    Faculty = dto.Faculty, Year_Of_Study = dto.Year_Of_Study, Degree_Program = dto.Degree_Program
                });
            else
            {
                profile.Student_Number = dto.Student_Number;
                profile.Faculty = dto.Faculty;
                profile.Year_Of_Study = dto.Year_Of_Study;
                profile.Degree_Program = dto.Degree_Program;
            }
            await _context.SaveChangesAsync();
            return Ok("Student profile updated.");
        }

        // PUT: api/Users/{id}/tutor-profile
        [HttpPut("{id}/tutor-profile")]
        public async Task<IActionResult> UpdateTutorProfile(int id, [FromBody] TutorProfileDto dto)
        {
            var profile = await _context.Tutor_Profiles.FirstOrDefaultAsync(p => p.User_ID == id);
            if (profile == null)
                _context.Tutor_Profiles.Add(new Tutor_Profile {
                    User_ID = id, Qualifications = dto.Qualifications,
                    Specialization = dto.Specialization, Years_Of_Experience = dto.Years_Of_Experience
                });
            else
            {
                profile.Qualifications = dto.Qualifications;
                profile.Specialization = dto.Specialization;
                profile.Years_Of_Experience = dto.Years_Of_Experience;
            }
            await _context.SaveChangesAsync();
            return Ok("Tutor profile updated.");
        }

        // PUT: api/Users/{id}/admin-profile
        [HttpPut("{id}/admin-profile")]
        public async Task<IActionResult> UpdateAdminProfile(int id, [FromBody] AdminProfileDto dto)
        {
            var profile = await _context.Admin_Profiles.FirstOrDefaultAsync(p => p.User_ID == id);
            if (profile == null)
                _context.Admin_Profiles.Add(new Admin_Profile { User_ID = id, Job_Title = dto.Job_Title });
            else
                profile.Job_Title = dto.Job_Title;
            await _context.SaveChangesAsync();
            return Ok("Admin profile updated.");
        }

        // DELETE: api/Users/{id} (1.4 Delete user profile)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            // Remove all related records first to avoid FK constraint violations
            var bookings = _context.Bookings.Where(b => b.Student_ID == id);
            _context.Bookings.RemoveRange(bookings);

            var slots = _context.Booking_Slots.Where(s => s.Tutor_ID == id);
            // Delete bookings referencing those slots too
            var slotIds = slots.Select(s => s.Booking_Slot_ID).ToList();
            var slotBookings = _context.Bookings.Where(b => slotIds.Contains(b.Booking_Slot_ID));
            _context.Bookings.RemoveRange(slotBookings);
            _context.Booking_Slots.RemoveRange(slots);

            var logHours = _context.Log_Hours.Where(l => l.Tutor_ID == id);
            _context.Log_Hours.RemoveRange(logHours);

            var announcements = _context.Announcements.Where(a => a.Tutor_ID == id || a.Admin_ID == id);
            _context.Announcements.RemoveRange(announcements);

            var testimonials = _context.Testimonials.Where(t => t.Student_ID == id);
            _context.Testimonials.RemoveRange(testimonials);

            var notifications = _context.Notifications.Where(n => n.User_ID == id);
            _context.Notifications.RemoveRange(notifications);

            var studentQuizzes = _context.Student_Quizzes.Where(q => q.Student_ID == id);
            _context.Student_Quizzes.RemoveRange(studentQuizzes);

            var sessionAttendances = _context.Session_Attendances.Where(a => a.Student_ID == id);
            _context.Session_Attendances.RemoveRange(sessionAttendances);

            var groupAllocations = _context.Student_Group_Allocations.Where(g => g.Student_ID == id);
            _context.Student_Group_Allocations.RemoveRange(groupAllocations);

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await _audit.LogAsync(1, "User Deleted", $"User_ID: {id}, Email: {user.Email}");
                return Ok("User deleted successfully.");
            }
            catch (DbUpdateException)
            {
                return Conflict("Cannot delete this user because they have associated records that could not be removed automatically.");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MODULES CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class ModulesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public ModulesController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // GET: api/Modules (Everyone can see available modules)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ModuleReturnDto>>> GetModules([FromQuery] int? studentId = null)
        {
            // If studentId is provided, return only enrolled modules for that student
            if (studentId.HasValue && studentId.Value > 0)
            {
                var enrolledModules = await _context.Student_Modules
                    .Where(sm => sm.Student_ID == studentId.Value && sm.IsActive)
                    .Include(sm => sm.Module)
                    .Select(sm => sm.Module)
                    .Select(m => new ModuleReturnDto
                    {
                        Module_Code = m!.Module_Code,
                        Module_Name = m.Module_Name,
                        Module_Description = m.Module_Description,
                        Module_Price = m.Module_Price
                    })
                    .ToListAsync();

                return Ok(enrolledModules);
            }

            // Return all available modules (for browsing before enrollment)
            var modules = await _context.Modules
                .Select(m => new ModuleReturnDto
                {
                    Module_Code = m.Module_Code,
                    Module_Name = m.Module_Name,
                    Module_Description = m.Module_Description,
                    Module_Price = m.Module_Price
                })
                .ToListAsync();

            return Ok(modules);
        }

        // POST: api/Modules (4.1 Create module - Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Module>> CreateModule(ModuleCreateDto request)
        {
            if (await _context.Modules.AnyAsync(m => m.Module_Code == request.Module_Code))
            {
                return BadRequest("A module with this code already exists.");
            }

            var newModule = new Module
            {
                Module_Code = request.Module_Code,
                Module_Name = request.Module_Name,
                Module_Description = request.Module_Description,
                Module_Price = request.Module_Price
            };

            _context.Modules.Add(newModule);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(1, "Module Created", $"Module_Code: {newModule.Module_Code}, Name: {newModule.Module_Name}");

            return Ok("Module created successfully.");
        }

        // PUT: api/Modules/{code} (4.3 Update module - Admin only)
        [HttpPut("{code}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateModule(string code, ModuleCreateDto request)
        {
            var module = await _context.Modules.FindAsync(code);
            if (module == null) return NotFound("Module not found.");

            module.Module_Name = request.Module_Name;
            module.Module_Description = request.Module_Description;
            module.Module_Price = request.Module_Price;

            await _context.SaveChangesAsync();
            return Ok("Module updated successfully.");
        }

        // DELETE: api/Modules/{code} (4.4 Delete module - Admin only)
        [HttpDelete("{code}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteModule(string code)
        {
            var module = await _context.Modules.FindAsync(code);
            if (module == null) return NotFound("Module not found.");

            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(1, "Module Deleted", $"Module_Code: {code}");
            return Ok("Module deleted successfully.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MODULE RESOURCES CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class ModuleResourcesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryService _cloudinary;

        public ModuleResourcesController(AppDbContext context, CloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Module_Resource>>> GetResources()
            => Ok(await _context.Module_Resources.ToListAsync());

        // GET: api/ModuleResources/module/{moduleCode}
        [HttpGet("module/{moduleCode}")]
        public async Task<ActionResult> GetResourcesByModule(string moduleCode)
        {
            var resources = await _context.Module_Resources
                .Where(r => r.Module_Code == moduleCode)
                .OrderBy(r => r.Folder_Name)
                .ThenBy(r => r.Date_Added)
                .ToListAsync();

            return Ok(resources.Select(r => new
            {
                r.Module_Resource_ID,
                r.Module_Resource_Name,
                r.Module_Resource_Type_ID,
                r.Resource_URL,
                r.Folder_Name,
                r.Is_Visible,
                r.Folder_Is_Visible,
                r.Date_Added,
                r.Module_Code
            }));
        }

        // POST: api/ModuleResources/upload — file upload (PDF)
        [HttpPost("upload")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file provided.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".mp4", ".webm", ".mov" };
            if (!allowed.Contains(ext)) return BadRequest("File type not supported.");

            using var stream = file.OpenReadStream();
            var fileName = $"{Guid.NewGuid()}{ext}";
            try
            {
                var isVideo = new[] { ".mp4", ".webm", ".mov" }.Contains(ext);
                var url = isVideo
                    ? await _cloudinary.UploadVideoAsync(stream, fileName)
                    : await _cloudinary.UploadImageAsync(stream, fileName);
                return Ok(url);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Upload failed: {ex.Message}");
            }
        }

        // POST: api/ModuleResources
        [HttpPost]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult> CreateResource(ResourceCreateDto request)
        {
            // Inherit folder visibility from existing resources in the same folder
            var folderName = request.Folder_Name?.Trim() ?? string.Empty;
            bool folderVisible = true;
            var existingInFolder = await _context.Module_Resources
                .Where(r => r.Module_Code == request.Module_Code && r.Folder_Name == folderName)
                .FirstOrDefaultAsync();
            if (existingInFolder != null)
                folderVisible = existingInFolder.Folder_Is_Visible;

            var resource = new Module_Resource
            {
                Module_Resource_Name = request.Module_Resource_Name,
                Module_Resource_Type_ID = request.Module_Resource_Type_ID,
                Module_Code = request.Module_Code,
                Resource_URL = request.Resource_URL,
                Folder_Name = folderName,
                Is_Visible = false,
                Folder_Is_Visible = folderVisible,
                Date_Added = DateTime.UtcNow
            };
            _context.Module_Resources.Add(resource);
            await _context.SaveChangesAsync();
            return Ok("Resource added successfully.");
        }

        // PUT: api/ModuleResources/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> UpdateResource(int id, ResourceCreateDto request)
        {
            var resource = await _context.Module_Resources.FindAsync(id);
            if (resource == null) return NotFound("Resource not found.");
            resource.Module_Resource_Name = request.Module_Resource_Name;
            resource.Module_Resource_Type_ID = request.Module_Resource_Type_ID;
            resource.Resource_URL = request.Resource_URL;
            resource.Folder_Name = request.Folder_Name?.Trim() ?? string.Empty;
            await _context.SaveChangesAsync();
            return Ok("Resource updated.");
        }

        // PUT: api/ModuleResources/{id}/visibility  — toggles a single file's Is_Visible
        [HttpPut("{id}/visibility")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> ToggleVisibility(int id, [FromBody] ResourceVisibilityDto request)
        {
            var resource = await _context.Module_Resources.FindAsync(id);
            if (resource == null) return NotFound("Resource not found.");
            resource.Is_Visible = request.Is_Visible;
            await _context.SaveChangesAsync();
            return Ok("Visibility updated.");
        }

        // PUT: api/ModuleResources/folder-visibility
        // Hide folder: sets Folder_Is_Visible=false AND Is_Visible=false for every file in folder
        // Show folder: sets Folder_Is_Visible=true only — file Is_Visible states are untouched
        [HttpPut("folder-visibility")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> SetFolderVisibility([FromBody] FolderVisibilityDto request)
        {
            var resources = await _context.Module_Resources
                .Where(r => r.Module_Code == request.Module_Code && r.Folder_Name == request.Folder_Name)
                .ToListAsync();

            foreach (var r in resources)
            {
                r.Folder_Is_Visible = request.Is_Visible;
                if (!request.Is_Visible)
                    r.Is_Visible = false; // hide all files when hiding folder
            }

            await _context.SaveChangesAsync();
            return Ok("Folder visibility updated.");
        }

        // DELETE: api/ModuleResources/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> DeleteResource(int id)
        {
            var resource = await _context.Module_Resources.FindAsync(id);
            if (resource == null) return NotFound("Resource not found.");
            _context.Module_Resources.Remove(resource);
            await _context.SaveChangesAsync();
            return Ok("Resource deleted.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LOG HOURS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class LogHoursController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public LogHoursController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // GET: api/LogHours — all records (admin view)
        [HttpGet]
        public async Task<ActionResult> GetLoggedHours()
        {
            var logs = await _context.Log_Hours
                .Include(l => l.Tutor)
                .OrderByDescending(l => l.Log_Hours_Date)
                .ToListAsync();

            var result = logs.Select(l => new
            {
                l.Log_Hours_ID,
                l.Log_Hours_Date,
                l.Log_Hours_Time,
                l.Log_Hours_Amount,
                l.Tutor_ID,
                TutorName = l.Tutor != null ? $"{l.Tutor.FirstName} {l.Tutor.LastName}" : "Unknown",
                l.IsApproved,
                l.ApprovalDate,
                l.ApprovedBy_Admin_ID
            });

            return Ok(result);
        }

        // GET: api/LogHours/tutor/{tutorId} — specific tutor's hours (3.2 View tutor hours)
        [HttpGet("tutor/{tutorId}")]
        public async Task<ActionResult<IEnumerable<Log_Hours>>> GetTutorHours(int tutorId)
        {
            var hours = await _context.Log_Hours
                .Where(l => l.Tutor_ID == tutorId)
                .OrderByDescending(l => l.Log_Hours_Date)
                .ToListAsync();
            return Ok(hours);
        }

        // POST: api/LogHours — create new log entry (3.1 Add tutor hours)
        [HttpPost]
        public async Task<ActionResult<Log_Hours>> LogTime(LogHoursCreateDto request)
        {
            var newLog = new Log_Hours
            {
                Log_Hours_Date = request.Log_Hours_Date,
                Log_Hours_Time = request.Log_Hours_Time,
                Log_Hours_Amount = request.Log_Hours_Amount,
                Tutor_ID = request.Tutor_ID
            };

            _context.Log_Hours.Add(newLog);
            await _context.SaveChangesAsync();

            return Ok("Tutor hours logged successfully.");
        }

        // PUT: api/LogHours/{id} — update log entry (3.3 Update tutor hours)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLogHours(int id, LogHoursCreateDto request)
        {
            var log = await _context.Log_Hours.FindAsync(id);
            if (log == null) return NotFound("Log entry not found.");

            log.Log_Hours_Date = request.Log_Hours_Date;
            log.Log_Hours_Time = request.Log_Hours_Time;
            log.Log_Hours_Amount = request.Log_Hours_Amount;

            await _context.SaveChangesAsync();
            return Ok("Log entry updated successfully.");
        }

        // DELETE: api/LogHours/{id} — delete log entry (3.4 Delete tutor hours)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLogHours(int id)
        {
            var log = await _context.Log_Hours.FindAsync(id);
            if (log == null) return NotFound("Log entry not found.");

            _context.Log_Hours.Remove(log);
            await _context.SaveChangesAsync();
            return Ok("Log entry deleted successfully.");
        }

        // GET: api/LogHours/pending — all unapproved entries with tutor name (admin)
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetPendingHours()
        {
            var logs = await _context.Log_Hours
                .Include(l => l.Tutor)
                .Where(l => !l.IsApproved)
                .OrderBy(l => l.Log_Hours_Date)
                .ToListAsync();

            var result = logs.Select(l => new
            {
                l.Log_Hours_ID,
                l.Log_Hours_Date,
                l.Log_Hours_Time,
                l.Log_Hours_Amount,
                l.Tutor_ID,
                TutorName = l.Tutor != null ? $"{l.Tutor.FirstName} {l.Tutor.LastName}" : "Unknown"
            });

            return Ok(result);
        }

        // PUT: api/LogHours/{id}/approve — approve a log entry (admin)
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveLogHours(int id, [FromBody] int adminId)
        {
            var log = await _context.Log_Hours.FindAsync(id);
            if (log == null) return NotFound("Log entry not found.");

            log.IsApproved = true;
            log.ApprovalDate = DateTime.UtcNow;
            log.ApprovedBy_Admin_ID = adminId;

            await _context.SaveChangesAsync();
            await _audit.LogAsync(adminId, "Hours Approved", $"Log_ID: {id}, Tutor_ID: {log.Tutor_ID}, Hours: {log.Log_Hours_Amount}");
            return Ok("Log entry approved.");
        }

        // DELETE: api/LogHours/{id}/reject — reject (delete) a log entry (admin)
        [HttpDelete("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectLogHours(int id)
        {
            var log = await _context.Log_Hours.FindAsync(id);
            if (log == null) return NotFound("Log entry not found.");

            _context.Log_Hours.Remove(log);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(1, "Hours Rejected", $"Log_ID: {id}, Tutor_ID: {log.Tutor_ID}");
            return Ok("Log entry rejected and removed.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TESTIMONIALS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class TestimonialsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TestimonialsController(AppDbContext context) { _context = context; }

        // GET: Categories - open to all (needed by submit form and public viewer)
        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<ActionResult> GetCategories()
            => Ok(await _context.Testimonial_Categories.ToListAsync());

        // GET: For the public website (Only shows Approved ones)
        [HttpGet("approved")]
        [AllowAnonymous]
        public async Task<ActionResult> GetApprovedTestimonials()
        {
            return Ok(await _context.Testimonials.Where(t => t.IsApproved == true).ToListAsync());
        }

        // GET: For the Admin Dashboard (Shows ones waiting for review)
        [HttpGet("pending")]
        public async Task<ActionResult> GetPendingTestimonials()
        {
            return Ok(await _context.Testimonials.Where(t => t.IsApproved == false).ToListAsync());
        }

        // POST: A student submits a new testimonial
        [HttpPost]
        public async Task<ActionResult> CreateTestimonial(TestimonialCreateDto request)
        {
            var testimonial = new Testimonial
            {
                Testimonial_Description = request.Description,
                Student_ID = request.Student_ID,
                Testimonial_Category_ID = request.Testimonial_Category_ID,
                IsApproved = false // ALWAYS false initially. Admins must review!
            };
            _context.Testimonials.Add(testimonial);
            await _context.SaveChangesAsync();
            return Ok("Testimonial submitted for admin approval.");
        }

        // GET: All testimonials for a student
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult> GetStudentTestimonials(int studentId)
            => Ok(await _context.Testimonials.Where(t => t.Student_ID == studentId).ToListAsync());

        // PUT: An Admin clicks "Approve"
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveTestimonial(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null) return NotFound("Testimonial not found.");

            testimonial.IsApproved = true;
            await _context.SaveChangesAsync();
            return Ok("Testimonial approved for public display.");
        }

        // PUT: Student updates their testimonial (2.11 Update Testimonial)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTestimonial(int id, TestimonialUpdateDto request)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null) return NotFound("Testimonial not found.");

            testimonial.Testimonial_Description = request.Description;
            testimonial.Testimonial_Category_ID = request.Testimonial_Category_ID;
            testimonial.IsApproved = false; // Reset approval after edit
            await _context.SaveChangesAsync();
            return Ok("Testimonial updated. Awaiting admin approval.");
        }

        // DELETE: Student deletes their testimonial (2.12 Delete Testimonial)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null) return NotFound("Testimonial not found.");

            _context.Testimonials.Remove(testimonial);
            await _context.SaveChangesAsync();
            return Ok("Testimonial deleted.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ADMIN CONTENT ITERATION 4 CONTROLLER
    // FAQs (UC 4.12-4.15) and Help Pages (UC 4.24-4.27)
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/AdminContent")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class AdminContentIteration4Controller : ControllerBase
    {
        private readonly AppDbContext _context;
        public AdminContentIteration4Controller(AppDbContext context)
        {
            _context = context;
        }

        // ─── FAQs ──────────────────────────────────────────────────────────────

        [HttpGet("faqs")]
        [AllowAnonymous]
        public async Task<ActionResult> GetFAQs()
        {
            var faqs = await _context.FAQs.ToListAsync();
            return Ok(faqs.Select(f => new {
                faq_ID          = f.FAQ_ID,
                question        = f.Question,
                answer          = f.Answer,
                faq_Category_ID = f.FAQ_Category_ID
            }));
        }

        [HttpPost("faqs")]
        public async Task<ActionResult> CreateFAQ(FAQCreateDto request)
        {
            var faq = new FAQ
            {
                Question = request.Question,
                Answer = request.Answer,
                FAQ_Category_ID = request.FAQ_Category_ID
            };
            _context.FAQs.Add(faq);
            await _context.SaveChangesAsync();
            return Ok("FAQ added successfully.");
        }

        [HttpPut("faqs/{id}")]
        public async Task<IActionResult> UpdateFAQ(int id, FAQUpdateDto request)
        {
            var faq = await _context.FAQs.FindAsync(id);
            if (faq == null) return NotFound();
            faq.Question = request.Question;
            faq.Answer = request.Answer;
            faq.FAQ_Category_ID = request.FAQ_Category_ID;
            await _context.SaveChangesAsync();
            return Ok("FAQ updated.");
        }

        [HttpDelete("faqs/{id}")]
        public async Task<IActionResult> DeleteFAQ(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);
            if (faq == null) return NotFound();
            _context.FAQs.Remove(faq);
            await _context.SaveChangesAsync();
            return Ok("FAQ deleted.");
        }

        // ─── HELP PAGES ────────────────────────────────────────────────────────

        [HttpGet("help-pages")]
        [AllowAnonymous]
        public async Task<ActionResult> GetHelpPages()
            => Ok(await _context.Help_Pages.ToListAsync());

        [HttpPost("help-pages")]
        public async Task<ActionResult> CreateHelpPage(HelpPageCreateDto request)
        {
            var page = new Help_Page
            {
                Help_Page_Title = request.Help_Page_Title,
                Help_Page_Description = request.Help_Page_Description
            };
            _context.Help_Pages.Add(page);
            await _context.SaveChangesAsync();
            return Ok("Help page created.");
        }

        [HttpPut("help-pages/{id}")]
        public async Task<IActionResult> UpdateHelpPage(int id, HelpPageCreateDto request)
        {
            var page = await _context.Help_Pages.FindAsync(id);
            if (page == null) return NotFound();
            page.Help_Page_Title = request.Help_Page_Title;
            page.Help_Page_Description = request.Help_Page_Description;
            await _context.SaveChangesAsync();
            return Ok("Help page updated.");
        }

        [HttpDelete("help-pages/{id}")]
        public async Task<IActionResult> DeleteHelpPage(int id)
        {
            var page = await _context.Help_Pages.FindAsync(id);
            if (page == null) return NotFound();
            _context.Help_Pages.Remove(page);
            await _context.SaveChangesAsync();
            return Ok("Help page deleted.");
        }
    }
}
