using PersonalExpenseTracker.Models.Reports;

namespace PersonalExpenseTracker.Services;

public interface IReportService
{
    Task<ReportViewModel> CreateAsync(
        string userId,
        int months,
        CancellationToken cancellationToken = default);
}
