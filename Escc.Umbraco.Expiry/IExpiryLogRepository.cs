using System.Collections.Generic;

namespace Escc.Umbraco.Expiry
{
    public interface IExpiryLogRepository
    {
        void SetExpiryLogDetails(ExpiryLogEntry model);
        List<ExpiryLogEntry> GetExpiryLogs();
        List<ExpiryLogEntry> GetExpiryLogSuccessDetails();
        List<ExpiryLogEntry> GetExpiryLogFailureDetails();
        ExpiryLogEntry GetExpiryLogById(int id);
    }
}