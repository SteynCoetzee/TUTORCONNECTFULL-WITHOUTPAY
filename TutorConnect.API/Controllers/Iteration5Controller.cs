using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnect.API.Data;
using TutorConnect.API.DTOs;
using TutorConnect.API.Models;
using TutorConnect.API.Services;

namespace TutorConnect.API.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // ASSIGNMENTS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class AssignmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryService _cloudinary;
        private readonly AuditService _audit;
        public AssignmentsController(AppDbContext context, CloudinaryService cloudinary, AuditService audit)
        {
            _context = context;
            _cloudinary = cloudinary;
            _audit = audit;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Assignment>>> GetAssignments() => await _context.Assignments.ToListAsync();

        // GET: api/Assignments/module/{moduleCode}?studentId=X
        [HttpGet("module/{moduleCode}")]
        public async Task<ActionResult> GetModuleAssignments(string moduleCode, [FromQuery] int? studentId = null)
        {
            var assignments = await _context.Assignments
                .Where(a => a.Module_Code == moduleCode)
                .OrderBy(a => a.Assignment_Date)
                .ToListAsync();

            List<Assignment_Submission> submissions = new();
            if (studentId.HasValue)
            {
                var ids = assignments.Select(a => a.Assignment_ID).ToList();
                submissions = await _context.Assignment_Submissions
                    .Where(s => ids.Contains(s.Assignment_ID) && s.Student_ID == studentId.Value)
                    .ToListAsync();
            }

            return Ok(assignments.Select(a => {
                var sub = submissions.FirstOrDefault(s => s.Assignment_ID == a.Assignment_ID);
                return new {
                    a.Assignment_ID, a.Assignment_Name, a.Assignment_Date,
                    a.Assignment_URL, a.Is_Visible, a.Module_Code,
                    HasSubmitted    = sub != null,
                    SubmissionDate  = sub?.Submission_Date,
                    SubmissionFile  = sub?.File_Name,
                    SubmissionUrl   = sub?.File_Path,
                    Grade           = sub?.Grade,
                    Feedback        = sub?.Feedback
                };
            }));
        }

        // GET: api/Assignments/{id}/download-brief
        // Fetches from Cloudinary server-side (bypasses browser CORS/auth issues)
        [HttpGet("{id}/download-brief")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadBrief(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null || string.IsNullOrEmpty(assignment.Assignment_URL))
                return NotFound("Brief not found.");

            using var http = new HttpClient();
            var response = await http.GetAsync(assignment.Assignment_URL);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode,
                    "Could not fetch file from storage. If this persists, check Cloudinary access settings.");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            return File(bytes, "application/pdf", $"{assignment.Assignment_Name}.pdf");
        }

        // POST: api/Assignments/upload-brief  — uploads PDF to Cloudinary
        [HttpPost("upload-brief")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult> UploadBrief(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file provided.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf") return BadRequest("Only PDF files are accepted.");
            using var stream = file.OpenReadStream();
            var fileName = $"brief_{Guid.NewGuid()}{ext}";
            try
            {
                var url = await _cloudinary.UploadRawAsync(stream, fileName);
                return Ok(url);
            }
            catch (Exception ex) { return StatusCode(500, $"Upload failed: {ex.Message}"); }
        }

// GET: api/Assignments/submissions/{submissionId}/download
        [HttpGet("submissions/{submissionId}/download")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> DownloadSubmission(int submissionId)
        {
            var sub = await _context.Assignment_Submissions.FindAsync(submissionId);
            if (sub == null || string.IsNullOrEmpty(sub.File_Path))
                return NotFound("Submission file not found.");

            using var http = new HttpClient();
            var response = await http.GetAsync(sub.File_Path);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Failed to fetch file.");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            return File(bytes, "application/octet-stream", sub.File_Name);
        }

        [HttpPost]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult> CreateAssignment(AssignmentCreateDto request)
        {
            if (string.IsNullOrEmpty(request.Assignment_Name) || string.IsNullOrEmpty(request.Module_Code))
                return BadRequest("Assignment name and module code are required.");

            var assignment = new Assignment
            {
                Assignment_Name = request.Assignment_Name,
                Assignment_Date = request.Assignment_Date,
                Module_Code     = request.Module_Code,
                Assignment_URL  = request.Assignment_URL,
                Is_Visible      = false
            };
            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Assignment created.", assignmentId = assignment.Assignment_ID });
        }

        // PUT: api/Assignments/{id}/visibility
        [HttpPut("{id}/visibility")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> ToggleVisibility(int id, [FromBody] ResourceVisibilityDto request)
        {
            var a = await _context.Assignments.FindAsync(id);
            if (a == null) return NotFound("Assignment not found.");
            a.Is_Visible = request.Is_Visible;
            await _context.SaveChangesAsync();
            return Ok("Visibility updated.");
        }

        // POST: api/Assignments/{id}/submit  — student submits via Cloudinary
        [HttpPost("{id}/submit")]
        public async Task<IActionResult> SubmitAssignment(int id, [FromForm] int studentId, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File is required.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".doc", ".docx", ".zip" };
            if (!allowed.Contains(ext)) return BadRequest("Only PDF, Word, or ZIP files accepted.");

            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null) return NotFound("Assignment not found.");

            // Check for existing submission (one per student per assignment)
            var existing = await _context.Assignment_Submissions
                .FirstOrDefaultAsync(s => s.Assignment_ID == id && s.Student_ID == studentId);

            string fileUrl;
            try
            {
                using var stream = file.OpenReadStream();
                var fileName = $"submission_{studentId}_{id}_{Guid.NewGuid()}{ext}";
                fileUrl = await _cloudinary.UploadRawAsync(stream, fileName);
            }
            catch (Exception ex) { return StatusCode(500, $"Upload failed: {ex.Message}"); }

            if (existing != null)
            {
                // Update re-submission
                existing.File_Name = file.FileName;
                existing.File_Path = fileUrl;
                existing.File_Type = ext.TrimStart('.');
                existing.File_Size = file.Length;
                existing.Submission_Date = DateTime.UtcNow;
            }
            else
            {
                _context.Assignment_Submissions.Add(new Assignment_Submission {
                    Assignment_ID   = id,
                    Student_ID      = studentId,
                    File_Name       = file.FileName,
                    File_Path       = fileUrl,
                    File_Type       = ext.TrimStart('.'),
                    File_Size       = file.Length,
                    Submission_Date = DateTime.UtcNow
                });
            }

            _context.Notifications.Add(new Notification {
                User_ID    = studentId,
                Message    = $"Assignment '{assignment.Assignment_Name}' submitted successfully.",
                Date_Sent  = DateTime.UtcNow,
                Is_Read    = false
            });

            await _context.SaveChangesAsync();
            await _audit.LogAsync(studentId, "Assignment Submitted", $"Assignment_ID: {id}, File: {file.FileName}");
            return Ok(new { message = "Assignment submitted.", fileName = file.FileName });
        }

        // GET: api/Assignments/{id}/submissions  — tutor sees all submissions with student names
        [HttpGet("{id}/submissions")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult> GetAssignmentSubmissions(int id)
        {
            var submissions = await _context.Assignment_Submissions
                .Where(s => s.Assignment_ID == id)
                .OrderByDescending(s => s.Submission_Date)
                .ToListAsync();

            var studentIds = submissions.Select(s => s.Student_ID).Distinct().ToList();
            var students = await _context.Users
                .Where(u => studentIds.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => $"{u.FirstName} {u.LastName}");

            return Ok(submissions.Select(s => new {
                s.Submission_ID, s.Assignment_ID, s.Student_ID,
                StudentName     = students.TryGetValue(s.Student_ID, out var n) ? n : $"Student #{s.Student_ID}",
                s.File_Name,
                DownloadUrl     = s.File_Path,
                s.Submission_Date,
                s.Grade,
                s.Feedback
            }));
        }

        // PUT: api/assignments/{assignmentId}/submissions/{submissionId}/grade
        /// <summary>
        /// Grade a student submission
        /// </summary>
        [HttpPut("{assignmentId}/submissions/{submissionId}/grade")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> GradeSubmission(int assignmentId, int submissionId, [FromBody] GradeSubmissionDto gradeRequest)
        {
            var submission = await _context.Assignment_Submissions.FindAsync(submissionId);
            if (submission == null)
                return NotFound("Submission not found.");

            decimal? grade = gradeRequest.Grade;
            string? feedback = gradeRequest.Feedback;

            if (grade < 0 || grade > 100)
                return BadRequest("Grade must be between 0 and 100.");

            submission.Grade = grade;
            submission.Feedback = feedback;
            submission.Feedback_Date = DateTime.UtcNow;

            // Send notification to student
            var notification = new Notification
            {
                User_ID = submission.Student_ID,
                Message = $"Your assignment has been graded: {grade}%",
                Date_Sent = DateTime.UtcNow,
                Is_Read = false
            };
            _context.Notifications.Add(notification);

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Submission graded successfully.");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"Failed to grade submission: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> UpdateAssignment(int id, [FromBody] AssignmentCreateDto request)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null)
                return NotFound("Assignment not found.");

            assignment.Assignment_Name = request.Assignment_Name;
            assignment.Assignment_Date = request.Assignment_Date;

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Assignment updated successfully.");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"Failed to update assignment: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null)
                return NotFound("Assignment not found.");

            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();
            return Ok("Assignment deleted successfully.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GRADES CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class GradesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public GradesController(AppDbContext context) { _context = context; }

        // GET: api/grades/student/{studentId}
        /// <summary>
        /// Get all grades for a student across all modules
        /// </summary>
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<List<GradeViewDto>>> GetStudentGrades(int studentId)
        {
            var grades = new List<GradeViewDto>();

            // Get quiz grades
            var quizGrades = await _context.Student_Quizzes
                .Include(sq => sq.Quiz)
                .Where(sq => sq.Student_ID == studentId)
                .Select(sq => new GradeViewDto
                {
                    Grade_ID = sq.Student_Quiz_ID,
                    Assessment_Name = sq.Quiz != null ? sq.Quiz.Quiz_Name : "Unknown Quiz",
                    Assessment_Type = "Quiz",
                    Score = sq.Quiz_Score,
                    Total_Points = 100, // Assuming out of 100
                    Percentage = sq.Quiz_Score,
                    Feedback = null,
                    Grade_Date = sq.Quiz != null ? sq.Quiz.Quiz_Date : DateTime.UtcNow
                })
                .ToListAsync();

            grades.AddRange(quizGrades);

            // Get assignment grades
            var assignmentGrades = await _context.Assignment_Submissions
                .Include(asub => asub.Assignment)
                .Where(asub => asub.Student_ID == studentId && asub.Grade.HasValue)
                .Select(asub => new GradeViewDto
                {
                    Grade_ID = asub.Submission_ID,
                    Assessment_Name = asub.Assignment != null ? asub.Assignment.Assignment_Name : "Unknown Assignment",
                    Assessment_Type = "Assignment",
                    Score = asub.Grade ?? 0,
                    Total_Points = 100, // Assuming out of 100
                    Percentage = (asub.Grade ?? 0),
                    Feedback = asub.Feedback,
                    Grade_Date = asub.Feedback_Date ?? DateTime.UtcNow
                })
                .ToListAsync();

            grades.AddRange(assignmentGrades);

            return Ok(grades.OrderByDescending(g => g.Grade_Date).ToList());
        }

        // GET: api/grades/student/{studentId}/module/{moduleCode}
        /// <summary>
        /// Get grades for a student in a specific module
        /// </summary>
        [HttpGet("student/{studentId}/module/{moduleCode}")]
        public async Task<ActionResult<List<GradeViewDto>>> GetStudentModuleGrades(int studentId, string moduleCode)
        {
            var grades = new List<GradeViewDto>();

            // Get quiz grades for module
            var quizGrades = await _context.Student_Quizzes
                .Include(sq => sq.Quiz)
                .Where(sq => sq.Student_ID == studentId && sq.Quiz != null && sq.Quiz.Module_Code == moduleCode)
                .Select(sq => new GradeViewDto
                {
                    Grade_ID = sq.Student_Quiz_ID,
                    Assessment_Name = sq.Quiz != null ? sq.Quiz.Quiz_Name : "Unknown Quiz",
                    Assessment_Type = "Quiz",
                    Score = sq.Quiz_Score,
                    Total_Points = 100,
                    Percentage = sq.Quiz_Score,
                    Feedback = null,
                    Grade_Date = sq.Quiz != null ? sq.Quiz.Quiz_Date : DateTime.UtcNow
                })
                .ToListAsync();

            grades.AddRange(quizGrades);

            // Get assignment grades for module
            var assignmentGrades = await _context.Assignment_Submissions
                .Include(asub => asub.Assignment)
                .Where(asub => asub.Student_ID == studentId
                    && asub.Grade.HasValue
                    && asub.Assignment != null
                    && asub.Assignment.Module_Code == moduleCode)
                .Select(asub => new GradeViewDto
                {
                    Grade_ID = asub.Submission_ID,
                    Assessment_Name = asub.Assignment != null ? asub.Assignment.Assignment_Name : "Unknown Assignment",
                    Assessment_Type = "Assignment",
                    Score = asub.Grade ?? 0,
                    Total_Points = 100,
                    Percentage = (asub.Grade ?? 0),
                    Feedback = asub.Feedback,
                    Grade_Date = asub.Feedback_Date ?? DateTime.UtcNow
                })
                .ToListAsync();

            grades.AddRange(assignmentGrades);

            if (grades.Count == 0)
                return Ok(new List<GradeViewDto>());

            return Ok(grades.OrderByDescending(g => g.Grade_Date).ToList());
        }

        // GET: api/grades/quiz/{quizId}/student/{studentId}
        /// <summary>
        /// Get specific quiz grade for a student
        /// </summary>
        [HttpGet("quiz/{quizId}/student/{studentId}")]
        public async Task<ActionResult<GradeViewDto>> GetQuizGrade(int quizId, int studentId)
        {
            var quizGrade = await _context.Student_Quizzes
                .Include(sq => sq.Quiz)
                .FirstOrDefaultAsync(sq => sq.Quiz_ID == quizId && sq.Student_ID == studentId);

            if (quizGrade == null)
                return NotFound("Grade not found.");

            var gradeDto = new GradeViewDto
            {
                Grade_ID = quizGrade.Student_Quiz_ID,
                Assessment_Name = quizGrade.Quiz != null ? quizGrade.Quiz.Quiz_Name : "Unknown Quiz",
                Assessment_Type = "Quiz",
                Score = quizGrade.Quiz_Score,
                Total_Points = 100,
                Percentage = quizGrade.Quiz_Score,
                Feedback = null,
                Grade_Date = quizGrade.Quiz != null ? quizGrade.Quiz.Quiz_Date : DateTime.UtcNow
            };

            return Ok(gradeDto);
        }

        // GET: api/grades/average/student/{studentId}
        /// <summary>
        /// Get average grade for a student across all assessments
        /// </summary>
        [HttpGet("average/student/{studentId}")]
        public async Task<ActionResult<object>> GetStudentAverageGrade(int studentId)
        {
            var quizScores = await _context.Student_Quizzes
                .Where(sq => sq.Student_ID == studentId)
                .Select(sq => sq.Quiz_Score)
                .ToListAsync();

            var assignmentScores = await _context.Assignment_Submissions
                .Where(asub => asub.Student_ID == studentId && asub.Grade.HasValue)
                .Select(asub => asub.Grade ?? 0)
                .ToListAsync();

            var allScores = new List<decimal>();
            allScores.AddRange(quizScores);
            allScores.AddRange(assignmentScores);

            var averageGrade = allScores.Count > 0 ? allScores.Average() : 0;

            return Ok(new
            {
                studentId = studentId,
                averageGrade = Math.Round(averageGrade, 2),
                totalAssessments = allScores.Count,
                quizCount = quizScores.Count,
                assignmentCount = assignmentScores.Count
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // QUIZZES CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class QuizzesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        public QuizzesController(AppDbContext context, AuditService audit) { _context = context; _audit = audit; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Quiz>>> GetQuizzes() => await _context.Quizzes.ToListAsync();

        // GET: api/Quizzes/module/{moduleCode}?studentId=X
        [HttpGet("module/{moduleCode}")]
        public async Task<ActionResult> GetModuleQuizzes(string moduleCode, [FromQuery] int? studentId = null)
        {
            var quizzes = await _context.Quizzes
                .Where(q => q.Module_Code == moduleCode)
                .OrderBy(q => q.Quiz_Date)
                .ToListAsync();

            // If a student ID is provided, fetch their submissions for all quizzes at once
            List<Student_Quiz> submissions = new();
            if (studentId.HasValue)
            {
                var quizIds = quizzes.Select(q => q.Quiz_ID).ToList();
                submissions = await _context.Student_Quizzes
                    .Where(sq => quizIds.Contains(sq.Quiz_ID) && sq.Student_ID == studentId.Value && sq.End_Time != null)
                    .ToListAsync();
            }

            return Ok(quizzes.Select(q => {
                var qSubmissions = submissions.Where(sq => sq.Quiz_ID == q.Quiz_ID).ToList();
                return new {
                    q.Quiz_ID, q.Quiz_Name, q.Quiz_Details, q.Quiz_Date, q.Module_Code, q.Is_Visible,
                    HasSubmitted = qSubmissions.Any(),
                    BestScore    = qSubmissions.Any() ? (decimal?)qSubmissions.Max(sq => sq.Quiz_Score) : null
                };
            }));
        }

        // GET: api/Quizzes/{id}/questions  — full quiz with questions + options
        [HttpGet("{id}/questions")]
        public async Task<ActionResult> GetQuizWithQuestions(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Quiz not found.");

            var questions = await _context.Quiz_Questions
                .Where(q => q.Quiz_ID == id)
                .OrderBy(q => q.Question_Order)
                .ToListAsync();

            var questionIds = questions.Select(q => q.Question_ID).ToList();
            var options = await _context.Quiz_Question_Options
                .Where(o => questionIds.Contains(o.Question_ID))
                .ToListAsync();

            return Ok(new {
                quiz.Quiz_ID, quiz.Quiz_Name, quiz.Quiz_Details, quiz.Quiz_Date, quiz.Module_Code, quiz.Is_Visible,
                Questions = questions.Select(q => new {
                    q.Question_ID, q.Question_Text, q.Question_Order,
                    Options = options
                        .Where(o => o.Question_ID == q.Question_ID)
                        .Select(o => new { o.Option_ID, o.Option_Text, o.Is_Correct })
                        .ToList()
                }).ToList()
            });
        }

        // POST: api/Quizzes
        [HttpPost]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult> CreateQuiz(QuizCreateDto request)
        {
            if (string.IsNullOrEmpty(request.Quiz_Name) || string.IsNullOrEmpty(request.Module_Code))
                return BadRequest("Quiz name and module code are required.");

            var quiz = new Quiz {
                Quiz_Name = request.Quiz_Name,
                Quiz_Details = request.Quiz_Details,
                Quiz_Date = request.Quiz_Date,
                Module_Code = request.Module_Code,
                Is_Visible = false
            };
            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Quiz created.", quizId = quiz.Quiz_ID });
        }

        // POST: api/Quizzes/{id}/questions  — save all questions (replaces existing)
        [HttpPost("{id}/questions")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> SaveQuizQuestions(int id, [FromBody] List<QuizQuestionSaveDto> questions)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Quiz not found.");

            // Remove existing questions and options
            var existing = await _context.Quiz_Questions.Where(q => q.Quiz_ID == id).ToListAsync();
            var existingIds = existing.Select(q => q.Question_ID).ToList();
            var existingOptions = await _context.Quiz_Question_Options
                .Where(o => existingIds.Contains(o.Question_ID)).ToListAsync();
            _context.Quiz_Question_Options.RemoveRange(existingOptions);
            _context.Quiz_Questions.RemoveRange(existing);
            await _context.SaveChangesAsync();

            // Save new questions + options
            for (int i = 0; i < questions.Count; i++)
            {
                var qDto = questions[i];
                var question = new Quiz_Question {
                    Quiz_ID = id,
                    Question_Text = qDto.Question_Text,
                    Question_Order = i
                };
                _context.Quiz_Questions.Add(question);
                await _context.SaveChangesAsync();

                foreach (var opt in qDto.Options)
                    _context.Quiz_Question_Options.Add(new Quiz_Question_Option {
                        Question_ID = question.Question_ID,
                        Option_Text = opt.Option_Text,
                        Is_Correct = opt.Is_Correct
                    });
            }
            await _context.SaveChangesAsync();
            return Ok("Questions saved.");
        }

        // POST: api/Quizzes/{id}/start  — student starts attempt
        [HttpPost("{id}/start")]
        public async Task<ActionResult> StartQuiz(int id, [FromBody] int studentId)
        {
            // Return existing active attempt if any
            var active = await _context.Student_Quizzes
                .FirstOrDefaultAsync(sq => sq.Quiz_ID == id && sq.Student_ID == studentId && sq.End_Time == null);
            if (active != null)
                return Ok(new { studentQuizId = active.Student_Quiz_ID, alreadyStarted = true });

            // Block if already completed
            var completed = await _context.Student_Quizzes
                .AnyAsync(sq => sq.Quiz_ID == id && sq.Student_ID == studentId && sq.End_Time != null);
            if (completed) return BadRequest("You have already completed this quiz.");

            var attempt = new Student_Quiz {
                Quiz_ID = id, Student_ID = studentId,
                Quiz_Score = 0, Start_Time = DateTime.UtcNow
            };
            _context.Student_Quizzes.Add(attempt);
            await _context.SaveChangesAsync();
            return Ok(new { studentQuizId = attempt.Student_Quiz_ID, alreadyStarted = false });
        }

        // POST: api/Quizzes/{id}/submit
        [HttpPost("{id}/submit")]
        public async Task<ActionResult> SubmitQuiz(int id, [FromBody] QuizSubmitDto request)
        {
            var attempt = await _context.Student_Quizzes
                .FirstOrDefaultAsync(sq => sq.Quiz_ID == id && sq.Student_ID == request.Student_ID && sq.End_Time == null);
            if (attempt == null) return BadRequest("No active quiz attempt. Start the quiz first.");

            var questions = await _context.Quiz_Questions.Where(q => q.Quiz_ID == id).ToListAsync();
            var questionIds = questions.Select(q => q.Question_ID).ToList();
            var correctOptions = await _context.Quiz_Question_Options
                .Where(o => questionIds.Contains(o.Question_ID) && o.Is_Correct).ToListAsync();

            int correct = 0;
            foreach (var ans in request.Answers)
            {
                _context.Student_Quiz_Answers.Add(new Student_Quiz_Answer {
                    Student_Quiz_ID = attempt.Student_Quiz_ID,
                    Question_ID = ans.Question_ID,
                    Option_ID = ans.Option_ID
                });
                if (correctOptions.Any(o => o.Question_ID == ans.Question_ID && o.Option_ID == ans.Option_ID))
                    correct++;
            }

            decimal score = questions.Count > 0 ? Math.Round((decimal)correct / questions.Count * 100, 1) : 0;
            attempt.Quiz_Score = score;
            attempt.End_Time = DateTime.UtcNow;
            attempt.Submission_Date = DateTime.UtcNow;

            _context.Notifications.Add(new Notification {
                User_ID = request.Student_ID,
                Message = $"Quiz submitted: {(await _context.Quizzes.FindAsync(id))?.Quiz_Name}. Score: {score}%",
                Date_Sent = DateTime.UtcNow, Is_Read = false
            });

            await _context.SaveChangesAsync();
            await _audit.LogAsync(request.Student_ID, "Quiz Submitted", $"Quiz_ID: {id}, Score: {score}%, Correct: {correct}/{questions.Count}");
            return Ok(new { score, correct, total = questions.Count });
        }

        // GET: api/Quizzes/{id}/submissions  — all student submissions (tutor/admin)
        [HttpGet("{id}/submissions")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult> GetSubmissions(int id)
        {
            var submissions = await _context.Student_Quizzes
                .Where(sq => sq.Quiz_ID == id && sq.End_Time != null)
                .OrderByDescending(sq => sq.End_Time)
                .ToListAsync();

            var studentIds = submissions.Select(sq => sq.Student_ID).Distinct().ToList();
            var students = await _context.Users
                .Where(u => studentIds.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => $"{u.FirstName} {u.LastName}");

            return Ok(submissions.Select(sq => new
            {
                sq.Student_Quiz_ID,
                sq.Student_ID,
                StudentName    = students.TryGetValue(sq.Student_ID, out var name) ? name : $"Student #{sq.Student_ID}",
                sq.Quiz_Score,
                sq.Start_Time,
                sq.End_Time,
                sq.Submission_Date,
                DurationSeconds = sq.Start_Time.HasValue && sq.End_Time.HasValue
                    ? (int)(sq.End_Time.Value - sq.Start_Time.Value).TotalSeconds
                    : (int?)null
            }));
        }

        // GET: api/Quizzes/{id}/student/{studentId}/result
        [HttpGet("{id}/student/{studentId}/result")]
        public async Task<ActionResult> GetQuizResult(int id, int studentId)
        {
            var result = await _context.Student_Quizzes
                .FirstOrDefaultAsync(sq => sq.Quiz_ID == id && sq.Student_ID == studentId && sq.End_Time != null);
            if (result == null) return NotFound("No submission found.");
            return Ok(new { score = result.Quiz_Score, startTime = result.Start_Time, endTime = result.End_Time, submissionDate = result.Submission_Date });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult> UpdateQuiz(int id, [FromBody] QuizCreateDto request)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Quiz not found.");
            quiz.Quiz_Name = request.Quiz_Name;
            quiz.Quiz_Details = request.Quiz_Details;
            quiz.Quiz_Date = request.Quiz_Date;
            await _context.SaveChangesAsync();
            return Ok("Quiz updated.");
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult> DeleteQuiz(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Quiz not found.");
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
            return Ok("Quiz deleted.");
        }

        [HttpPut("{id}/visibility")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> ToggleVisibility(int id, [FromBody] ResourceVisibilityDto request)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Quiz not found.");
            quiz.Is_Visible = request.Is_Visible;
            await _context.SaveChangesAsync();
            return Ok("Visibility updated.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANNOUNCEMENTS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class AnnouncementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AnnouncementsController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Announcement>>> GetAnnouncements()
            => await _context.Announcements.OrderByDescending(a => a.Date_Posted).ToListAsync();

        [HttpGet("website")]
        public async Task<ActionResult<IEnumerable<Announcement>>> GetWebsiteAnnouncements()
            => await _context.Announcements
                .Where(a => a.Module_Code == null || a.Module_Code == string.Empty)
                .OrderByDescending(a => a.Date_Posted).ToListAsync();

        [HttpGet("module/{moduleCode}")]
        public async Task<ActionResult<IEnumerable<Announcement>>> GetModuleAnnouncements(string moduleCode)
            => await _context.Announcements
                .Where(a => a.Module_Code == moduleCode)
                .OrderByDescending(a => a.Date_Posted).ToListAsync();

        // GET: api/Announcements/student/{studentId}
        // Returns announcements for all modules the student is actively enrolled in
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<Announcement>>> GetStudentAnnouncements(int studentId)
        {
            var enrolledCodes = await _context.Student_Modules
                .Where(sm => sm.Student_ID == studentId && sm.IsActive)
                .Select(sm => sm.Module_Code)
                .ToListAsync();

            var announcements = await _context.Announcements
                .Where(a => a.Module_Code != null && enrolledCodes.Contains(a.Module_Code))
                .OrderByDescending(a => a.Date_Posted)
                .ToListAsync();

            return Ok(announcements);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Announcement>> GetAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();
            return announcement;
        }

        [HttpPost]
        public async Task<ActionResult> CreateAnnouncement(AnnouncementCreateDto request)
        {
            var announcement = new Announcement
            {
                Announcement_Name = request.Announcement_Name,
                Announcement_Details = request.Announcement_Details,
                Announcement_Type = request.Announcement_Type,
                Date_Posted = DateTime.UtcNow,
                Tutor_ID = request.Tutor_ID,
                Admin_ID = request.Admin_ID,
                Module_Code = request.Module_Code
            };
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            return Ok("Announcement created.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnnouncement(int id, AnnouncementUpdateDto request)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound("Announcement not found.");

            announcement.Announcement_Name = request.Announcement_Name;
            announcement.Announcement_Details = request.Announcement_Details;
            announcement.Announcement_Type = request.Announcement_Type;
            announcement.Module_Code = request.Module_Code;

            await _context.SaveChangesAsync();
            return Ok("Announcement updated.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();
            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync();
            return Ok("Announcement deleted.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REVIEWS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReviewsController(AppDbContext context) { _context = context; }

        // --- TUTOR REVIEWS ---
        // GET: api/Reviews/tutor/{tutorId}  — reviews FOR a tutor (tutor views their own feedback)
        [HttpGet("tutor/{tutorId}")]
        public async Task<ActionResult> GetTutorReviews(int tutorId)
        {
            var reviews = await _context.Tutor_Reviews
                .Where(r => r.Tutor_ID == tutorId)
                .ToListAsync();

            var tutorIds = reviews.Select(r => r.Tutor_ID).Distinct().ToList();
            var studentIds = reviews.Select(r => r.Student_ID).Distinct().ToList();
            var users = await _context.Users
                .Where(u => tutorIds.Contains(u.User_ID) || studentIds.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => $"{u.FirstName} {u.LastName}");

            return Ok(reviews.Select(r => new
            {
                r.Tutor_Review_ID,
                r.Tutor_Rating,
                r.Student_ID,
                r.Tutor_ID,
                TutorName   = users.TryGetValue(r.Tutor_ID,   out var tn) ? tn : $"Tutor #{r.Tutor_ID}",
                StudentName = users.TryGetValue(r.Student_ID, out var sn) ? sn : $"Student #{r.Student_ID}"
            }));
        }

        // GET: api/Reviews/tutor/student/{studentId}  — reviews WRITTEN BY a student
        [HttpGet("tutor/student/{studentId}")]
        public async Task<ActionResult> GetTutorReviewsByStudent(int studentId)
        {
            var reviews = await _context.Tutor_Reviews
                .Where(r => r.Student_ID == studentId)
                .ToListAsync();

            var tutorIds = reviews.Select(r => r.Tutor_ID).Distinct().ToList();
            var users = await _context.Users
                .Where(u => tutorIds.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => $"{u.FirstName} {u.LastName}");

            return Ok(reviews.Select(r => new
            {
                r.Tutor_Review_ID,
                r.Tutor_Rating,
                r.Student_ID,
                r.Tutor_ID,
                TutorName = users.TryGetValue(r.Tutor_ID, out var tn) ? tn : $"Tutor #{r.Tutor_ID}"
            }));
        }

        [HttpPost("tutor")]
        public async Task<ActionResult> CreateTutorReview(TutorReviewCreateDto request)
        {
            var review = new Tutor_Review
            {
                Tutor_Rating = request.Rating,
                Student_ID = request.Student_ID,
                Tutor_ID = request.Tutor_ID
            };
            _context.Tutor_Reviews.Add(review);
            await _context.SaveChangesAsync();
            return Ok("Tutor reviewed successfully.");
        }

        // DELETE: api/Reviews/tutor/{id}
        [HttpDelete("tutor/{id}")]
        public async Task<IActionResult> DeleteTutorReview(int id)
        {
            var review = await _context.Tutor_Reviews.FindAsync(id);
            if (review == null) return NotFound("Review not found.");
            _context.Tutor_Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return Ok("Tutor review deleted.");
        }

        // GET: api/Reviews/tutors-for-student/{studentId}
        // Returns distinct tutors assigned to the modules a student is enrolled in
        [HttpGet("tutors-for-student/{studentId}")]
        public async Task<ActionResult> GetTutorsForStudent(int studentId)
        {
            var moduleCodes = await _context.Student_Modules
                .Where(sm => sm.Student_ID == studentId && sm.IsActive)
                .Select(sm => sm.Module_Code)
                .ToListAsync();

            var rows = await _context.Tutor_Modules
                .Where(tm => moduleCodes.Contains(tm.Module_Code) && tm.IsActive)
                .Include(tm => tm.Tutor)
                .Include(tm => tm.Module)
                .ToListAsync();

            var tutors = rows
                .GroupBy(tm => tm.Tutor_ID)
                .Select(g => new
                {
                    tutorId   = g.Key,
                    tutorName = g.First().Tutor != null
                        ? $"{g.First().Tutor!.FirstName} {g.First().Tutor!.LastName}"
                        : "Unknown",
                    modules = g.Select(tm => new
                    {
                        moduleCode = tm.Module_Code,
                        moduleName = tm.Module != null ? tm.Module.Module_Name : tm.Module_Code
                    }).ToList()
                })
                .ToList();

            return Ok(tutors);
        }

        // --- SESSION REVIEWS ---
        // GET: api/Reviews/session/tutor/{tutorId} — reviews for sessions a tutor ran
        [HttpGet("session/tutor/{tutorId}")]
        public async Task<ActionResult> GetTutorSessionReviews(int tutorId)
        {
            var slotIds = await _context.Booking_Slots
                .Where(s => s.Tutor_ID == tutorId)
                .Select(s => s.Booking_Slot_ID)
                .ToListAsync();

            if (!slotIds.Any()) return Ok(new List<object>());

            var reviews = await _context.Session_Reviews
                .Where(r => slotIds.Contains(r.Session_ID))
                .ToListAsync();

            var slots = await _context.Booking_Slots
                .Where(s => slotIds.Contains(s.Booking_Slot_ID))
                .ToListAsync();
            var slotMap = slots.ToDictionary(s => s.Booking_Slot_ID);

            var studentIds = reviews.Select(r => r.Student_ID).Distinct().ToList();
            var students = await _context.Users
                .Where(u => studentIds.Contains(u.User_ID))
                .ToDictionaryAsync(u => u.User_ID, u => $"{u.FirstName} {u.LastName}");

            return Ok(reviews.Select(r =>
            {
                slotMap.TryGetValue(r.Session_ID, out var slot);
                return new
                {
                    r.Session_Review_ID,
                    r.Session_Rating,
                    r.Session_Description,
                    r.Student_ID,
                    r.Session_ID,
                    SlotDate    = slot != null ? slot.Slot_Date.ToString("yyyy-MM-dd") : "",
                    SlotTime    = slot != null ? slot.Slot_Time.ToString("HH:mm") : "",
                    SessionType = slot?.Session_Type ?? "",
                    ModuleCode  = slot?.Module_Code ?? "",
                    StudentName = students.TryGetValue(r.Student_ID, out var sn)
                        ? sn : $"Student #{r.Student_ID}"
                };
            }));
        }

        // GET: api/Reviews/session/student/{studentId}
        [HttpGet("session/student/{studentId}")]
        public async Task<ActionResult> GetStudentSessionReviews(int studentId)
        {
            var reviews = await _context.Session_Reviews
                .Where(r => r.Student_ID == studentId)
                .ToListAsync();

            // Session_ID maps to Booking_Slot_ID — join to get slot details
            var slotIds = reviews.Select(r => r.Session_ID).Distinct().ToList();
            var slots = await _context.Booking_Slots
                .Where(s => slotIds.Contains(s.Booking_Slot_ID))
                .Include(s => s.Tutor)
                .ToListAsync();
            var slotMap = slots.ToDictionary(s => s.Booking_Slot_ID);

            return Ok(reviews.Select(r =>
            {
                slotMap.TryGetValue(r.Session_ID, out var slot);
                return new
                {
                    r.Session_Review_ID,
                    r.Session_Rating,
                    r.Session_Description,
                    r.Student_ID,
                    r.Session_ID,
                    SlotDate    = slot != null ? slot.Slot_Date.ToString("yyyy-MM-dd") : "",
                    SlotTime    = slot != null ? slot.Slot_Time.ToString("HH:mm") : "",
                    SessionType = slot?.Session_Type ?? "",
                    TutorName   = slot?.Tutor != null
                        ? $"{slot.Tutor.FirstName} {slot.Tutor.LastName}"
                        : ""
                };
            }));
        }

        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult> GetSessionReviews(int sessionId)
            => Ok(await _context.Session_Reviews.Where(r => r.Session_ID == sessionId).ToListAsync());

        // DELETE: api/Reviews/session/{id}
        [HttpDelete("session/{id}")]
        public async Task<IActionResult> DeleteSessionReview(int id)
        {
            var review = await _context.Session_Reviews.FindAsync(id);
            if (review == null) return NotFound("Review not found.");
            _context.Session_Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return Ok("Session review deleted.");
        }

        [HttpPost("session")]
        public async Task<ActionResult> CreateSessionReview(SessionReviewCreateDto request)
        {
            var review = new Session_Review
            {
                Session_Rating = request.Rating,
                Session_Description = request.Description,
                Student_ID = request.Student_ID,
                Session_ID = request.Session_ID
            };
            _context.Session_Reviews.Add(review);
            await _context.SaveChangesAsync();
            return Ok("Session reviewed successfully.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ATTENDANCE CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AttendanceController(AppDbContext context) { _context = context; }

        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult> GetAttendance(int sessionId)
        {
            return Ok(await _context.Session_Attendances.Where(a => a.Session_ID == sessionId).ToListAsync());
        }

        [HttpPost]
        public async Task<ActionResult> MarkAttendance(AttendanceCreateDto request)
        {
            var attendance = new Session_Attendance
            {
                Session_ID = request.Session_ID,
                Student_ID = request.Student_ID,
                Attendance_Status_ID = request.Attendance_Status_ID,
                Session_Attendance_Name = $"Session {request.Session_ID} - Student {request.Student_ID}" // Auto-generates the name!
            };
            _context.Session_Attendances.Add(attendance);
            await _context.SaveChangesAsync();
            return Ok("Attendance recorded successfully.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BOOKING SLOTS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class BookingSlotsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public BookingSlotsController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult> GetSlots()
        {
            var slots = await _context.Booking_Slots
                .Include(s => s.Tutor)
                .OrderBy(s => s.Slot_Date).ThenBy(s => s.Slot_Time)
                .ToListAsync();

            return Ok(slots.Select(s => new
            {
                s.Booking_Slot_ID,
                s.Slot_Date,
                s.Slot_Time,
                s.Session_Type,
                s.Location,
                s.Is_Booked,
                s.Tutor_ID,
                s.Max_Capacity,
                s.Module_Code,
                TutorName = s.Tutor != null ? $"{s.Tutor.FirstName} {s.Tutor.LastName}" : "Unknown"
            }));
        }

        // GET: api/BookingSlots/tutor/{tutorId}
        [HttpGet("tutor/{tutorId}")]
        public async Task<ActionResult> GetSlotsByTutor(int tutorId)
        {
            var slots = await _context.Booking_Slots
                .Include(s => s.Tutor)
                .Where(s => s.Tutor_ID == tutorId)
                .OrderBy(s => s.Slot_Date).ThenBy(s => s.Slot_Time)
                .ToListAsync();

            return Ok(slots.Select(s => new
            {
                s.Booking_Slot_ID,
                s.Slot_Date,
                s.Slot_Time,
                s.Session_Type,
                s.Location,
                s.Is_Booked,
                s.Tutor_ID,
                s.Max_Capacity,
                s.Module_Code,
                TutorName = s.Tutor != null ? $"{s.Tutor.FirstName} {s.Tutor.LastName}" : "Unknown"
            }));
        }

        // GET: api/BookingSlots/available
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<Booking_Slot>>> GetAvailableSlots()
        {
            var slots = await _context.Booking_Slots
                .Where(s => !s.Is_Booked)
                .OrderBy(s => s.Slot_Date)
                .ThenBy(s => s.Slot_Time)
                .ToListAsync();
            return Ok(slots);
        }

        // GET: api/BookingSlots/available/module/{moduleCode}
        [HttpGet("available/module/{moduleCode}")]
        public async Task<ActionResult> GetAvailableSlotsByModule(string moduleCode)
        {
            var tutorIds = await _context.Tutor_Modules
                .Where(tm => tm.Module_Code == moduleCode && tm.IsActive)
                .Select(tm => tm.Tutor_ID)
                .ToListAsync();

            var slots = await _context.Booking_Slots
                .Where(s => !s.Is_Booked && tutorIds.Contains(s.Tutor_ID)
                            && (s.Module_Code == moduleCode || s.Module_Code == null))
                .Include(s => s.Tutor)
                .OrderBy(s => s.Slot_Date).ThenBy(s => s.Slot_Time)
                .ToListAsync();

            var result = new List<object>();
            foreach (var s in slots)
            {
                int bookingCount = await _context.Bookings
                    .CountAsync(b => b.Booking_Slot_ID == s.Booking_Slot_ID);
                int capacity = s.Max_Capacity ?? 1;
                int remaining = capacity - bookingCount;
                if (remaining > 0)
                {
                    result.Add(new
                    {
                        s.Booking_Slot_ID,
                        SlotDate        = s.Slot_Date.ToString("yyyy-MM-dd"),
                        SlotTime        = s.Slot_Time.ToString("HH:mm"),
                        s.Session_Type,
                        s.Location,
                        s.Tutor_ID,
                        s.Module_Code,
                        MaxCapacity     = s.Max_Capacity,
                        BookingCount    = bookingCount,
                        Remaining       = remaining,
                        TutorName       = s.Tutor != null
                            ? $"{s.Tutor.FirstName} {s.Tutor.LastName}"
                            : "Unknown"
                    });
                }
            }
            return Ok(result);
        }

        // GET: api/BookingSlots/available/student/{studentId}
        [HttpGet("available/student/{studentId}")]
        public async Task<ActionResult> GetAvailableSlotsForStudent(int studentId)
        {
            var moduleCodes = await _context.Student_Modules
                .Where(sm => sm.Student_ID == studentId && sm.IsActive)
                .Select(sm => sm.Module_Code)
                .ToListAsync();

            if (!moduleCodes.Any()) return Ok(new List<object>());

            var tutorIds = await _context.Tutor_Modules
                .Where(tm => moduleCodes.Contains(tm.Module_Code) && tm.IsActive)
                .Select(tm => tm.Tutor_ID)
                .Distinct()
                .ToListAsync();

            var slots = await _context.Booking_Slots
                .Where(s => !s.Is_Booked && tutorIds.Contains(s.Tutor_ID)
                            && (s.Module_Code == null || moduleCodes.Contains(s.Module_Code)))
                .Include(s => s.Tutor)
                .OrderBy(s => s.Slot_Date).ThenBy(s => s.Slot_Time)
                .ToListAsync();

            var result = new List<object>();
            foreach (var s in slots)
            {
                int bookingCount = await _context.Bookings
                    .CountAsync(b => b.Booking_Slot_ID == s.Booking_Slot_ID);
                int capacity = s.Max_Capacity ?? 1;
                int remaining = capacity - bookingCount;
                if (remaining > 0)
                {
                    result.Add(new
                    {
                        s.Booking_Slot_ID,
                        SlotDate     = s.Slot_Date.ToString("yyyy-MM-dd"),
                        SlotTime     = s.Slot_Time.ToString("HH:mm"),
                        s.Session_Type,
                        s.Location,
                        s.Tutor_ID,
                        s.Module_Code,
                        MaxCapacity  = s.Max_Capacity,
                        Remaining    = remaining,
                        TutorName    = s.Tutor != null
                            ? $"{s.Tutor.FirstName} {s.Tutor.LastName}"
                            : "Unknown"
                    });
                }
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> CreateSlot(BookingSlotCreateDto request)
        {
            var slot = new Booking_Slot
            {
                Slot_Date = request.Slot_Date,
                Slot_Time = request.Slot_Time,
                Session_Type = request.Session_Type,
                Location = request.Location,
                Tutor_ID = request.Tutor_ID,
                Is_Booked = false,
                Max_Capacity = request.Max_Capacity,
                Module_Code = request.Module_Code
            };
            _context.Booking_Slots.Add(slot);
            await _context.SaveChangesAsync();
            return Ok("Booking slot created.");
        }

        // PUT: api/BookingSlots/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSlot(int id, BookingSlotCreateDto request)
        {
            var slot = await _context.Booking_Slots.FindAsync(id);
            if (slot == null) return NotFound("Slot not found.");
            if (slot.Is_Booked) return BadRequest("Cannot edit a slot that is already booked.");

            slot.Slot_Date    = request.Slot_Date;
            slot.Slot_Time    = request.Slot_Time;
            slot.Session_Type = request.Session_Type;
            slot.Location     = request.Location;
            slot.Max_Capacity = request.Max_Capacity;
            slot.Module_Code  = request.Module_Code;

            await _context.SaveChangesAsync();
            return Ok("Booking slot updated.");
        }

        // DELETE: api/BookingSlots/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSlot(int id)
        {
            var slot = await _context.Booking_Slots.FindAsync(id);
            if (slot == null) return NotFound("Slot not found.");
            if (slot.Is_Booked) return BadRequest("Cannot delete a slot that is already booked.");
            _context.Booking_Slots.Remove(slot);
            await _context.SaveChangesAsync();
            return Ok("Booking slot deleted.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BOOKINGS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        public BookingsController(AppDbContext context, AuditService audit) { _context = context; _audit = audit; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings() => await _context.Bookings.Include(b => b.Booking_Slot).ToListAsync();

        // GET: api/Bookings/student/{studentId}
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult> GetStudentBookings(int studentId)
        {
            var bookings = await _context.Bookings
                .Where(b => b.Student_ID == studentId)
                .Include(b => b.Booking_Slot)
                    .ThenInclude(s => s!.Tutor)
                .OrderBy(b => b.Booking_Slot!.Slot_Date)
                .ThenBy(b => b.Booking_Slot!.Slot_Time)
                .ToListAsync();

            return Ok(bookings.Select(b => new
            {
                b.Booking_ID,
                b.Booking_Slot_ID,
                SlotDate    = b.Booking_Slot != null ? b.Booking_Slot.Slot_Date.ToString("yyyy-MM-dd") : "",
                SlotTime    = b.Booking_Slot != null ? b.Booking_Slot.Slot_Time.ToString("HH:mm") : "",
                SessionType = b.Booking_Slot?.Session_Type ?? "",
                Location    = b.Booking_Slot?.Location ?? "",
                ModuleCode  = b.Booking_Slot?.Module_Code ?? "",
                TutorName   = b.Booking_Slot?.Tutor != null
                    ? $"{b.Booking_Slot.Tutor.FirstName} {b.Booking_Slot.Tutor.LastName}"
                    : "Unknown"
            }));
        }

        // DELETE: api/Bookings/{id}  — student cancels their own booking
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound("Booking not found.");

            var slot = await _context.Booking_Slots.FindAsync(booking.Booking_Slot_ID);

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            // Re-open the slot if it was marked as full
            if (slot != null && slot.Is_Booked)
            {
                int remaining = await _context.Bookings
                    .CountAsync(b => b.Booking_Slot_ID == slot.Booking_Slot_ID);
                int capacity = slot.Max_Capacity ?? 1;
                if (remaining < capacity)
                {
                    slot.Is_Booked = false;
                    await _context.SaveChangesAsync();
                }
            }

            await _audit.LogAsync(booking.Student_ID, "Booking Cancelled", $"Booking_ID: {id}");
            return Ok("Booking cancelled.");
        }

        [HttpPost]
        public async Task<ActionResult> BookSession(BookingCreateDto request)
        {
            var slot = await _context.Booking_Slots.FindAsync(request.Booking_Slot_ID);
            if (slot == null || slot.Is_Booked) return BadRequest("Slot is unavailable.");

            // Prevent duplicate bookings by the same student
            var alreadyBooked = await _context.Bookings
                .AnyAsync(b => b.Student_ID == request.Student_ID && b.Booking_Slot_ID == request.Booking_Slot_ID);
            if (alreadyBooked) return BadRequest("You have already booked this session.");

            var booking = new Booking
            {
                Student_ID = request.Student_ID,
                Booking_Slot_ID = request.Booking_Slot_ID
            };
            _context.Bookings.Add(booking);

            // For group sessions: mark slot as booked only when capacity is reached
            int capacity = slot.Max_Capacity ?? 1;
            if (capacity <= 1)
            {
                slot.Is_Booked = true;
            }
            else
            {
                int currentCount = await _context.Bookings
                    .CountAsync(b => b.Booking_Slot_ID == request.Booking_Slot_ID);
                if (currentCount + 1 >= capacity)
                    slot.Is_Booked = true;
            }

            await _context.SaveChangesAsync();
            await _audit.LogAsync(request.Student_ID, "Session Booked", $"Booking_Slot_ID: {request.Booking_Slot_ID}");
            return Ok("Session successfully booked.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PAYMENTS CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Payments/student/5 (Use Case 5.10 View Payment)
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<PaymentReturnDto>>> GetStudentPayments(int studentId)
        {
            var payments = await _context.Payments
                .Where(p => p.Student_ID == studentId)
                .Select(p => new PaymentReturnDto
                {
                    Payment_ID = p.Payment_ID,
                    Amount = p.Amount,
                    Payment_Date = p.Payment_Date,
                    Payment_Status = p.Payment_Status,
                    Bank = p.Bank,
                    Payment_Reference = p.Payment_Reference,
                    Module_Code = p.Module_Code
                })
                .ToListAsync();

            return Ok(payments);
        }

        // POST: api/Payments (Use Case 5.9 Make Payment via EFT/Ozow)
        [HttpPost]
        public async Task<ActionResult<Payment>> MakePayment(PaymentCreateDto request)
        {
            var newPayment = new Payment
            {
                Amount = request.Amount,
                Payment_Date = DateTime.UtcNow,
                Payment_Status = "Pending", // Admin must verify the EFT clears!
                Account_Name = request.Account_Name,
                Account_Number = request.Account_Number,
                Branch_Code = request.Branch_Code,
                Bank = request.Bank,
                Payment_Reference = request.Payment_Reference,
                Student_ID = request.Student_ID,
                Module_Code = request.Module_Code
            };

            _context.Payments.Add(newPayment);

            // BONUS: Generate an automatic notification for the student!
            var notification = new Notification
            {
                Message = $"Your payment of R{request.Amount} for {request.Module_Code} has been logged and is Pending Verification.",
                Date_Sent = DateTime.UtcNow,
                User_ID = request.Student_ID
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return Ok("Payment details submitted successfully. Status is Pending.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ENROLLMENT CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class EnrollmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        public EnrollmentController(AppDbContext context, AuditService audit) { _context = context; _audit = audit; }

        // POST: api/enrollment/enroll
        /// <summary>
        /// Enroll a student in a module
        /// </summary>
        [HttpPost("enroll")]
        public async Task<IActionResult> EnrollInModule([FromBody] EnrollmentCreateDto request)
        {
            if (string.IsNullOrEmpty(request.Module_Code))
                return BadRequest("Module code is required.");

            // Check if student exists
            var student = await _context.Users.FindAsync(request.Student_ID);
            if (student == null)
                return NotFound("Student not found.");

            // Check if module exists
            var module = await _context.Modules.FindAsync(request.Module_Code);
            if (module == null)
                return NotFound("Module not found.");

            // Check if already enrolled
            var existingEnrollment = await _context.Student_Modules
                .FirstOrDefaultAsync(sm => sm.Student_ID == request.Student_ID
                    && sm.Module_Code == request.Module_Code
                    && sm.IsActive);

            if (existingEnrollment != null)
                return BadRequest("Student is already enrolled in this module.");

            // Create enrollment
            var enrollment = new Student_Module
            {
                Student_ID = request.Student_ID,
                Module_Code = request.Module_Code,
                Enrollment_Date = DateTime.UtcNow,
                IsActive = true
            };

            _context.Student_Modules.Add(enrollment);

            // Send notification to admin
            var notification = new Notification
            {
                User_ID = 1, // Admin user (assuming ID 1 is admin)
                Message = $"Student {student.FirstName} {student.LastName} enrolled in module {request.Module_Code}",
                Date_Sent = DateTime.UtcNow,
                Is_Read = false
            };
            _context.Notifications.Add(notification);

            try
            {
                await _context.SaveChangesAsync();
                await _audit.LogAsync(request.Student_ID, "Module Enrolled", $"Module_Code: {request.Module_Code}");
                return Ok(new { message = "Successfully enrolled in module.", enrollmentId = enrollment.Enrollment_ID });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Failed to enroll in module.");
            }
        }

        // DELETE: api/enrollment/unenroll/{moduleCode}
        /// <summary>
        /// Unenroll a student from a module
        /// </summary>
        [HttpDelete("unenroll/{moduleCode}")]
        public async Task<IActionResult> UnenrollFromModule(int studentId, string moduleCode, [FromBody] EnrollmentUnenrollDto? request)
        {
            var enrollment = await _context.Student_Modules
                .FirstOrDefaultAsync(sm => sm.Student_ID == studentId
                    && sm.Module_Code == moduleCode
                    && sm.IsActive);

            if (enrollment == null)
                return NotFound("Enrollment not found.");

            // Mark enrollment as inactive
            enrollment.IsActive = false;

            // Record unenrollment details in the separate Student_Unenrollments table
            _context.Student_Unenrollments.Add(new Student_Unenrollment
            {
                Student_ID      = studentId,
                Module_Code     = moduleCode,
                Enrollment_ID   = enrollment.Enrollment_ID,
                Unenroll_Date   = DateTime.UtcNow,
                Unenroll_Reason = request?.Unenroll_Reason
            });

            try
            {
                await _context.SaveChangesAsync();
                await _audit.LogAsync(studentId, "Module Unenrolled", $"Module_Code: {moduleCode}");
                return Ok("Successfully unenrolled from module.");
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Failed to unenroll from module.");
            }
        }

        // GET: api/enrollment/student/{studentId}
        /// <summary>
        /// Get all enrolled modules for a student
        /// </summary>
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<List<EnrollmentViewDto>>> GetStudentModules(int studentId)
        {
            var enrollments = await _context.Student_Modules
                .Where(sm => sm.Student_ID == studentId && sm.IsActive)
                .Include(sm => sm.Module)
                .Select(sm => new EnrollmentViewDto
                {
                    Enrollment_ID = sm.Enrollment_ID,
                    Student_ID = sm.Student_ID,
                    Module_Code = sm.Module_Code,
                    Module_Name = sm.Module != null ? sm.Module.Module_Name : string.Empty,
                    Enrollment_Date = sm.Enrollment_Date,
                    IsActive = sm.IsActive
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        // GET: api/enrollment/module/{moduleCode}
        /// <summary>
        /// Get all enrolled students for a module
        /// </summary>
        [HttpGet("module/{moduleCode}")]
        [Authorize(Roles = "Admin,Tutor")]
        public async Task<ActionResult<List<EnrollmentViewDto>>> GetModuleStudents(string moduleCode)
        {
            var enrollments = await _context.Student_Modules
                .Where(sm => sm.Module_Code == moduleCode && sm.IsActive)
                .Include(sm => sm.Student)
                .Select(sm => new EnrollmentViewDto
                {
                    Enrollment_ID = sm.Enrollment_ID,
                    Student_ID = sm.Student_ID,
                    Module_Code = sm.Module_Code,
                    Module_Name = moduleCode,
                    Enrollment_Date = sm.Enrollment_Date,
                    IsActive = sm.IsActive
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        // GET: api/enrollment/check/{studentId}/{moduleCode}
        /// <summary>
        /// Check if a student is enrolled in a module
        /// </summary>
        [HttpGet("check/{studentId}/{moduleCode}")]
        public async Task<IActionResult> CheckEnrollment(int studentId, string moduleCode)
        {
            var enrollment = await _context.Student_Modules
                .FirstOrDefaultAsync(sm => sm.Student_ID == studentId
                    && sm.Module_Code == moduleCode
                    && sm.IsActive);

            return Ok(new { isEnrolled = enrollment != null });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TUTOR MODULE CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class TutorModuleController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TutorModuleController(AppDbContext context) { _context = context; }

        // GET: api/TutorModule/tutor/{tutorId}
        [HttpGet("tutor/{tutorId}")]
        public async Task<ActionResult<List<TutorModuleViewDto>>> GetModulesForTutor(int tutorId)
        {
            var assignments = await _context.Tutor_Modules
                .Where(tm => tm.Tutor_ID == tutorId && tm.IsActive)
                .Include(tm => tm.Module)
                .Select(tm => new TutorModuleViewDto
                {
                    Tutor_Module_ID = tm.Tutor_Module_ID,
                    Tutor_ID = tm.Tutor_ID,
                    Tutor_Name = string.Empty,
                    Module_Code = tm.Module_Code,
                    Module_Name = tm.Module != null ? tm.Module.Module_Name : string.Empty,
                    Assigned_Date = tm.Assigned_Date,
                    IsActive = tm.IsActive
                })
                .ToListAsync();

            return Ok(assignments);
        }

        // GET: api/TutorModule/module/{moduleCode}
        [HttpGet("module/{moduleCode}")]
        [Authorize(Roles = "Admin,Tutor")]
        public async Task<ActionResult<List<TutorModuleViewDto>>> GetTutorsForModule(string moduleCode)
        {
            var assignments = await _context.Tutor_Modules
                .Where(tm => tm.Module_Code == moduleCode && tm.IsActive)
                .Include(tm => tm.Tutor)
                .Include(tm => tm.Module)
                .Select(tm => new TutorModuleViewDto
                {
                    Tutor_Module_ID = tm.Tutor_Module_ID,
                    Tutor_ID = tm.Tutor_ID,
                    Tutor_Name = tm.Tutor != null
                        ? tm.Tutor.FirstName + " " + tm.Tutor.LastName
                        : string.Empty,
                    Module_Code = tm.Module_Code,
                    Module_Name = tm.Module != null ? tm.Module.Module_Name : string.Empty,
                    Assigned_Date = tm.Assigned_Date,
                    IsActive = tm.IsActive
                })
                .ToListAsync();

            return Ok(assignments);
        }

        // GET: api/TutorModule/check/{tutorId}/{moduleCode}
        [HttpGet("check/{tutorId}/{moduleCode}")]
        public async Task<IActionResult> CheckAssignment(int tutorId, string moduleCode)
        {
            var assignment = await _context.Tutor_Modules
                .FirstOrDefaultAsync(tm => tm.Tutor_ID == tutorId
                    && tm.Module_Code == moduleCode
                    && tm.IsActive);

            return Ok(new { isAssigned = assignment != null });
        }

        // POST: api/TutorModule/assign
        [HttpPost("assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignTutorToModule([FromBody] TutorModuleAssignDto request)
        {
            var tutor = await _context.Users.FindAsync(request.Tutor_ID);
            if (tutor == null)
                return NotFound("Tutor not found.");

            var module = await _context.Modules.FindAsync(request.Module_Code);
            if (module == null)
                return NotFound("Module not found.");

            var existing = await _context.Tutor_Modules
                .FirstOrDefaultAsync(tm => tm.Tutor_ID == request.Tutor_ID
                    && tm.Module_Code == request.Module_Code
                    && tm.IsActive);

            if (existing != null)
                return BadRequest("Tutor is already assigned to this module.");

            var assignment = new Tutor_Module
            {
                Tutor_ID = request.Tutor_ID,
                Module_Code = request.Module_Code,
                Assigned_Date = DateTime.UtcNow,
                IsActive = true
            };

            _context.Tutor_Modules.Add(assignment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tutor assigned to module.", assignmentId = assignment.Tutor_Module_ID });
        }

        // DELETE: api/TutorModule/unassign/{tutorId}/{moduleCode}
        [HttpDelete("unassign/{tutorId}/{moduleCode}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnassignTutorFromModule(int tutorId, string moduleCode)
        {
            var assignment = await _context.Tutor_Modules
                .FirstOrDefaultAsync(tm => tm.Tutor_ID == tutorId
                    && tm.Module_Code == moduleCode
                    && tm.IsActive);

            if (assignment == null)
                return NotFound("Assignment not found.");

            assignment.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok("Tutor unassigned from module.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ADMIN CONTENT ITERATION 5 CONTROLLER
    // FAQ Categories (UC 4.16-4.19), Help Resources (UC 4.28-4.31),
    // Media Content (UC 4.20-4.23), Testimonial Categories (UC 4.32-4.35)
    // ─────────────────────────────────────────────────────────────────────────

    [Route("api/AdminContent")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class AdminContentIteration5Controller : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryService _cloudinary;
        public AdminContentIteration5Controller(AppDbContext context, CloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        // ─── FAQ CATEGORIES ────────────────────────────────────────────────────

        [HttpGet("faq-categories")]
        [AllowAnonymous]
        public async Task<ActionResult> GetFAQCategories()
        {
            var cats = await _context.FAQ_Categories.ToListAsync();
            return Ok(cats.Select(c => new {
                faq_Category_ID = c.FAQ_Category_ID,
                category_Name   = c.Category_Name
            }));
        }

        [HttpPost("faq-categories")]
        public async Task<ActionResult> CreateFAQCategory(FAQCategoryCreateDto request)
        {
            var cat = new FAQ_Category { Category_Name = request.Category_Name };
            _context.FAQ_Categories.Add(cat);
            await _context.SaveChangesAsync();
            return Ok("FAQ category created.");
        }

        [HttpPut("faq-categories/{id}")]
        public async Task<IActionResult> UpdateFAQCategory(int id, FAQCategoryCreateDto request)
        {
            var cat = await _context.FAQ_Categories.FindAsync(id);
            if (cat == null) return NotFound();
            cat.Category_Name = request.Category_Name;
            await _context.SaveChangesAsync();
            return Ok("FAQ category updated.");
        }

        [HttpDelete("faq-categories/{id}")]
        public async Task<IActionResult> DeleteFAQCategory(int id)
        {
            var cat = await _context.FAQ_Categories.FindAsync(id);
            if (cat == null) return NotFound();
            _context.FAQ_Categories.Remove(cat);
            await _context.SaveChangesAsync();
            return Ok("FAQ category deleted.");
        }

        // ─── MEDIA CONTENT ─────────────────────────────────────────────────────

        [HttpGet("media")]
        [AllowAnonymous]
        public async Task<ActionResult> GetMedia()
            => Ok(await _context.Media_Contents.ToListAsync());

        [HttpPost("media/upload")]
        public async Task<ActionResult> UploadMediaFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file provided.");
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".mp4", ".webm" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return BadRequest("File type not allowed.");

            var mimeType = file.ContentType;
            var fileName = $"{Guid.NewGuid()}{ext}";

            try
            {
                using var stream = file.OpenReadStream();
                var isVideo = new[] { ".mp4", ".webm", ".ogg" }.Contains(ext);
                var publicUrl = isVideo
                    ? await _cloudinary.UploadVideoAsync(stream, fileName)
                    : await _cloudinary.UploadImageAsync(stream, fileName);
                return Ok(publicUrl);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Upload failed: {ex.Message}");
            }
        }

        [HttpPost("media")]
        public async Task<ActionResult> CreateMedia(MediaCreateDto request)
        {
            var media = new Media_Content
            {
                Media_Name = request.Media_Name,
                Media_Address = request.Media_Address
            };
            _context.Media_Contents.Add(media);
            await _context.SaveChangesAsync();
            return Ok("Media content added.");
        }

        [HttpPut("media/{id}")]
        public async Task<IActionResult> UpdateMedia(int id, MediaCreateDto request)
        {
            var media = await _context.Media_Contents.FindAsync(id);
            if (media == null) return NotFound();
            media.Media_Name = request.Media_Name;
            media.Media_Address = request.Media_Address;
            await _context.SaveChangesAsync();
            return Ok("Media content updated.");
        }

        [HttpDelete("media/{id}")]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var media = await _context.Media_Contents.FindAsync(id);
            if (media == null) return NotFound();
            _context.Media_Contents.Remove(media);
            await _context.SaveChangesAsync();
            return Ok("Media content deleted.");
        }

        // ─── HELP RESOURCES ────────────────────────────────────────────────────

        [HttpGet("help-resources")]
        [AllowAnonymous]
        public async Task<ActionResult> GetHelpResources()
            => Ok(await _context.Help_Resources.ToListAsync());

        [HttpPost("help-resources")]
        public async Task<ActionResult> CreateHelpResource(HelpResourceCreateDto request)
        {
            var resource = new Help_Resource { Video_Title = request.Video_Title, Video_URL = request.Video_URL };
            _context.Help_Resources.Add(resource);
            await _context.SaveChangesAsync();
            return Ok("Help resource added.");
        }

        [HttpPut("help-resources/{id}")]
        public async Task<IActionResult> UpdateHelpResource(int id, HelpResourceCreateDto request)
        {
            var resource = await _context.Help_Resources.FindAsync(id);
            if (resource == null) return NotFound();
            resource.Video_Title = request.Video_Title;
            resource.Video_URL = request.Video_URL;
            await _context.SaveChangesAsync();
            return Ok("Help resource updated.");
        }

        [HttpDelete("help-resources/{id}")]
        public async Task<IActionResult> DeleteHelpResource(int id)
        {
            var resource = await _context.Help_Resources.FindAsync(id);
            if (resource == null) return NotFound();
            _context.Help_Resources.Remove(resource);
            await _context.SaveChangesAsync();
            return Ok("Help resource deleted.");
        }

        // ─── TESTIMONIAL CATEGORIES ────────────────────────────────────────────

        [HttpGet("testimonial-categories")]
        [AllowAnonymous]
        public async Task<ActionResult> GetTestimonialCategories()
            => Ok(await _context.Testimonial_Categories.ToListAsync());

        [HttpPost("testimonial-categories")]
        public async Task<ActionResult> CreateTestimonialCategory(TestimonialCategoryCreateDto request)
        {
            var cat = new Testimonial_Category { Test_Category_Name = request.Test_Category_Name };
            _context.Testimonial_Categories.Add(cat);
            await _context.SaveChangesAsync();
            return Ok("Testimonial category created.");
        }

        [HttpPut("testimonial-categories/{id}")]
        public async Task<IActionResult> UpdateTestimonialCategory(int id, TestimonialCategoryCreateDto request)
        {
            var cat = await _context.Testimonial_Categories.FindAsync(id);
            if (cat == null) return NotFound();
            cat.Test_Category_Name = request.Test_Category_Name;
            await _context.SaveChangesAsync();
            return Ok("Testimonial category updated.");
        }

        [HttpDelete("testimonial-categories/{id}")]
        public async Task<IActionResult> DeleteTestimonialCategory(int id)
        {
            var cat = await _context.Testimonial_Categories.FindAsync(id);
            if (cat == null) return NotFound();
            _context.Testimonial_Categories.Remove(cat);
            await _context.SaveChangesAsync();
            return Ok("Testimonial category deleted.");
        }
    }
}
