using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TutorConnect.API.Data;

#nullable disable

namespace TutorConnect.API.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260705000001_AddStudentUnenrollments")]
    public partial class AddStudentUnenrollments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Student_Unenrollments]') AND type = N'U')
                BEGIN
                    CREATE TABLE [Student_Unenrollments] (
                        [Unenrollment_ID] int NOT NULL IDENTITY,
                        [Student_ID] int NOT NULL,
                        [Module_Code] nvarchar(450) NOT NULL,
                        [Enrollment_ID] int NOT NULL,
                        [Unenroll_Date] datetime2 NOT NULL,
                        [Unenroll_Reason] nvarchar(max) NULL,
                        CONSTRAINT [PK_Student_Unenrollments] PRIMARY KEY ([Unenrollment_ID]),
                        CONSTRAINT [FK_Student_Unenrollments_Users_Student_ID] FOREIGN KEY ([Student_ID]) REFERENCES [Users] ([User_ID]) ON DELETE CASCADE,
                        CONSTRAINT [FK_Student_Unenrollments_Modules_Module_Code] FOREIGN KEY ([Module_Code]) REFERENCES [Modules] ([Module_Code]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_Student_Unenrollments_Student_ID] ON [Student_Unenrollments] ([Student_ID]);
                    CREATE INDEX [IX_Student_Unenrollments_Module_Code] ON [Student_Unenrollments] ([Module_Code]);
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Student_Unenrollments]') AND type = N'U')
                    DROP TABLE [Student_Unenrollments];
            ");
        }
    }
}
