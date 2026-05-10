namespace TutorConnect.API.DTOs
{
    // --- ATTENDANCE ---
    public class AttendanceCreateDto
    {
        public int Session_ID { get; set; }
        public int Student_ID { get; set; }
        public int Attendance_Status_ID { get; set; }
    }

    // --- FAQS (ADMIN CONTENT) ---
    public class FAQCreateDto
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int FAQ_Category_ID { get; set; }
    }

    public class FAQUpdateDto
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int FAQ_Category_ID { get; set; }
    }

    public class FAQCategoryCreateDto
    {
        public string Category_Name { get; set; } = string.Empty;
    }

    // --- MEDIA CONTENT ---
    public class MediaCreateDto
    {
        public string Media_Name { get; set; } = string.Empty;
        public string Media_Address { get; set; } = string.Empty;
    }

    // --- HELP PAGE ---
    public class HelpPageCreateDto
    {
        public string Help_Page_Title { get; set; } = string.Empty;
        public string Help_Page_Description { get; set; } = string.Empty;
    }

    public class HelpResourceCreateDto
    {
        public string Video_Title { get; set; } = string.Empty;
        public string Video_URL { get; set; } = string.Empty;
    }

    // --- TESTIMONIAL CATEGORY ---
    public class TestimonialCategoryCreateDto
    {
        public string Test_Category_Name { get; set; } = string.Empty;
    }

    public class TestimonialUpdateDto
    {
        public string Description { get; set; } = string.Empty;
        public int Testimonial_Category_ID { get; set; }
    }

    // --- INTERNAL REVIEWS ---
    public class TutorReviewCreateDto
    {
        public int Rating { get; set; }
        public int Student_ID { get; set; }
        public int Tutor_ID { get; set; }
    }

    public class SessionReviewCreateDto
    {
        public int Rating { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Student_ID { get; set; }
        public int Session_ID { get; set; }
    }

    // --- PUBLIC TESTIMONIALS ---
    public class TestimonialCreateDto
    {
        public string Description { get; set; } = string.Empty;
        public int Student_ID { get; set; }
        public int Testimonial_Category_ID { get; set; }
    }
}