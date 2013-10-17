using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StartPageControl
{
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class Preferences : Window
    {

        public string PreferencesString { get; set; }

        public Preferences()
        {
            InitializeComponent();
        }


        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            textBoxProjects.Clear();
            var projects = PreferencesString.Split(';');
            foreach(string elProject in projects)
            {
                textBoxProjects.AppendText(string.Format("{0}\n", elProject));
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void SavePreferences_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < textBoxProjects.LineCount; ++i)
            {
                string str = textBoxProjects.GetLineText(i).Trim();
                if(str.Length > 0)
                {
                    sb.AppendFormat("{0};", str);
                }
            }

            PreferencesString = sb.ToString();

            DialogResult = true;
            Close();
        }


    }
}
