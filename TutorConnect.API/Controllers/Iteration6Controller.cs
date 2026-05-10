using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnect.API.Data;

namespace TutorConnect.API.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // REPORTS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReportsController(AppDbContext context) { _context = context; }

        // Use Case 6.9 & 6.10: Generates the Tutor Hours data for Chart.js
        [HttpGet("tutor-hours")]
        public async Task<ActionResult> GetTutorHoursReport()
        {
            var report = await _context.Log_Hours
                .GroupBy(l => l.Tutor_ID)
                .Select(g => new {
                    TutorId = g.Key,
                    TotalHoursWorked = g.Sum(h => h.Log_Hours_Amount)
                }).ToListAsync();

            return Ok(report);
        }

        // Use Case 6.5 & 6.6: Generates Monthly Income data for Chart.js
        [HttpGet("monthly-income")]
        public async Task<ActionResult> GetMonthlyIncome()
        {
            var report = await _context.Payments
                .Where(p => p.Payment_Status == "Paid")
                .GroupBy(p => p.Payment_Date.Month)
                .Select(g => new {
                    Month = g.Key,
                    TotalIncome = g.Sum(p => p.Amount)
                }).ToListAsync();

            return Ok(report);
        }

        // Use Case 6.1 & 6.2: Tutor Rating Report
        [HttpGet("tutor-ratings")]
        public async Task<ActionResult> GetTutorRatingsReport()
        {
            var report = await _context.Tutor_Reviews
                .GroupBy(r => r.Tutor_ID)
                .Select(g => new {
                    TutorId = g.Key,
                    AverageRating = g.Average(r => r.Tutor_Rating),
                    TotalReviews = g.Count()
                }).ToListAsync();

            return Ok(report);
        }

        // Use Case 6.3 & 6.4: Monthly Students Report
        [HttpGet("monthly-students")]
        public async Task<ActionResult> GetMonthlyStudentsReport()
        {
            var report = await _context.Bookings
                .GroupBy(b => b.Booking_Slot!.Slot_Date.Month)
                .Select(g => new {
                    Month = g.Key,
                    UniqueStudents = g.Select(b => b.Student_ID).Distinct().Count()
                }).ToListAsync();

            return Ok(report);
        }

        // Use Case 6.7 & 6.8: Session Report
        [HttpGet("sessions")]
        public async Task<ActionResult> GetSessionsReport()
        {
            var report = await _context.Bookings
                .Include(b => b.Booking_Slot)
                .Select(b => new {
                    BookingId = b.Booking_ID,
                    StudentId = b.Student_ID,
                    SlotDate = b.Booking_Slot != null ? b.Booking_Slot.Slot_Date.ToString() : "",
                    SlotTime = b.Booking_Slot != null ? b.Booking_Slot.Slot_Time.ToString() : "",
                    SessionType = b.Booking_Slot != null ? b.Booking_Slot.Session_Type : ""
                }).ToListAsync();

            return Ok(report);
        }

        // Use Case 6.11 & 6.12: Popular Module Report
        [HttpGet("popular-modules")]
        public async Task<ActionResult> GetPopularModulesReport()
        {
            var report = await _context.Announcements
                .GroupBy(a => a.Module_Code)
                .Select(g => new {
                    ModuleCode = g.Key,
                    AnnouncementCount = g.Count()
                }).ToListAsync();

            return Ok(report);
        }
    }
}
