using System;

namespace ContactTracing.Core.Enums
{
    public enum ServerUpdateType
    {
        LockCase = 0,
        LockContact = 1,
        LockRelationship = 2,
        UnlockCase = 3,
        UnlockContact = 4,
        UnlockRelationship = 5,
        LockAllClientIsRefreshing = 6,
        UnlockAllClientRefreshComplete = 7,
        AddCase = 8,
        UpdateCase = 9,
        DeleteCase = 10,
        AddContact = 11,
        UpdateContact = 12,
        DeleteContact = 13,
        UpdateCaseContactRelationship = 14,
        UpdateFollowUp = 15,
        UpdateFollowUpAndForceRefresh = 16,
        DataImported = 17
    }
}
