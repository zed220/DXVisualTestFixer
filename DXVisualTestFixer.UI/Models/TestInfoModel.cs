﻿using System.Collections.Generic;
using DevExpress.Mvvm;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Common;
using JetBrains.Annotations;

namespace DXVisualTestFixer.UI.ViewModels {
	public class TestInfoModel : BindableBase, ITestInfoModel {
		readonly MainViewModel _viewModel;

		public TestInfoModel(MainViewModel viewModel, TestInfo testInfo) {
			_viewModel = viewModel;
			TestInfo = testInfo;
		}

		[UsedImplicitly] public bool ImageEquals => TestInfo.ImageEquals;

		public TestInfo TestInfo { get; }
		public TestState Valid => TestInfo.Valid;
		public string Version => TestInfo.Version;
		public string VersionAndFork => TestInfo.Repository.VersionAndFork;
		public bool Optimized => TestInfo.Optimized;
		public bool Colorized => TestInfo.Colorized;
		public string Browser => TestInfo.Browser;
		public string Volunteer => TestInfo.Volunteer;
		public string VolunteerShort => Volunteer != null ? InitialsExtractor.Extract(Volunteer) : null;
		public string Name => TestInfo.Name;
		public string TeamName => TestInfo.TeamName;
		public string Theme => TestInfo.Theme;
		public int Dpi => TestInfo.Dpi;
		public int Problem => TestInfo.Problem;
		public string ProblemName => TestInfo.ProblemName;

		public bool CommitChange {
			get => GetProperty(() => CommitChange);
			set => SetCommitChange(value);
		}
		public bool CommitAsBlinking {
			get => TestInfo.CommitAsBlinking;
			set => TestInfo.CommitAsBlinking = value;
		}

		public string ToLog() {
			return $"Team: {TestInfo?.TeamName}, Version: {TestInfo?.Version}, Test: {TestInfo?.NameWithNamespace}, Theme: {TestInfo?.Theme}";
		}

		void SetCommitChange(bool value) {
			if(Valid == TestState.Error)
				return;
			CommitAsBlinking = false;
			SetProperty(() => CommitChange, value, OnChanged);
		}

		void OnChanged() {
			if(CommitChange)
				_viewModel.CommitTest(this);
			else
				_viewModel.UndoCommitTest(this);
		}
	}
}