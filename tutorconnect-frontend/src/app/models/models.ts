// ─── Auth & User ──────────────────────────────────────────────────────────────
export interface UserProfile {
  user_ID: number;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  address?: string;
  bio?: string;
  user_Role_ID: number;
  roleName?: string;
}

export interface UserProfileUpdate {
  firstName: string;
  lastName: string;
  phone?: string;
  address?: string;
  bio?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  roleId: number;
}

// ─── Modules ──────────────────────────────────────────────────────────────────
export interface Module {
  module_Code: string;
  module_Name: string;
  module_Description: string;
  module_Price: number;
}

export interface ModuleResource {
  module_Resource_ID: number;
  module_Resource_Name: string;
  module_Resource_Type_ID: string; // 'PDF' | 'Link' | 'Video'
  resource_URL: string;
  folder_Name: string;
  is_Visible: boolean;
  folder_Is_Visible: boolean;
  date_Added: string;
  module_Code: string;
}

export interface ModuleAssignment {
  assignment_ID: number;
  assignment_Name: string;
  assignment_Date: string;
  assignment_URL: string;
  is_Visible: boolean;
  module_Code: string;
  hasSubmitted?: boolean;
  submissionDate?: string;
  submissionFile?: string;
  submissionUrl?: string;
  grade?: number | null;
  feedback?: string | null;
}

export interface ModuleQuiz {
  quiz_ID: number;
  quiz_Name: string;
  quiz_Details: string;
  quiz_Date: string;
  module_Code: string;
  is_Visible: boolean;
  hasSubmitted?: boolean;
  bestScore?: number | null;
}

export interface Enrollment {
  enrollment_ID: number;
  student_ID: number;
  module_Code: string;
  module_Name: string;
  enrollment_Date: string;
  isActive: boolean;
}

export interface TutorModuleAssignment {
  tutor_Module_ID: number;
  tutor_ID: number;
  tutor_Name: string;
  module_Code: string;
  module_Name: string;
  assigned_Date: string;
  isActive: boolean;
}

// ─── Announcements ────────────────────────────────────────────────────────────
export interface Announcement {
  announcement_ID: number;
  announcement_Name: string;
  announcement_Details: string;
  announcement_Type: string;
  date_Posted: string;
  tutor_ID?: number;
  admin_ID?: number;
  module_Code: string;
}

export interface AnnouncementCreate {
  announcement_Name: string;
  announcement_Details: string;
  announcement_Type: string;
  tutor_ID?: number;
  admin_ID?: number;
  module_Code: string;
}

export interface AnnouncementUpdate {
  announcement_Name: string;
  announcement_Details: string;
  announcement_Type: string;
  module_Code: string;
}

// ─── Bookings ─────────────────────────────────────────────────────────────────
export interface BookingSlot {
  booking_Slot_ID: number;
  slot_Date: string;
  slot_Time: string;
  session_Type: string;
  is_Booked: boolean;
  tutor_ID: number;
}

export interface Booking {
  booking_ID: number;
  student_ID: number;
  booking_Slot_ID: number;
  booking_Slot?: BookingSlot;
}

export interface BookingCreate {
  student_ID: number;
  booking_Slot_ID: number;
}

// ─── Notifications ────────────────────────────────────────────────────────────
export interface Notification {
  notification_ID: number;
  message: string;
  date_Sent: string;
  is_Read: boolean;
}

// ─── Reports ──────────────────────────────────────────────────────────────────
export interface TutorHoursReport {
  tutorId: number;
  totalHoursWorked: number;
}

export interface MonthlyIncomeReport {
  month: number;
  totalIncome: number;
}

export interface TutorRatingsReport {
  tutorId: number;
  averageRating: number;
  totalReviews: number;
}

export interface MonthlyStudentsReport {
  month: number;
  uniqueStudents: number;
}

export interface SessionsReport {
  bookingId: number;
  studentId: number;
  slotDate: string;
  slotTime: string;
  sessionType: string;
}

export interface PopularModulesReport {
  moduleCode: string;
  announcementCount: number;
}

// ─── Testimonials ─────────────────────────────────────────────────────────────
export interface Testimonial {
  testimonial_ID: number;
  testimonial_Description: string;
  student_ID: number;
  testimonial_Category_ID: number;
  isApproved: boolean;
}

export interface TestimonialCreate {
  description: string;
  student_ID: number;
  testimonial_Category_ID: number;
}

export interface TestimonialUpdate {
  description: string;
  testimonial_Category_ID: number;
}

export interface TestimonialCategory {
  testimonial_Category_ID: number;
  test_Category_Name: string;
}

// ─── JWT Token Claims ─────────────────────────────────────────────────────────
export interface DecodedToken {
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': string;
  exp: number;
}
