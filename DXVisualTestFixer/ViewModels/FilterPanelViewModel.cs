using DevExpress.Data.Filtering;
using DevExpress.Mvvm;
using DevExpress.Mvvm.ModuleInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.ViewModels {
    public interface IFilterPanelViewModel : ISupportParameter { }

    public class FilterPanelViewModel : ViewModelBase, IFilterPanelViewModel {
        public FilterPanelViewModel() {
            ModuleManager.DefaultManager.GetEvents(this).ViewModelRemoving += FilterPanelViewModel_ViewModelRemoving;
        }

        void FilterPanelViewModel_ViewModelRemoving(object sender, ViewModelRemovingEventArgs e) {
            ModuleManager.DefaultManager.GetEvents(this).ViewModelRemoving -= FilterPanelViewModel_ViewModelRemoving;
            SetFilterAction = null;
        }

        public List<int> DpiList {
            get { return GetProperty(() => DpiList); }
            set { SetProperty(() => DpiList, value); }
        }
        public List<string> TeamsList {
            get { return GetProperty(() => TeamsList); }
            set { SetProperty(() => TeamsList, value); }
        }
        public List<string> VersionsList {
            get { return GetProperty(() => VersionsList); }
            set { SetProperty(() => VersionsList, value); }
        }

        public List<object> SelectedDpis {
            get { return GetProperty(() => SelectedDpis); }
            set { SetProperty(() => SelectedDpis, value, OnFilterChanged); }
        }
        public List<object> SelectedTeams {
            get { return GetProperty(() => SelectedTeams); }
            set { SetProperty(() => SelectedTeams, value, OnFilterChanged); }
        }
        public List<object> SelectedVersions {
            get { return GetProperty(() => SelectedVersions); }
            set { SetProperty(() => SelectedVersions, value, OnFilterChanged); }
        }

        Action<CriteriaOperator> SetFilterAction { get; set; }

        protected override void OnParameterChanged(object parameter) {
            base.OnParameterChanged(parameter);
            FilterPanelViewModelParameter filterPanelViewModelParameter = parameter as FilterPanelViewModelParameter;
            if(filterPanelViewModelParameter == null)
                return;
            BuildFilters(filterPanelViewModelParameter.Tests);
            SetFilterAction = filterPanelViewModelParameter.SetFilterAction;
            InitializeDefaultFilters();
        }

        private void InitializeDefaultFilters() {
            List<string> actualTeamsList = TeamsList != null ? TeamsList : new List<string>();
            if(actualTeamsList.Contains("Editors"))
                actualTeamsList.Remove("Editors");
            SelectedTeams = new List<object>(actualTeamsList);
            //TeamsList.ForEach(SelectedTeams.Add);
            SelectedVersions = new List<object>(VersionsList ?? new List<string>());
            //VersionsList.ForEach(SelectedVersions.Add);
            SelectedDpis = new List<object>() { 96 };
            //SelectedDpis.Add(96);
        }

        void BuildFilters(List<TestInfoWrapper> tests) {
            TeamsList = tests.Select(t => t.TeamName).Distinct().OrderBy(t => t).ToList();
            DpiList = tests.Select(t => t.Dpi).Distinct().OrderBy(d => d).ToList();
            if(!DpiList.Contains(96)) {
                DpiList.Add(96);
                DpiList.Sort();
            }
            VersionsList = tests.Select(t => t.Version).Distinct().OrderBy(v => v).ToList();
        }

        void OnFilterChanged() {
            if(SetFilterAction == null)
                return;
            List<CriteriaOperator> resultList = new List<CriteriaOperator>();
            if(SelectedDpis != null && SelectedDpis.Count > 0) {
                List<CriteriaOperator> dpis = new List<CriteriaOperator>();
                foreach(int selectedDpi in SelectedDpis.Cast<int>()) {
                    dpis.Add(new BinaryOperator("Dpi", selectedDpi, BinaryOperatorType.Equal));
                }
                resultList.Add(CriteriaOperator.Or(dpis));
            }
            if(SelectedTeams != null && SelectedTeams.Count > 0) {
                List<CriteriaOperator> teams = new List<CriteriaOperator>();
                foreach(string selectedTeam in SelectedTeams.Cast<string>()) {
                    teams.Add(new BinaryOperator("TeamName", selectedTeam, BinaryOperatorType.Equal));
                }
                resultList.Add(CriteriaOperator.Or(teams));
            }
            if(SelectedVersions != null && SelectedVersions.Count > 0) {
                List<CriteriaOperator> versions = new List<CriteriaOperator>();
                foreach(string selectedVersion in SelectedVersions.Cast<string>()) {
                    versions.Add(new BinaryOperator("Version", selectedVersion, BinaryOperatorType.Equal));
                }
                resultList.Add(CriteriaOperator.Or(versions));
            }
            if(resultList.Count > 0) {
                SetFilterAction(CriteriaOperator.And(resultList));
            }
            else
                SetFilterAction(null);
            //CriteriaOperator.Or()
            //CriteriaOperator.And()
            //CriteriaOperator result = null;
            //foreach(var str in SelectedDpis ?? new List<object>()) {
            //    result += CriteriaOperator.Parse($"[Dpi] = {str}");
            //}

        }
    }

    public class FilterPanelViewModelParameter {
        public FilterPanelViewModelParameter(List<TestInfoWrapper> tests, Action<CriteriaOperator> setFilterAction) {
            Tests = tests;
            SetFilterAction = setFilterAction;
        }

        public List<TestInfoWrapper> Tests { get; private set; }
        public Action<CriteriaOperator> SetFilterAction { get; private set; }
    }
}
