using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentEnrollmentAndFileUploadSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Student_Modules table if it does not exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Student_Modules]') AND type = N'U')
                BEGIN
                    CREATE TABLE [Student_Modules] (
                        [Enrollment_ID] int NOT NULL IDENTITY,
                        [Student_ID] int NOT NULL,
                        [Module_Code] nvarchar(450) NOT NULL,
                        [Enrollment_Date] datetime2 NOT NULL,
                        [IsActive] bit NOT NULL,
                        [Unenroll_Reason] nvarchar(max) NULL,
                        [Unenroll_Date] datetime2 NULL,
                        CONSTRAINT [PK_Student_Modules] PRIMARY KEY ([Enrollment_ID]),
                        CONSTRAINT [FK_Student_Modules_Modules_Module_Code] FOREIGN KEY ([Module_Code]) REFERENCES [Modules] ([Module_Code]) ON DELETE CASCADE,
                        CONSTRAINT [FK_Student_Modules_Users_Student_ID] FOREIGN KEY ([Student_ID]) REFERENCES [Users] ([User_ID]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_Student_Modules_Module_Code] ON [Student_Modules] ([Module_Code]);
                    CREATE INDEX [IX_Student_Modules_Student_ID] ON [Student_Modules] ([Student_ID]);
                    CREATE INDEX [IX_Student_Modules_Active_Enrollments] ON [Student_Modules] ([Student_ID], [Module_Code], [IsActive]);
                END
            ");

            // Rename Assignment_Submission_ID to Submission_ID if needed
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'Assignment_Submission_ID')
                    EXEC sp_rename '[Assignment_Submissions].[Assignment_Submission_ID]', 'Submission_ID', 'COLUMN';
            ");

            // Rename Assignment_grade_amount to Grade if needed
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'Assignment_grade_amount')
                    EXEC sp_rename '[Assignment_Submissions].[Assignment_grade_amount]', 'Grade', 'COLUMN';
            ");

            // Rename Assignment_Feedback to Feedback if needed
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'Assignment_Feedback')
                    EXEC sp_rename '[Assignment_Submissions].[Assignment_Feedback]', 'Feedback', 'COLUMN';
            ");

            // Add Submission_Date column if it does not exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'Submission_Date')
                    ALTER TABLE [Assignment_Submissions] ADD [Submission_Date] datetime2 NOT NULL DEFAULT '2026-04-17T00:00:00.000';
            ");

            // Add File_Name column if it does not exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'File_Name')
                    ALTER TABLE [Assignment_Submissions] ADD [File_Name] nvarchar(max) NOT NULL DEFAULT '';
            ");

            // Add File_Path column if it does not exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'File_Path')
                    ALTER TABLE [Assignment_Submissions] ADD [File_Path] nvarchar(max) NOT NULL DEFAULT '';
            ");

            // Add File_Type column if it does not exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'File_Type')
                    ALTER TABLE [Assignment_Submissions] ADD [File_Type] nvarchar(max) NOT NULL DEFAULT '';
            ");

            // Add File_Size column if it does not exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'File_Size')
                    ALTER TABLE [Assignment_Submissions] ADD [File_Size] bigint NOT NULL DEFAULT 0;
            ");

            // Add Feedback_Date column if it does not exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'Feedback_Date')
                    ALTER TABLE [Assignment_Submissions] ADD [Feedback_Date] datetime2 NULL;
            ");

            // Add Graded_By column if it does not exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'Graded_By')
                    ALTER TABLE [Assignment_Submissions] ADD [Graded_By] int NULL;
            ");

            // Add Feedback column if it does not exist (was not renamed — Grade column was)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'Feedback')
                    ALTER TABLE [Assignment_Submissions] ADD [Feedback] nvarchar(max) NULL;
            ");

            // Add Grade column (decimal) if it does not exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'Grade')
                    ALTER TABLE [Assignment_Submissions] ADD [Grade] decimal(5,2) NULL;
            ");

            // Add Student_ID column if it does not exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Assignment_Submissions]') AND name = 'Student_ID')
                    ALTER TABLE [Assignment_Submissions] ADD [Student_ID] int NOT NULL DEFAULT 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Student_Modules]') AND type = N'U')
                    DROP TABLE [Student_Modules];
            ");
        }
    }
}
