using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToBookingSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Drop old shadow FK constraints if they still exist ────────────────
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Bookings_Booking_Slots_Booking_Slot_ID1') ALTER TABLE [Bookings] DROP CONSTRAINT [FK_Bookings_Booking_Slots_Booking_Slot_ID1]");
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Log_Hours_Users_TutorUser_ID') ALTER TABLE [Log_Hours] DROP CONSTRAINT [FK_Log_Hours_Users_TutorUser_ID]");
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Users_User_Roles_User_Role_ID1') ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_User_Roles_User_Role_ID1]");

            // ── Drop old shadow indexes if they still exist ───────────────────────
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Users_User_Role_ID1' AND object_id=OBJECT_ID('Users')) DROP INDEX [IX_Users_User_Role_ID1] ON [Users]");
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Log_Hours_TutorUser_ID' AND object_id=OBJECT_ID('Log_Hours')) DROP INDEX [IX_Log_Hours_TutorUser_ID] ON [Log_Hours]");
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Bookings_Booking_Slot_ID1' AND object_id=OBJECT_ID('Bookings')) DROP INDEX [IX_Bookings_Booking_Slot_ID1] ON [Bookings]");

            // ── Drop old shadow columns if they still exist ───────────────────────
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Users') AND name='User_Role_ID1') ALTER TABLE [Users] DROP COLUMN [User_Role_ID1]");
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Log_Hours') AND name='TutorUser_ID') ALTER TABLE [Log_Hours] DROP COLUMN [TutorUser_ID]");
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Bookings') AND name='Booking_Slot_ID1') ALTER TABLE [Bookings] DROP COLUMN [Booking_Slot_ID1]");

            // ── Add new columns if they don't exist yet ───────────────────────────
            migrationBuilder.Sql("IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Help_Resources') AND name='Video_Title') ALTER TABLE [Help_Resources] ADD [Video_Title] nvarchar(max) NOT NULL DEFAULT ''");
            migrationBuilder.Sql("IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Booking_Slots') AND name='Location') ALTER TABLE [Booking_Slots] ADD [Location] nvarchar(max) NOT NULL DEFAULT ''");
            migrationBuilder.Sql("IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Booking_Slots') AND name='Max_Capacity') ALTER TABLE [Booking_Slots] ADD [Max_Capacity] int NULL");

            // ── Create indexes if they don't exist yet ────────────────────────────
            migrationBuilder.Sql("IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Users_User_Role_ID' AND object_id=OBJECT_ID('Users')) CREATE INDEX [IX_Users_User_Role_ID] ON [Users]([User_Role_ID])");
            migrationBuilder.Sql("IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Log_Hours_Tutor_ID' AND object_id=OBJECT_ID('Log_Hours')) CREATE INDEX [IX_Log_Hours_Tutor_ID] ON [Log_Hours]([Tutor_ID])");
            migrationBuilder.Sql("IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Bookings_Booking_Slot_ID' AND object_id=OBJECT_ID('Bookings')) CREATE INDEX [IX_Bookings_Booking_Slot_ID] ON [Bookings]([Booking_Slot_ID])");
            migrationBuilder.Sql("IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Booking_Slots_Tutor_ID' AND object_id=OBJECT_ID('Booking_Slots')) CREATE INDEX [IX_Booking_Slots_Tutor_ID] ON [Booking_Slots]([Tutor_ID])");

            // ── Add FK constraints if they don't exist yet ────────────────────────
            migrationBuilder.Sql("IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Booking_Slots_Users_Tutor_ID') ALTER TABLE [Booking_Slots] ADD CONSTRAINT [FK_Booking_Slots_Users_Tutor_ID] FOREIGN KEY ([Tutor_ID]) REFERENCES [Users]([User_ID]) ON DELETE CASCADE");
            migrationBuilder.Sql("IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Bookings_Booking_Slots_Booking_Slot_ID') ALTER TABLE [Bookings] ADD CONSTRAINT [FK_Bookings_Booking_Slots_Booking_Slot_ID] FOREIGN KEY ([Booking_Slot_ID]) REFERENCES [Booking_Slots]([Booking_Slot_ID]) ON DELETE CASCADE");
            migrationBuilder.Sql("IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Log_Hours_Users_Tutor_ID') ALTER TABLE [Log_Hours] ADD CONSTRAINT [FK_Log_Hours_Users_Tutor_ID] FOREIGN KEY ([Tutor_ID]) REFERENCES [Users]([User_ID]) ON DELETE CASCADE");
            migrationBuilder.Sql("IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Users_User_Roles_User_Role_ID') ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_User_Roles_User_Role_ID] FOREIGN KEY ([User_Role_ID]) REFERENCES [User_Roles]([User_Role_ID]) ON DELETE CASCADE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Booking_Slots') AND name='Location') ALTER TABLE [Booking_Slots] DROP COLUMN [Location]");
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Booking_Slots') AND name='Max_Capacity') ALTER TABLE [Booking_Slots] DROP COLUMN [Max_Capacity]");
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Help_Resources') AND name='Video_Title') ALTER TABLE [Help_Resources] DROP COLUMN [Video_Title]");
        }
    }
}
