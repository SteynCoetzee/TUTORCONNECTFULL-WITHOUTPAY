namespace TutorConnect.API.Services
{
    public class GoogleCalendarService
    {
        // Uses Jitsi Meet — free, no API key required, no Google Workspace needed.
        // Generates a unique room URL that both parties can join directly from the email.
        public Task<string> CreateMeetLinkAsync(
            string title,
            DateOnly date,
            TimeOnly time,
            int durationMinutes = 60)
        {
            // Build a stable but unique room name from the title + date/time
            var sanitised = new string(title
                .Replace(" ", "-")
                .Where(c => char.IsLetterOrDigit(c) || c == '-')
                .ToArray());
            var shortId = Guid.NewGuid().ToString("N")[..8];
            var roomName = $"TutorConnect-{date:yyyyMMdd}-{shortId}";
            var link = $"https://meet.jit.si/{roomName}";
            return Task.FromResult(link);
        }
    }
}
