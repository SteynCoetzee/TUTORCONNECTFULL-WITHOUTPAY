namespace TutorConnect.API.DTOs
{
    public class ModuleCreateDto
    {
        public string Module_Code { get; set; } = string.Empty;
        public string Module_Name { get; set; } = string.Empty;
        public string Module_Description { get; set; } = string.Empty;
        public decimal Module_Price { get; set; }
    }

    public class ModuleReturnDto
    {
        public string Module_Code { get; set; } = string.Empty;
        public string Module_Name { get; set; } = string.Empty;
        public string Module_Description { get; set; } = string.Empty;
        public decimal Module_Price { get; set; }
    }

    public class TutorModuleAssignDto
    {
        public int Tutor_ID { get; set; }
        public string Module_Code { get; set; } = string.Empty;
    }

    public class TutorModuleViewDto
    {
        public int Tutor_Module_ID { get; set; }
        public int Tutor_ID { get; set; }
        public string Tutor_Name { get; set; } = string.Empty;
        public string Module_Code { get; set; } = string.Empty;
        public string Module_Name { get; set; } = string.Empty;
        public DateTime Assigned_Date { get; set; }
        public bool IsActive { get; set; }
    }
}