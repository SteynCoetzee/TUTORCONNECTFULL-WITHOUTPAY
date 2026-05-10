namespace TutorConnect.API.DTOs
{
    // ─── ENROLLMENT ─────────────────────────────────────────────
    public class EnrollmentCreateDto
    {
        public int Student_ID { get; set; }
        public string Module_Code { get; set; } = string.Empty;
    }

    public class EnrollmentUnenrollDto
    {
        public string? Unenroll_Reason { get; set; }
    }

    public class EnrollmentViewDto
    {
        public int Enrollment_ID { get; set; }
        public int Student_ID { get; set; }
        public string Module_Code { get; set; } = string.Empty;
        public string Module_Name { get; set; } = string.Empty;
        public DateTime Enrollment_Date { get; set; }
        public bool IsActive { get; set; }
    }

    // ─── GRADES ─────────────────────────────────────────────────
    public class GradeViewDto
    {
        public int Grade_ID { get; set; }
        public string Assessment_Name { get; set; } = string.Empty;
        public string Assessment_Type { get; set; } = string.Empty; // Quiz or Assignment
        public decimal Score { get; set; }
        public decimal Total_Points { get; set; }
        public decimal Percentage { get; set; }
        public string? Feedback { get; set; }
        public DateTime Grade_Date { get; set; }
    }

    // ─── QUIZ SUBMISSION ─────────────────────────────────────────
    public class QuizSubmissionDto
    {
        public int Student_ID { get; set; }
        public List<QuizAnswerDto> Answers { get; set; } = new();
    }

    public class QuizAnswerDto
    {
        public int Question_ID { get; set; }
        public string Selected_Answer { get; set; } = string.Empty;
    }

    // ─── ASSIGNMENT SUBMISSION ───────────────────────────────────
    public class AssignmentSubmissionViewDto
    {
        public int Submission_ID { get; set; }
        public int Assignment_ID { get; set; }
        public int Student_ID { get; set; }
        public string File_Name { get; set; } = string.Empty;
        public DateTime Submission_Date { get; set; }
        public decimal? Grade { get; set; }
        public string? Feedback { get; set; }
    }

    // ─── ANNOUNCEMENTS ───────────────────────────────────────────
    public class AnnouncementCreateDto
    {
        public string Announcement_Name { get; set; } = string.Empty;
        public string Announcement_Details { get; set; } = string.Empty;
        public string Announcement_Type { get; set; } = "Update";
        public int? Tutor_ID { get; set; }
        public int? Admin_ID { get; set; }
        public string Module_Code { get; set; } = string.Empty;
    }

    public class AnnouncementUpdateDto
    {
        public string Announcement_Name { get; set; } = string.Empty;
        public string Announcement_Details { get; set; } = string.Empty;
        public string Announcement_Type { get; set; } = "Update";
        public string Module_Code { get; set; } = string.Empty;
    }

    public class BookingSlotCreateDto
    {
        public DateOnly Slot_Date { get; set; }
        public TimeOnly Slot_Time { get; set; }
        public string Session_Type { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Tutor_ID { get; set; }
        public int? Max_Capacity { get; set; }
        public string? Module_Code { get; set; }
    }

    public class BookingCreateDto
    {
        public int Student_ID { get; set; }
        public int Booking_Slot_ID { get; set; }
    }

    public class GradeSubmissionDto
    {
        public decimal? Grade { get; set; }
        public string? Feedback { get; set; }
    }

    public class AssignmentCreateDto
    {
        public string Assignment_Name { get; set; } = string.Empty;
        public DateTime Assignment_Date { get; set; }
        public string Module_Code { get; set; } = string.Empty;
        public string Assignment_URL { get; set; } = string.Empty;
    }

    public class QuizCreateDto
    {
        public string Quiz_Name { get; set; } = string.Empty;
        public string Quiz_Details { get; set; } = string.Empty;
        public DateTime Quiz_Date { get; set; }
        public string Module_Code { get; set; } = string.Empty;
    }

    public class QuizQuestionSaveDto
    {
        public string Question_Text { get; set; } = string.Empty;
        public int Question_Order { get; set; }
        public List<QuizOptionSaveDto> Options { get; set; } = new();
    }

    public class QuizOptionSaveDto
    {
        public string Option_Text { get; set; } = string.Empty;
        public bool Is_Correct { get; set; }
    }

    public class QuizSubmitDto
    {
        public int Student_ID { get; set; }
        public List<StudentAnswerDto> Answers { get; set; } = new();
    }

    public class StudentAnswerDto
    {
        public int Question_ID { get; set; }
        public int Option_ID { get; set; }
    }
}