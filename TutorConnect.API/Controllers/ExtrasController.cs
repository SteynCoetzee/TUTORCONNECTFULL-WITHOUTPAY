using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnect.API.Data;
using TutorConnect.API.DTOs;
using TutorConnect.API.Models;
using TutorConnect.API.Services;

namespace TutorConnect.API.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // NOTIFICATIONS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Notifications/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<NotificationReturnDto>>> GetUserNotifications(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.User_ID == userId)
                .OrderByDescending(n => n.Date_Sent) // Newest first!
                .Select(n => new NotificationReturnDto
                {
                    Notification_ID = n.Notification_ID,
                    Message = n.Message,
                    Date_Sent = n.Date_Sent,
                    Is_Read = n.Is_Read
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // PUT: api/Notifications/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound("Notification not found.");
            notification.Is_Read = true;
            await _context.SaveChangesAsync();
            return Ok("Notification marked as read.");
        }

        // PUT: api/Notifications/user/{userId}/read-all
        [HttpPut("user/{userId}/read-all")]
        public async Task<IActionResult> MarkAllRead(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.User_ID == userId && !n.Is_Read)
                .ToListAsync();
            foreach (var n in unread) n.Is_Read = true;
            await _context.SaveChangesAsync();
            return Ok("All notifications marked as read.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AUDIT LOG CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AuditLogsController(AppDbContext context) { _context = context; }

        // GET: api/AuditLogs
        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int? userId = null, [FromQuery] string? type = null)
        {
            var query = _context.Audit_Logs.AsQueryable();
            if (userId.HasValue)  query = query.Where(a => a.User_ID == userId.Value);
            if (!string.IsNullOrEmpty(type)) query = query.Where(a => a.Transaction_Type.Contains(type));

            var logs = await query
                .OrderByDescending(a => a.Audit_Date)
                .ThenByDescending(a => a.Audit_Time)
                .Take(200)
                .ToListAsync();

            var userIds = logs.Select(l => l.User_ID).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => $"{u.FirstName} {u.LastName}");

            return Ok(logs.Select(l => new
            {
                l.Audit_Log_ID,
                l.Audit_Date,
                l.Audit_Time,
                l.User_ID,
                UserName         = users.TryGetValue(l.User_ID, out var name) ? name : $"User #{l.User_ID}",
                l.Transaction_Type,
                l.Critical_Data
            }));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MODULE WISHLIST CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class ModuleWishlistController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ModuleWishlistController(AppDbContext context) { _context = context; }

        // GET: api/ModuleWishlist — admin sees all items with student names
        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var items = await _context.Module_Wishlists
                .OrderByDescending(w => w.Date_Submitted)
                .ToListAsync();

            var studentIds = items.Select(w => w.Student_ID).Distinct().ToList();
            var students = await _context.Users
                .Where(u => studentIds.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => $"{u.FirstName} {u.LastName}");

            return Ok(items.Select(w => new
            {
                w.Wishlist_ID,
                w.Module_Code,
                w.Module_Name,
                w.Student_ID,
                w.Date_Submitted,
                StudentName = students.TryGetValue(w.Student_ID, out var name) ? name : $"Student #{w.Student_ID}"
            }));
        }

        // GET: api/ModuleWishlist/student/{studentId} — student sees their own items
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult> GetByStudent(int studentId)
        {
            var items = await _context.Module_Wishlists
                .Where(w => w.Student_ID == studentId)
                .OrderByDescending(w => w.Date_Submitted)
                .ToListAsync();

            return Ok(items);
        }

        // POST: api/ModuleWishlist
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Module_Wishlist request)
        {
            if (string.IsNullOrWhiteSpace(request.Module_Code) || string.IsNullOrWhiteSpace(request.Module_Name))
                return BadRequest("Module code and name are required.");

            var item = new Module_Wishlist
            {
                Module_Code   = request.Module_Code.Trim().ToUpper(),
                Module_Name   = request.Module_Name.Trim(),
                Student_ID    = request.Student_ID,
                Date_Submitted = DateTime.UtcNow
            };

            _context.Module_Wishlists.Add(item);
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        // DELETE: api/ModuleWishlist/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Module_Wishlists.FindAsync(id);
            if (item == null) return NotFound("Wishlist item not found.");
            _context.Module_Wishlists.Remove(item);
            await _context.SaveChangesAsync();
            return Ok("Wishlist item removed.");
        }
    }
}
