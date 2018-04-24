using DevExpress.Data.Filtering;
using DXVisualTestFixer.Common;
using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.ViewModels {
    public class FilterPanelViewModel : BindableBase, IFilterPanelViewModel {
        string _DpiNullText, _TeamsNullText, _VersionsNullText;
        Action<CriteriaOperator> SetFilterAction { get; }
        List<int> _DpiList;
        List<string> _TeamsList;
        List<string> _VersionsList;
        List<object> _SelectedDpis;
        List<object> _SelectedTeams;
        List<object> _SelectedVersions;

        public List<int> DpiList {
            get { return _DpiList; }
            set { SetProperty(ref _DpiList, value); }
        }
        public List<string> TeamsList {
            get { return _TeamsList; }
            set { SetProperty(ref _TeamsList, value); }
        }
        public List<string> VersionsList {
            get { return _VersionsList; }
            set { SetProperty(ref _VersionsList, value); }
        }
        public List<object> SelectedDpis {
            get { return _SelectedDpis; }
            set { SetProperty(ref _SelectedDpis, value, OnFilterChanged); }
        }
        public List<object> SelectedTeams {
            get { return _SelectedTeams; }
            set { SetProperty(ref _SelectedTeams, value, OnFilterChanged); }
        }
        public List<object> SelectedVersions {
            get { return _SelectedVersions; }
            set { SetProperty(ref _SelectedVersions, value, OnFilterChanged); }
        }
        public string DpiNullText {
            get { return _DpiNullText; }
            set { SetProperty(ref _DpiNullText, value); }
        }
        public string TeamsNullText {
            get { return _TeamsNullText; }
            set { SetProperty(ref _TeamsNullText, value); }
        }
        public string VersionsNullText {
            get { return _VersionsNullText; }
            set { SetProperty(ref _VersionsNullText, value); }
        }

        public FilterPanelViewModel(IMainViewModel mainViewModel) {
            BuildFilters(mainViewModel.Tests);
            SetFilterAction = mainViewModel.SetFilter;
            InitializeDefaultFilters();
            InitializeNullTexts();
        }

        void InitializeNullTexts() {
            DpiNullText = BuildNullText(DpiList);
            TeamsNullText = BuildNullText(TeamsList);
            VersionsNullText = BuildNullText(VersionsList);
        }
        static string BuildNullText<T>(IEnumerable<T> source) {
            return $"({string.Join(", ", source)})";
        }

        private void InitializeDefaultFilters() {
            List<string> actualTeamsList = TeamsList != null ? TeamsList.ToList() : new List<string>();
            //if(actualTeamsList.Contains("Editors"))
            //    actualTeamsList.Remove("Editors");
            SelectedTeams = new List<object>(actualTeamsList);
            //TeamsList.ForEach(SelectedTeams.Add);
            SelectedVersions = new List<object>(VersionsList ?? new List<string>());
            //VersionsList.ForEach(SelectedVersions.Add);
            SelectedDpis = new List<object>();
            DpiList.ForEach(dpi => SelectedDpis.Add(dpi));
            //SelectedDpis.Add(96);
        }

        void BuildFilters(List<ITestInfoWrapper> tests) {
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
}
