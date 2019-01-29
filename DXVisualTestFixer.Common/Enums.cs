namespace DXVisualTestFixer.Common {
    public enum TestState {
        Valid, Invalid, Fixed, Error
    }
    public enum FarmRefreshType {
        notification,
    }
    public enum FarmIntegrationStatus {
        Success = 0,
        Failure = 1,
        Exception = 2,
        Unknown = 3
    }
    public enum GitUpdateResult {
        None,
        Updated,
        Error
    }
    public enum GitCommitResult {
        None,
        Committed,
        Error
    }
}
