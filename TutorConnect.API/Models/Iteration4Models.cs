// Iteration 4: UC 1.x (Users), UC 3.1-3.8 (Tutor Hours + Resources), UC 4.1-4.15 (Modules + FAQs), UC 4.24-4.27 (Help Pages), UC 2.9-2.12 (Testimonials)

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TutorConnect.API.Models
{
    public class User_Role
    {
        [Key]
        public int User_Role_ID { get; set; }
        public string User_Role_Name { get; set; } = string.Empty;

        public ICollection<User> Users { get; set; } = new List<User>();
    }

    public class User
    {
        [Key]
        public int User_ID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public int User_Role_ID { get; set; }
        public User_Role? User_Role { get; set; }
        public string? PasswordResetCode { get; set; }
        public DateTime? PasswordResetCodeExpiration { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Bio { get; set; }
    }

    public class Module
    {
        [Key]
        public string Module_Code { get; set; } = string.Empty; // e.g., "INF370"
        public string Module_Name { get; set; } = string.Empty;
        public string Module_Description { get; set; } = string.Empty;
        public decimal Module_Price { get; set; }
    }

    public class Module_Resource
    {
        [Key]
        public int Module_Resource_ID { get; set; }
        public string Module_Resource_Name { get; set; } = string.Empty;   // Title
        public string Module_Resource_Type_ID { get; set; } = string.Empty; // PDF | Link | Video
        public string Resource_URL { get; set; } = string.Empty;
        public string Folder_Name { get; set; } = string.Empty;
        public bool Is_Visible { get; set; } = false;        // file-level visibility
        public bool Folder_Is_Visible { get; set; } = true;  // folder-level visibility
        public DateTime Date_Added { get; set; } = DateTime.UtcNow;

        public string Module_Code { get; set; } = string.Empty;
        public Module? Module { get; set; }
    }

    public class Student_Profile
    {
        [Key]
        public int Student_ID { get; set; }
        public int User_ID { get; set; }
        public User? User { get; set; }
        public string? Student_Number { get; set; }
        public string? Faculty { get; set; }
        public int? Year_Of_Study { get; set; }
        public string? Degree_Program { get; set; }
    }

    public class Tutor_Profile
    {
        [Key]
        public int Tutor_ID { get; set; }
        public int User_ID { get; set; }
        public User? User { get; set; }
        public string? Qualifications { get; set; }
        public string? Specialization { get; set; }
        public int? Years_Of_Experience { get; set; }
    }

    public class Admin_Profile
    {
        [Key]
        public int Admin_ID { get; set; }
        public int User_ID { get; set; }
        public User? User { get; set; }
        public string? Job_Title { get; set; }
    }

    public class Log_Hours
    {
        [Key]
        public int Log_Hours_ID { get; set; }

        public DateOnly Log_Hours_Date { get; set; }
        public TimeOnly Log_Hours_Time { get; set; }

        public decimal Log_Hours_Amount { get; set; }

        public int Tutor_ID { get; set; }
        public User? Tutor { get; set; }
        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovalDate { get; set; }

        public int? ApprovedBy_Admin_ID { get; set; }
    }

    public class Help_Page
    {
        [Key]
        public int Help_Page_ID { get; set; }
        public string Help_Page_Title { get; set; } = string.Empty;
        public string Help_Page_Description { get; set; } = string.Empty;
    }

    public class FAQ
    {
        [Key]
        public int FAQ_ID { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int FAQ_Category_ID { get; set; }
    }

    public class Testimonial
    {
        [Key]
        public int Testimonial_ID { get; set; }
        public string Testimonial_Description { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = false; // Admins must approve these!

        public int Student_ID { get; set; }
        public int Testimonial_Category_ID { get; set; }
    }
}
