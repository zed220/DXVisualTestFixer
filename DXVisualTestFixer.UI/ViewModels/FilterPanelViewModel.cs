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
    public class FilterPanelViewModel : BindableBase {
        public class Problem {
            public Problem(int id, string value) {
                Id = id;
                Value = value;
            }

            public override bool Equals(object obj) {
                Problem other = obj as Problem;
                if(other == null)
                    return false;
                return other.Id == Id && other.Value == Value;
            }

            public override int GetHashCode() {
                var hashCode = 1325046378;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
                return hashCode;
            }

            public int Id { get; }
            public string Value { get; }
        }

        readonly ITestsService testsService;

        string _DpiNullText, _TeamsNullText, _VersionsNullText, _ProblemsNullText;
        List<int> _DpiList;
        List<string> _TeamsList;
        List<string> _VersionsList;
        List<Problem> _ProblemsList;
        List<object> _SelectedDpis;
        List<object> _SelectedTeams;
        List<object> _SelectedVersions;
        List<object> _SelectedProblems;
        bool _ShowFixedTests, _HasFixedTests;

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
        public List<Problem> ProblemsList {
            get { return _ProblemsList; }
            set { SetProperty(ref _ProblemsList, value); }
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
        public List<object> SelectedProblems {
            get { return _SelectedProblems; }
            set { SetProperty(ref _SelectedProblems, value, OnFilterChanged); }
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
        public string ProblemsNullText {
            get { return _ProblemsNullText; }
            set { SetProperty(ref _ProblemsNullText, value); }
        }
        public bool ShowFixedTests {
            get { return _ShowFixedTests; }
            set { SetProperty(ref _ShowFixedTests, value, OnFilterChanged); }
        }
        public bool HasFixedTests {
            get { return _HasFixedTests; }
            set { SetProperty(ref _HasFixedTests, value); }
        }

        public FilterPanelViewModel(ITestsService testsService) {
            this.testsService = testsService;
            BuildFilters(testsService.ActualState.TestList);
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
            SelectedTeams = new List<object>(actualTeamsList);
            SelectedVersions = new List<object>(VersionsList ?? new List<string>());
            SelectedDpis = new List<object>();
            DpiList.ForEach(dpi => SelectedDpis.Add(dpi));
        }

        void BuildFilters(List<TestInfo> tests) {
            TeamsList = tests.Select(t => t.Team.Name).Distinct().OrderBy(t => t).ToList();
            DpiList = tests.Select(t => t.Dpi).Distinct().OrderBy(d => d).ToList();
            if(!DpiList.Contains(96)) {
                DpiList.Add(96);
                DpiList.Sort();
            }
            VersionsList = tests.Select(t => t.Version).Distinct().OrderBy(v => v).ToList();
            ProblemsList = tests.Select(t => new Problem(t.Problem, t.ProblemName)).Distinct().OrderBy(n => n.Id).ToList();
            HasFixedTests = tests.FirstOrDefault(t => t.Valid == TestState.Fixed) != null;
        }

        void OnFilterChanged() {
            List<CriteriaOperator> resultList = new List<CriteriaOperator>();
            resultList.Add(CriteriaOperator.Or(GetCriteriaOperator(SelectedDpis, "Dpi")));
            resultList.Add(CriteriaOperator.Or(GetCriteriaOperator(SelectedTeams, "TeamName")));
            resultList.Add(CriteriaOperator.Or(GetCriteriaOperator(SelectedVersions, "Version")));
            resultList.Add(CriteriaOperator.Or(GetCriteriaOperator(SelectedProblems, "Problem")));
            if(HasFixedTests && !ShowFixedTests)
                resultList.Add(new BinaryOperator("Valid", TestState.Fixed, BinaryOperatorType.NotEqual));
            testsService.CurrentFilter = resultList.Count > 0 ? CriteriaOperator.And(resultList).ToString() : null;
        }
        static IEnumerable<CriteriaOperator> GetCriteriaOperator(IEnumerable selectedFilterItems, string fieldName) {
            if(selectedFilterItems == null)
                yield break;
            foreach(var selectedFilterItem in selectedFilterItems)
                yield return new BinaryOperator(fieldName, selectedFilterItem, BinaryOperatorType.Equal);
        }
    }
}
