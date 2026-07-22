using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentItemsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IF NOT EXISTS guard so this is safe to run on databases that already
            // have this column from the previous hand-written migration.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Payments]')
                    AND name = 'Enrollment_Items_Json'
                )
                BEGIN
                    ALTER TABLE [Payments] ADD [Enrollment_Items_Json] nvarchar(max) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Payments]')
                    AND name = 'Enrollment_Items_Json'
                )
                BEGIN
                    ALTER TABLE [Payments] DROP COLUMN [Enrollment_Items_Json];
                END
            ");
        }
    }
}
