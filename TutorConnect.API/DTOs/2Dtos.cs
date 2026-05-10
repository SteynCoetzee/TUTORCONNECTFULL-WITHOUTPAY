namespace TutorConnect.API.DTOs
{
    public class ResourceCreateDto
    {
        public string Module_Resource_Name { get; set; } = string.Empty;
        public string Module_Resource_Type_ID { get; set; } = string.Empty;
        public string Module_Code { get; set; } = string.Empty;
        public string Resource_URL { get; set; } = string.Empty;
        public string Folder_Name { get; set; } = string.Empty;
    }

    public class ResourceVisibilityDto
    {
        public bool Is_Visible { get; set; }
    }

    public class FolderVisibilityDto
    {
        public string Module_Code { get; set; } = string.Empty;
        public string Folder_Name { get; set; } = string.Empty;
        public bool Is_Visible { get; set; }
    }

    public class LogHoursCreateDto
    {
        // Swagger will automatically format these nicely because of the DateOnly/TimeOnly types!
        public DateOnly Log_Hours_Date { get; set; }
        public TimeOnly Log_Hours_Time { get; set; }
        public decimal Log_Hours_Amount { get; set; }
        public int Tutor_ID { get; set; }
    }
}