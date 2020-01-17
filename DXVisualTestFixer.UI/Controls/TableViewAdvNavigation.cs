using System;
using System.Windows.Input;
using DevExpress.Xpf.Grid;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.ViewModels;
using JetBrains.Annotations;

namespace DXVisualTestFixer.UI.Controls {
	public class TableViewAdvNavigation : TableView {
		public void MoveNextDataRow() {
			RepeatMoveAction(i => ++i);
		}

		public void MovePrevDataRow() {
			RepeatMoveAction(i => --i);
		}

		void RepeatMoveAction(Func<int, int> action) {
			var tryCount = 0;
			var focusedRowHandleCandidate = FocusedRowHandle;
			var visibleIndex = Grid.GetRowVisibleIndexByHandle(FocusedRowHandle);
			do {
				visibleIndex = action(visibleIndex);
				focusedRowHandleCandidate = Grid.GetRowHandleByVisibleIndex(visibleIndex);
			} while(focusedRowHandleCandidate < 0 && tryCount++ < 5);

			if(focusedRowHandleCandidate >= 0)
				FocusedRowHandle = focusedRowHandleCandidate;
		}

		[PublicAPI]
		public void ProcessDoubleClick(RowDoubleClickEventArgs e) {
			if(!e.HitInfo.InRow)
				return;
			if(Grid.IsGroupRowHandle(e.HitInfo.RowHandle))
				return;
			if(e.HitInfo.Column.FieldName != "Theme")
				return;
			e.Handled = true;
			InverseCommitChange(e.HitInfo.RowHandle);
		}

		TestInfoModel GetValidTestInfoModel(int rowHandle) {
			if(!Grid.IsValidRowHandle(rowHandle) || Grid.IsGroupRowHandle(rowHandle))
				return null;
			var testInfoModel = Grid.GetRow(rowHandle) as TestInfoModel;
			if(testInfoModel == null || testInfoModel.Valid == TestState.Error)
				return null;
			return testInfoModel;
		}

		void InverseCommitChange(int rowHandle) {
			var model = GetValidTestInfoModel(rowHandle);
			if(model != null)
				model.CommitChange = !model.CommitChange;
		}

		void SetCommitChange(int rowHandle, bool value) {
			if(Grid.IsGroupRowHandle(rowHandle)) {
				SetCommitChangeGroup(rowHandle, value);
				return;
			}

			var model = GetValidTestInfoModel(rowHandle);
			if(model != null)
				model.CommitChange = value;
		}

		void SetCommitChangeGroup(int groupRowHandle, bool value) {
			foreach(var rowHandle in GetChildHandles(groupRowHandle))
				SetCommitChange(rowHandle, value);
		}

		[PublicAPI]
		public void CommitAllInViewport() {
			var i = 0;
			while(i < Grid.VisibleRowCount - 1)
				SetCommitChange(Grid.GetRowHandleByVisibleIndex(i++), true);
		}

		[PublicAPI]
		public void ClearCommitsInViewport() {
			var i = 0;
			while(i < Grid.VisibleRowCount - 1)
				SetCommitChange(Grid.GetRowHandleByVisibleIndex(i++), false);
		}

		[PublicAPI]
		public void ProcessKeyDown(KeyEventArgs e) {
			if(e.Key != Key.Space)
				return;
			if(SearchControl != null && SearchControl.IsKeyboardFocusWithin)
				return;
			e.Handled = true;
			InverseCommitChange(FocusedRowHandle);
		}
	}
}