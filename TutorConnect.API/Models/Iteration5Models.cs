// Iteration 5: UC 2.x (Enrollment), UC 3.9-3.40 (Assignments/Quizzes/Reviews/Attendance/Announcements), UC 4.10-4.11 (Tutor Modules), UC 4.16-4.35 (FAQ Cat/Media/Help Resources/Testimonial Cat), UC 5.x (Bookings/Payments)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorConnect.API.Models
{
    public class Assignment
    {
        [Key]
        public int Assignment_ID { get; set; }
        public string Assignment_Name { get; set; } = string.Empty;
        public DateTime Assignment_Date { get; set; }
        public string Assignment_URL { get; set; } = string.Empty;  // tutor brief PDF
        public bool Is_Visible { get; set; } = false;

        public string Module_Code { get; set; } = string.Empty;
        public Module? Module { get; set; }
    }

    public class Assignment_Submission
    {
        [Key]
        public int Submission_ID { get; set; }

        // Assignment reference
        [ForeignKey("Assignment")]
        public int Assignment_ID { get; set; }
        public Assignment? Assignment { get; set; }

        // Student reference
        [ForeignKey("Student")]
        public int Student_ID { get; set; }
        public User? Student { get; set; }

        // Submission details
        public DateTime Submission_Date { get; set; } = DateTime.UtcNow;

        // File upload fields
        public string File_Name { get; set; } = string.Empty;
        public string File_Path { get; set; } = string.Empty;
        public string File_Type { get; set; } = string.Empty; // e.g., pdf, docx, txt
        public long File_Size { get; set; } // in bytes

        // Grading fields
        public decimal? Grade { get; set; }
        public string? Feedback { get; set; }
        public DateTime? Feedback_Date { get; set; }
        public int? Graded_By { get; set; } // Tutor ID who graded

        // For backward compatibility
        [NotMapped]
        public decimal Assignment_grade_amount
        {
            get => Grade ?? 0;
            set => Grade = value;
        }

        [NotMapped]
        public string Assignment_Feedback
        {
            get => Feedback ?? string.Empty;
            set => Feedback = value;
        }
    }

    public class Quiz
    {
        [Key]
        public int Quiz_ID { get; set; }
        public string Quiz_Name { get; set; } = string.Empty;
        public string Quiz_Details { get; set; } = string.Empty;
        public DateTime Quiz_Date { get; set; }
        public bool Is_Visible { get; set; } = false;
        public string Module_Code { get; set; } = string.Empty;
        public Module? Module { get; set; }
    }

    public class Quiz_Question
    {
        [Key]
        public int Question_ID { get; set; }
        public int Quiz_ID { get; set; }
        public string Question_Text { get; set; } = string.Empty;
        public int Question_Order { get; set; } = 0;
    }

    public class Quiz_Question_Option
    {
        [Key]
        public int Option_ID { get; set; }
        public int Question_ID { get; set; }
        public string Option_Text { get; set; } = string.Empty;
        public bool Is_Correct { get; set; } = false;
    }

    public class Student_Quiz_Answer
    {
        [Key]
        public int Answer_ID { get; set; }
        public int Student_Quiz_ID { get; set; }
        public int Question_ID { get; set; }
        public int Option_ID { get; set; }
    }

    // 1. Quizzes
    public class Student_Quiz
    {
        [Key]
        public int Student_Quiz_ID { get; set; }
        public decimal Quiz_Score { get; set; }
        public int Student_ID { get; set; }
        public int Quiz_ID { get; set; }
        public Quiz? Quiz { get; set; }
        public DateTime Submission_Date { get; set; } = DateTime.UtcNow;
        public DateTime? Start_Time { get; set; }
        public DateTime? End_Time { get; set; }
    }

    public class Announcement
    {
        [Key]
        public int Announcement_ID { get; set; }
        public string Announcement_Name { get; set; } = string.Empty;
        public string Announcement_Details { get; set; } = string.Empty;
        public string Announcement_Type { get; set; } = "Update"; // Update, Deadline, Event, Resource
        public DateTime Date_Posted { get; set; } = DateTime.UtcNow;

        // Either a Tutor or an Admin can make an announcement
        public int? Tutor_ID { get; set; }
        public int? Admin_ID { get; set; }

        public string Module_Code { get; set; } = string.Empty;
    }

    public class Tutor_Review
    {
        [Key]
        public int Tutor_Review_ID { get; set; }
        public int Tutor_Rating { get; set; } // e.g., 1 to 5
        public int Student_ID { get; set; }
        public int Tutor_ID { get; set; }
    }

    public class Session_Review
    {
        [Key]
        public int Session_Review_ID { get; set; }
        public int Session_Rating { get; set; }
        public string Session_Description { get; set; } = string.Empty;
        public int Student_ID { get; set; }
        public int Session_ID { get; set; }
    }

    // 3. Attendance
    public class Attendance_Status
    {
        [Key]
        public int Attendance_Status_ID { get; set; }
        public string Status_Name { get; set; } = string.Empty;
    }

    public class Session_Attendance
    {
        [Key]
        public int Session_Attendance_ID { get; set; }

        // --- THIS IS THE LINE THAT FIXES YOUR ERROR! ---
        public string Session_Attendance_Name { get; set; } = string.Empty;

        public int Session_ID { get; set; }
        public int Student_ID { get; set; }
        public int Attendance_Status_ID { get; set; }
        public Attendance_Status? Attendance_Status { get; set; }
    }

    // 2. Recorded Sessions
    public class Recorded_Session
    {
        [Key]
        public int Recording_ID { get; set; }
        public DateTime Recording_Date { get; set; }
        public string Recording_Name { get; set; } = string.Empty;
        public string Recording_Filesize { get; set; } = string.Empty;
        public string Recording_Filetype { get; set; } = string.Empty;
        public string Module_Code { get; set; } = string.Empty;
        public int Session_ID { get; set; }
    }

    // 4. Booking Slots
    public class Booking_Slot
    {
        [Key]
        public int Booking_Slot_ID { get; set; }
        public DateOnly Slot_Date { get; set; }
        public TimeOnly Slot_Time { get; set; }
        public string Session_Type { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool Is_Booked { get; set; } = false;
        public int Tutor_ID { get; set; }
        public User? Tutor { get; set; }
        public int? Max_Capacity { get; set; }
        public string? Module_Code { get; set; }
    }

    public class Booking
    {
        [Key]
        public int Booking_ID { get; set; }

        // Links the Student making the booking
        public int Student_ID { get; set; }
        public User? Student { get; set; }

        // Links to the available time slot
        public int Booking_Slot_ID { get; set; }
        public Booking_Slot? Booking_Slot { get; set; }
    }

    public class Payment
    {
        [Key]
        public int Payment_ID { get; set; }
        public decimal Amount { get; set; }
        public DateTime Payment_Date { get; set; }
        public string Payment_Status { get; set; } = "Pending"; // "Paid", "Pending", "Failed"

        // Iteration 2 EFT/Ozow Fields
        public string? Account_Name { get; set; }
        public string? Account_Number { get; set; }
        public string? Branch_Code { get; set; }
        public string? Bank { get; set; }
        public string? Payment_Reference { get; set; }

        // Foreign Keys
        public int Student_ID { get; set; }
        public User? Student { get; set; }

        public string Module_Code { get; set; } = string.Empty;
        public Module? Module { get; set; }
    }

    public class Student_Module
    {
        [Key]
        public int Enrollment_ID { get; set; }

        [ForeignKey("Student")]
        public int Student_ID { get; set; }
        public User? Student { get; set; }

        [ForeignKey("Module")]
        public string Module_Code { get; set; } = string.Empty;
        public Module? Module { get; set; }

        public DateTime Enrollment_Date { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class Student_Unenrollment
    {
        [Key]
        public int Unenrollment_ID { get; set; }

        public int Student_ID { get; set; }
        public User? Student { get; set; }

        [ForeignKey("Module")]
        public string Module_Code { get; set; } = string.Empty;
        public Module? Module { get; set; }

        public int Enrollment_ID { get; set; }         // links back to original enrollment
        public DateTime Unenroll_Date { get; set; } = DateTime.UtcNow;
        public string? Unenroll_Reason { get; set; }
    }

    public class Tutor_Module
    {
        [Key]
        public int Tutor_Module_ID { get; set; }

        [ForeignKey("Tutor")]
        public int Tutor_ID { get; set; }
        public User? Tutor { get; set; }

        [ForeignKey("Module")]
        public string Module_Code { get; set; } = string.Empty;
        public Module? Module { get; set; }

        public DateTime Assigned_Date { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class Help_Resource
    {
        [Key]
        public int Help_Video_ID { get; set; }
        public string Video_Title { get; set; } = string.Empty;
        public string Video_URL { get; set; } = string.Empty;
    }

    public class FAQ_Category
    {
        [Key]
        public int FAQ_Category_ID { get; set; }
        public string Category_Name { get; set; } = string.Empty;
    }

    public class Testimonial_Category
    {
        [Key]
        public int Testimonial_Category_ID { get; set; }
        public string Test_Category_Name { get; set; } = string.Empty;
    }

    public class Media_Content
    {
        [Key]
        public int Media_ID { get; set; }
        public string Media_Name { get; set; } = string.Empty;
        public string Media_Address { get; set; } = string.Empty; // The URL/Path
    }

    // 5 & 6. Student Groups (With the required Many-to-Many Bridging Table)
    public class Student_Group
    {
        [Key]
        public int Student_Group_ID { get; set; }
        public string Group_Name { get; set; } = string.Empty;
    }

    public class Student_Group_Allocation
    {
        [Key]
        public int Allocation_ID { get; set; }
        public int Student_ID { get; set; }
        public int Student_Group_ID { get; set; }
        public Student_Group? Student_Group { get; set; }
    }
}
