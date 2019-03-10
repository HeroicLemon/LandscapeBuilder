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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LandscapeBuilderGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            LandscapeBuilderViewModel viewModel = new LandscapeBuilderViewModel();

            DataContext = viewModel;
            InitializeComponent();
        }

        // TODO: There should be an MVVM way to do this...Interaction Triggers was not working though.
        private void MapColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.OldValue != null)
            {
                Color oldColor = (Color)e.OldValue;
                Color newColor = (Color)e.NewValue;

                LandscapeBuilderViewModel viewModel = (LandscapeBuilderViewModel)DataContext;
                viewModel.MapColorChanged(oldColor, newColor);
            }
        }
    }
}
