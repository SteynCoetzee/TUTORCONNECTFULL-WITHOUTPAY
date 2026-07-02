using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Student_Profiles]') AND type = N'U')
                BEGIN
                    CREATE TABLE [Student_Profiles] (
                        [Student_ID] int NOT NULL IDENTITY,
                        [User_ID] int NOT NULL,
                        [Student_Number] nvarchar(max) NULL,
                        [Faculty] nvarchar(max) NULL,
                        [Year_Of_Study] int NULL,
                        [Degree_Program] nvarchar(max) NULL,
                        CONSTRAINT [PK_Student_Profiles] PRIMARY KEY ([Student_ID]),
                        CONSTRAINT [FK_Student_Profiles_Users_User_ID] FOREIGN KEY ([User_ID]) REFERENCES [Users] ([User_ID]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_Student_Profiles_User_ID] ON [Student_Profiles] ([User_ID]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Tutor_Profiles]') AND type = N'U')
                BEGIN
                    CREATE TABLE [Tutor_Profiles] (
                        [Tutor_ID] int NOT NULL IDENTITY,
                        [User_ID] int NOT NULL,
                        [Qualifications] nvarchar(max) NULL,
                        [Specialization] nvarchar(max) NULL,
                        [Years_Of_Experience] int NULL,
                        CONSTRAINT [PK_Tutor_Profiles] PRIMARY KEY ([Tutor_ID]),
                        CONSTRAINT [FK_Tutor_Profiles_Users_User_ID] FOREIGN KEY ([User_ID]) REFERENCES [Users] ([User_ID]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_Tutor_Profiles_User_ID] ON [Tutor_Profiles] ([User_ID]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Admin_Profiles]') AND type = N'U')
                BEGIN
                    CREATE TABLE [Admin_Profiles] (
                        [Admin_ID] int NOT NULL IDENTITY,
                        [User_ID] int NOT NULL,
                        [Job_Title] nvarchar(max) NULL,
                        CONSTRAINT [PK_Admin_Profiles] PRIMARY KEY ([Admin_ID]),
                        CONSTRAINT [FK_Admin_Profiles_Users_User_ID] FOREIGN KEY ([User_ID]) REFERENCES [Users] ([User_ID]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_Admin_Profiles_User_ID] ON [Admin_Profiles] ([User_ID]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Admin_Profiles]') AND type = N'U') DROP TABLE [Admin_Profiles];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Tutor_Profiles]') AND type = N'U') DROP TABLE [Tutor_Profiles];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Student_Profiles]') AND type = N'U') DROP TABLE [Student_Profiles];");
        }
    }
}
