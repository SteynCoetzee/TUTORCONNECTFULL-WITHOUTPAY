using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TutorConnect.API.Data;

#nullable disable

namespace TutorConnect.API.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260707000002_AddPayFastEnrollmentItems")]
    public partial class AddPayFastEnrollmentItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns
                               WHERE object_id = OBJECT_ID(N'[Payments]') AND name = 'Enrollment_Items_Json')
                BEGIN
                    ALTER TABLE [Payments] ADD [Enrollment_Items_Json] nvarchar(max) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns
                           WHERE object_id = OBJECT_ID(N'[Payments]') AND name = 'Enrollment_Items_Json')
                BEGIN
                    ALTER TABLE [Payments] DROP COLUMN [Enrollment_Items_Json];
                END
            ");
        }
    }
}
