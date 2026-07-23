using Eagle.DAL.Data;
using Eagle.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eagle.BL.Services
{
    public class OverrideCodeService
    {
        private readonly EagleDbContext _db;
        public OverrideCodeService(EagleDbContext db) => _db = db;

        public async Task<string> GetOrCreateTodayCodeAsync()
        {
            var today = DateTime.UtcNow.Date;
            var existing = await _db.DailyOverrideCodes.FirstOrDefaultAsync(c => c.Date == today);
            if (existing != null) return existing.Code;

            var random = new Random();
            var code = random.Next(10000, 100000).ToString(); // 5 digits, 10000-99999

            var entry = new DailyOverrideCode { Date = today, Code = code };
            _db.DailyOverrideCodes.Add(entry);
            await _db.SaveChangesAsync();
            return code;
        }

        public async Task<bool> ValidateCodeAsync(string? inputCode)
        {
            if (string.IsNullOrWhiteSpace(inputCode)) return false;
            var today = DateTime.UtcNow.Date;
            var existing = await _db.DailyOverrideCodes.FirstOrDefaultAsync(c => c.Date == today);
            return existing != null && existing.Code == inputCode.Trim();
        }
    }
}