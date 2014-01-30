// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace StartPageControl
{

    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MyControl : UserControl
    {
        private const string SOLUTION_FILE_MASK = "*.sln";
        private const string PROJECTS_SETTING_NAME = "Projects";
        private const char PROJECTS_DEVIDER_CHAR = ';';
        private const char PROJECT_PREFS_DIVIDER_CHAR = '|';

        private string _CurrentPreferences = string.Empty;

        private bool _IsLoaded = false;

        List<Project> _Projects;

        public MyControl()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_IsLoaded)
            {
                _IsLoaded = true;
                LoadProjects();
            }
        }

        private void LoadProjects()
        {
            //lblStatus.Content = "Scanning Folder...";
            //lblStatus.Visibility = System.Windows.Visibility.Visible;                
            _CurrentPreferences = StartPageSettings.RetrieveString(PROJECTS_SETTING_NAME);
            lblStatus.Visibility = System.Windows.Visibility.Visible;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(CollectSolutions);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.WorkerReportsProgress = true;
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerAsync();
        }

        private List<Project> GetProjects()
        {
            List<Project> projects = new List<Project>();

            string projectsSetting = _CurrentPreferences;
            string[] projectStrings = projectsSetting.Split(PROJECTS_DEVIDER_CHAR);
            foreach (string elProject in projectStrings)
            {
                string[] projectSettings = elProject.Split(PROJECT_PREFS_DIVIDER_CHAR);
                if (projectSettings.Length < 2)
                {
                    continue;
                }
                projects.Add(new Project() { ProjectName = projectSettings[0], ProjectPath = projectSettings[1] });
            }

            return projects;
        }

        void CollectSolutions(object sender, DoWorkEventArgs e)
        {
            ((BackgroundWorker)sender).ReportProgress(0, "Getting projects settings...");
            _Projects = GetProjects();
            foreach (Project elProject in _Projects)
            {
                ((BackgroundWorker)sender).ReportProgress(0, string.Format("Scanning for solutions in project {0}...", elProject.ProjectName));
                var projectSolutions = LoadSolutionsForProject(elProject);
                if(elProject.Solutions == null)
                {
                    elProject.Solutions = new List<Solution>();
                }
                try
                {
                    elProject.Solutions.AddRange(projectSolutions);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                }

                elProject.Solutions.Sort(new Comparison<Solution>((x, y) => x.DisplayName.CompareTo(y.DisplayName)));
             }
        }

        private List<Solution> LoadSolutionsForProject(Project parProject)
        {
            List<Solution> solutions = new List<Solution>();
            try
            {
                var findedSolutions = GetSolutionsInDirectory(parProject.ProjectPath);
                foreach (string elSolutionFile in findedSolutions)
                {

                    solutions.Add(new Solution()
                        {
                            DisplayName = System.IO.Path.GetFileName(elSolutionFile),
                            FullPath = elSolutionFile        
                        }
                       );
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }

            return solutions;
        }

        private IEnumerable<string> GetSolutionsInDirectory(string parDirectoryName)
        {
            List<string> solutions = new List<string>();
            bool found = false;
            DirectoryInfo di = new DirectoryInfo(parDirectoryName);
            foreach (FileInfo fi in di.GetFiles(SOLUTION_FILE_MASK, SearchOption.TopDirectoryOnly))
            {
                found = true;
                solutions.Add(fi.FullName);
            }
            if (!found)
            {
                foreach (DirectoryInfo d in di.GetDirectories())
                {
                    solutions.AddRange(GetSolutionsInDirectory(d.FullName));
                }
            }

            return solutions;
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            lblStatus.Content = e.UserState.ToString();
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lblStatus.Content = "Creating GUI...";
            CreateProjectTabs();
            lblStatus.Content = "Finished";
            lblStatus.Visibility = System.Windows.Visibility.Hidden;
        }

        private void CreateProjectTabs()
        {
            solutionsTabPane.ItemsSource = _Projects;
            solutionsTabPane.SelectedIndex = 0;
        }

        public static DTE2 GetDTE(object dataContext)
        {
            DataSource source = dataContext as DataSource;
            foreach (IPropertyDescription property in source.Properties)
            {
                if (property.Name == "DTE")
                {
                    return source.GetValue("DTE") as DTE2;
                }
            }
            return null;
        }

        private void OpenSolution_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string path = btn.CommandParameter.ToString();
            if (!File.Exists(path))
            {
                MessageBox.Show("This Solution is missing");
            }
            else
            {
                var dte = GetDTE(DataContext);
                ServiceProvider serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte);
                IVsSolution solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

                solution.OpenSolutionFile((uint)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, path);
            }
        }

        private void ConfigurationBtn_Click(object sender, RoutedEventArgs e)
        {
            Preferences prefs = new Preferences();
            prefs.PreferencesString = StartPageSettings.RetrieveString(PROJECTS_SETTING_NAME);            
            var configResult = prefs.ShowDialog();
            if(configResult.HasValue && configResult.Value == true)
            {
                _CurrentPreferences = prefs.PreferencesString;
                StartPageSettings.StoreString(PROJECTS_SETTING_NAME, prefs.PreferencesString);
                LoadProjects();
            }   
        }

        private StartPageSettings _settings;
        /// <summary>
        /// Return a StartPageSettings object.
        ///
        /// Use: StartPageSettings.StoreString("MySettingName", "MySettingValue");
        /// Use: string value = StartPageSettings.RetrieveString("MySettingName");
        ///
        /// Note: As this property is using the Start Page tool window DataContext to retrieve the Visual Studio DTE, 
        /// this property can only be used after the UserControl is loaded and the inherited DataContext is set.
        /// </summary>
        private StartPageSettings StartPageSettings
        {
            get
            {
                if (_settings == null)
                {
                    DTE2 dte = GetDTE(DataContext);
                    ServiceProvider serviceProvider = Utilities.GetServiceProvider(dte);
                    _settings = new StartPageSettings(serviceProvider);
                }
                return _settings;
            }
        }
    }

    public class Project
    {
        public string ProjectName { get; set; }
        public string ProjectPath { get; set; }

        public List<Solution> Solutions { get; set; }
    }

    public class Solution
    {
        public string DisplayName { get; set; }
        public string FullPath { get; set; }
    }

}
