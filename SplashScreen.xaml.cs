using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HeartPC
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            LoadingText.Text = "Loading...";
        }

        public void UpdateProgress(int progress)
        {
            LoadingProgress.Value = progress;
        }

        public void UpdateStatus(string status)
        {
            LoadingText.Text = status;
        }
    }
}