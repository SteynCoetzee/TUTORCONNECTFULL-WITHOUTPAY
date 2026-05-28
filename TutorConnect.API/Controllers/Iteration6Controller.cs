using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnect.API.Data;

namespace TutorConnect.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReportsController(AppDbContext context) { _context = context; }

        private static string MonthName(int m) =>
            new[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" }[Math.Clamp(m,1,12)-1];

        // ── Tutor Hours (6.9 & 6.10) ─────────────────────────────────────────
        [HttpGet("tutor-hours")]
        public async Task<ActionResult> GetTutorHoursReport(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.Log_Hours.AsQueryable();
            if (from.HasValue) query = query.Where(l => l.Log_Hours_Date >= DateOnly.FromDateTime(from.Value));
            if (to.HasValue)   query = query.Where(l => l.Log_Hours_Date <= DateOnly.FromDateTime(to.Value));

            var grouped = await query
                .GroupBy(l => l.Tutor_ID)
                .Select(g => new { TutorId = g.Key, TotalHoursWorked = g.Sum(h => h.Log_Hours_Amount) })
                .ToListAsync();

            var ids = grouped.Select(g => g.TutorId).ToList();
            var names = await _context.Users
                .Where(u => ids.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => u.FirstName + " " + u.LastName);

            return Ok(grouped.Select(g => new {
                TutorName = names.GetValueOrDefault(g.TutorId, $"Tutor {g.TutorId}"),
                TotalHoursWorked = g.TotalHoursWorked
            }).OrderByDescending(x => x.TotalHoursWorked));
        }

        // ── Monthly Income (6.5 & 6.6) ───────────────────────────────────────
        [HttpGet("monthly-income")]
        public async Task<ActionResult> GetMonthlyIncome(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.Payments.Where(p => p.Payment_Status == "Paid");
            if (from.HasValue) query = query.Where(p => p.Payment_Date >= from.Value);
            if (to.HasValue)   query = query.Where(p => p.Payment_Date <= to.Value);

            var report = await query
                .GroupBy(p => new { p.Payment_Date.Year, p.Payment_Date.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, TotalIncome = g.Sum(p => p.Amount) })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToListAsync();

            return Ok(report.Select(r => new {
                Period = $"{MonthName(r.Month)} {r.Year}",
                r.TotalIncome
            }));
        }

        // ── Tutor Ratings (6.1 & 6.2) ────────────────────────────────────────
        [HttpGet("tutor-ratings")]
        public async Task<ActionResult> GetTutorRatingsReport()
        {
            var grouped = await _context.Tutor_Reviews
                .GroupBy(r => r.Tutor_ID)
                .Select(g => new {
                    TutorId = g.Key,
                    AverageRating = Math.Round(g.Average(r => (double)r.Tutor_Rating), 1),
                    TotalReviews = g.Count()
                }).ToListAsync();

            var ids = grouped.Select(g => g.TutorId).ToList();
            var names = await _context.Users
                .Where(u => ids.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => u.FirstName + " " + u.LastName);

            return Ok(grouped.Select(g => new {
                TutorName = names.GetValueOrDefault(g.TutorId, $"Tutor {g.TutorId}"),
                g.AverageRating,
                g.TotalReviews
            }).OrderByDescending(x => x.AverageRating));
        }

        // ── Monthly Students (6.3 & 6.4) ─────────────────────────────────────
        [HttpGet("monthly-students")]
        public async Task<ActionResult> GetMonthlyStudentsReport(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.Bookings.Include(b => b.Booking_Slot).AsQueryable();
            if (from.HasValue) query = query.Where(b => b.Booking_Slot!.Slot_Date >= DateOnly.FromDateTime(from.Value));
            if (to.HasValue)   query = query.Where(b => b.Booking_Slot!.Slot_Date <= DateOnly.FromDateTime(to.Value));

            var report = await query
                .GroupBy(b => new { b.Booking_Slot!.Slot_Date.Year, b.Booking_Slot.Slot_Date.Month })
                .Select(g => new {
                    g.Key.Year, g.Key.Month,
                    UniqueStudents = g.Select(b => b.Student_ID).Distinct().Count()
                })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToListAsync();

            return Ok(report.Select(r => new {
                Period = $"{MonthName(r.Month)} {r.Year}",
                r.UniqueStudents
            }));
        }

        // ── Sessions (6.7 & 6.8) ─────────────────────────────────────────────
        [HttpGet("sessions")]
        public async Task<ActionResult> GetSessionsReport(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.Bookings.Include(b => b.Booking_Slot).AsQueryable();
            if (from.HasValue) query = query.Where(b => b.Booking_Slot!.Slot_Date >= DateOnly.FromDateTime(from.Value));
            if (to.HasValue)   query = query.Where(b => b.Booking_Slot!.Slot_Date <= DateOnly.FromDateTime(to.Value));

            var raw = await query.ToListAsync();  // load into memory for TimeOnly formatting

            return Ok(raw.Select(b => new {
                BookingId   = b.Booking_ID,
                StudentId   = b.Student_ID,
                SlotDate    = b.Booking_Slot?.Slot_Date.ToString("yyyy-MM-dd") ?? "",
                SlotTime    = b.Booking_Slot?.Slot_Time.ToString("HH:mm") ?? "",
                SessionType = b.Booking_Slot?.Session_Type ?? "",
                Location    = b.Booking_Slot?.Location ?? ""
            }));
        }

        // ── Popular Modules (6.11 & 6.12) ────────────────────────────────────
        [HttpGet("popular-modules")]
        public async Task<ActionResult> GetPopularModulesReport(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.Announcements.AsQueryable();
            if (from.HasValue) query = query.Where(a => a.Date_Posted >= from.Value);
            if (to.HasValue)   query = query.Where(a => a.Date_Posted <= to.Value);

            var grouped = await query
                .GroupBy(a => a.Module_Code)
                .Select(g => new { ModuleCode = g.Key, AnnouncementCount = g.Count() })
                .OrderByDescending(r => r.AnnouncementCount)
                .ToListAsync();

            var codes = grouped.Select(g => g.ModuleCode).ToList();
            var modules = await _context.Modules
                .Where(m => codes.Contains(m.Module_Code))
                .ToDictionaryAsync(m => m.Module_Code, m => m.Module_Name);

            return Ok(grouped.Select(g => new {
                ModuleCode = g.ModuleCode,
                ModuleName = modules.GetValueOrDefault(g.ModuleCode, g.ModuleCode),
                g.AnnouncementCount
            }));
        }

        // ── Custom Query ──────────────────────────────────────────────────────
        [HttpGet("custom")]
        public async Task<ActionResult> GetCustomReport(
            [FromQuery] string entity  = "bookings",
            [FromQuery] string groupBy = "month",
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to   = null)
        {
            switch (entity.ToLower())
            {
                case "users":
                {
                    var data = await _context.Users
                        .Include(u => u.User_Role)
                        .GroupBy(u => u.User_Role!.User_Role_Name)
                        .Select(g => new { Category = g.Key ?? "Unknown", Count = g.Count() })
                        .ToListAsync();
                    return Ok(data);
                }
                case "bookings":
                {
                    var q = _context.Bookings.Include(b => b.Booking_Slot).AsQueryable();
                    if (from.HasValue) q = q.Where(b => b.Booking_Slot!.Slot_Date >= DateOnly.FromDateTime(from.Value));
                    if (to.HasValue)   q = q.Where(b => b.Booking_Slot!.Slot_Date <= DateOnly.FromDateTime(to.Value));
                    if (groupBy == "type")
                    {
                        var data = await q.GroupBy(b => b.Booking_Slot!.Session_Type)
                            .Select(g => new { Category = g.Key ?? "Unknown", Count = g.Count() })
                            .ToListAsync();
                        return Ok(data);
                    }
                    var byMonth = await q
                        .GroupBy(b => new { b.Booking_Slot!.Slot_Date.Year, b.Booking_Slot.Slot_Date.Month })
                        .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                        .OrderBy(r => r.Year).ThenBy(r => r.Month).ToListAsync();
                    return Ok(byMonth.Select(r => new { Category = $"{MonthName(r.Month)} {r.Year}", r.Count }));
                }
                case "enrollments":
                {
                    var q = _context.Student_Modules.AsQueryable();
                    if (from.HasValue) q = q.Where(e => e.Enrollment_Date >= from.Value);
                    if (to.HasValue)   q = q.Where(e => e.Enrollment_Date <= to.Value);
                    if (groupBy == "module")
                    {
                        var data = await q.GroupBy(e => e.Module_Code)
                            .Select(g => new { Category = g.Key, Count = g.Count() })
                            .OrderByDescending(x => x.Count).ToListAsync();
                        return Ok(data);
                    }
                    var byMonth = await q
                        .GroupBy(e => new { e.Enrollment_Date.Year, e.Enrollment_Date.Month })
                        .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                        .OrderBy(r => r.Year).ThenBy(r => r.Month).ToListAsync();
                    return Ok(byMonth.Select(r => new { Category = $"{MonthName(r.Month)} {r.Year}", r.Count }));
                }
                case "payments":
                {
                    var q = _context.Payments.AsQueryable();
                    if (from.HasValue) q = q.Where(p => p.Payment_Date >= from.Value);
                    if (to.HasValue)   q = q.Where(p => p.Payment_Date <= to.Value);
                    if (groupBy == "status")
                    {
                        var data = await q.GroupBy(p => p.Payment_Status)
                            .Select(g => new { Category = g.Key, Count = g.Count(), Total = g.Sum(p => p.Amount) })
                            .ToListAsync();
                        return Ok(data);
                    }
                    var byMonth = await q
                        .GroupBy(p => new { p.Payment_Date.Year, p.Payment_Date.Month })
                        .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Total = g.Sum(p => p.Amount) })
                        .OrderBy(r => r.Year).ThenBy(r => r.Month).ToListAsync();
                    return Ok(byMonth.Select(r => new { Category = $"{MonthName(r.Month)} {r.Year}", r.Count, r.Total }));
                }
                default:
                    return BadRequest("Unknown entity. Use: users, bookings, enrollments, payments");
            }
        }
    }
}
