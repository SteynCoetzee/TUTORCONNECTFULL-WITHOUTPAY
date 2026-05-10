using Microsoft.EntityFrameworkCore;
using TutorConnect.API.Models;

namespace TutorConnect.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.User_Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.User_Role_ID);

            modelBuilder.Entity<Log_Hours>()
                .HasOne(l => l.Tutor)
                .WithMany()
                .HasForeignKey(l => l.Tutor_ID);

            modelBuilder.Entity<Booking_Slot>()
                .HasOne(s => s.Tutor)
                .WithMany()
                .HasForeignKey(s => s.Tutor_ID);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Booking_Slot)
                .WithMany()
                .HasForeignKey(b => b.Booking_Slot_ID);
        }

        public DbSet<User_Role> User_Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Module> Modules { get; set; } = null!;
        public DbSet<Student_Module> Student_Modules { get; set; } = null!;
        public DbSet<Tutor_Module> Tutor_Modules { get; set; } = null!;
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Assignment_Submission> Assignment_Submissions { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Module_Resource> Module_Resources { get; set; }
        public DbSet<Log_Hours> Log_Hours { get; set; }

        public DbSet<Tutor_Review> Tutor_Reviews { get; set; }
        public DbSet<Session_Review> Session_Reviews { get; set; }
        public DbSet<Testimonial_Category> Testimonial_Categories { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }

        public DbSet<FAQ_Category> FAQ_Categories { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<Media_Content> Media_Contents { get; set; }
        public DbSet<Help_Page> Help_Pages { get; set; }
        public DbSet<Help_Resource> Help_Resources { get; set; }

        public DbSet<Audit_Log> Audit_Logs { get; set; }

        public DbSet<Student_Quiz> Student_Quizzes { get; set; }
        public DbSet<Recorded_Session> Recorded_Sessions { get; set; }
        public DbSet<Attendance_Status> Attendance_Statuses { get; set; }
        public DbSet<Session_Attendance> Session_Attendances { get; set; }
        public DbSet<Booking_Slot> Booking_Slots { get; set; }
        public DbSet<Student_Group> Student_Groups { get; set; }
        public DbSet<Student_Group_Allocation> Student_Group_Allocations { get; set; }
        public DbSet<Resource_Type> Resource_Types { get; set; }
        public DbSet<Business_Rule> Business_Rules { get; set; }

        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Module_Wishlist> Module_Wishlists { get; set; }
        public DbSet<Student_Unenrollment> Student_Unenrollments { get; set; }
        public DbSet<Student_Profile> Student_Profiles { get; set; }
        public DbSet<Tutor_Profile> Tutor_Profiles { get; set; }
        public DbSet<Admin_Profile> Admin_Profiles { get; set; }
        public DbSet<Quiz_Question> Quiz_Questions { get; set; }
        public DbSet<Quiz_Question_Option> Quiz_Question_Options { get; set; }
        public DbSet<Student_Quiz_Answer> Student_Quiz_Answers { get; set; }
    }
}