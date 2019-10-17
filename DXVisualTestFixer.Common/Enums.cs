namespace DXVisualTestFixer.Common {
	public enum TestState {
		Valid,
		Invalid,
		Fixed,
		Error
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