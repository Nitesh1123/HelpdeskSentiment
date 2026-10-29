using HelpdeskSentiment.Models;
using HelpdeskSentiment.Services;

namespace HelpdeskSentiment.Data
{
    // Adds some sample calls the first time the app runs so the dashboard is not empty.
    public static class SeedData
    {
        public static async Task SeedCallsAsync(HelpdeskDbContext db, ICallService calls)
        {
            if (db.CallRecords.Any())
            {
                return;
            }

            var samples = new[]
            {
                new CallRecord { CallerName = "Rahul Mehta", PhoneNumber = "9876543210", Category = CallCategory.Technical, AgentId = 1, DurationSeconds = 420,
                    Transcript = "My internet was not working since morning but the agent fixed it quickly. Thank you, very helpful service!" },
                new CallRecord { CallerName = "Sneha Kapoor", PhoneNumber = "9123456780", Category = CallCategory.Billing, AgentId = 2, DurationSeconds = 610,
                    Transcript = "I have been overcharged again this month. This is unacceptable and I am really frustrated. I want to talk to your manager." },
                new CallRecord { CallerName = "Arjun Patel", PhoneNumber = "9988776655", Category = CallCategory.Account, AgentId = 3, DurationSeconds = 180,
                    Transcript = "I want to update the email address on my account. Please send the confirmation link." },
                new CallRecord { CallerName = "Priya Nair", PhoneNumber = "8899001122", Category = CallCategory.Complaint, AgentId = 1, DurationSeconds = 900,
                    Transcript = "Worst experience ever. I waited for two hours, the app crashed twice and support was rude. Cancel my plan." },
                new CallRecord { CallerName = "Vikram Rao", PhoneNumber = "7766554433", Category = CallCategory.Technical, AgentId = 2, DurationSeconds = 300,
                    Transcript = "The router issue is resolved now, it works fine. Not bad at all, I appreciate the quick response." },
                new CallRecord { CallerName = "Anjali Gupta", PhoneNumber = "9012345678", Category = CallCategory.General, AgentId = 3, DurationSeconds = 120,
                    Transcript = "Just calling to know your office timings on Saturday." }
            };

            DateTime start = DateTime.Now.AddDays(-samples.Length);
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i].CallTime = start.AddDays(i).AddHours(10 + i);
                await calls.CreateAsync(samples[i]);
            }
        }
    }
}
