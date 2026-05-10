namespace TutorConnect.API.DTOs
{
    // --- PAYMENT DTOs ---
    public class PaymentCreateDto
    {
        public decimal Amount { get; set; }
        public string? Account_Name { get; set; }
        public string? Account_Number { get; set; }
        public string? Branch_Code { get; set; }
        public string? Bank { get; set; }
        public string? Payment_Reference { get; set; }
        public int Student_ID { get; set; }
        public string Module_Code { get; set; } = string.Empty;
    }

    public class PaymentReturnDto
    {
        public int Payment_ID { get; set; }
        public decimal Amount { get; set; }
        public DateTime Payment_Date { get; set; }
        public string Payment_Status { get; set; } = string.Empty;
        public string? Bank { get; set; }
        public string? Payment_Reference { get; set; }
        public string Module_Code { get; set; } = string.Empty;
    }

    // --- NOTIFICATION DTOs ---
    public class NotificationReturnDto
    {
        public int Notification_ID { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Date_Sent { get; set; }
        public bool Is_Read { get; set; }
    }
}