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

namespace MemuraiWiz.View
{
  /// <summary>
  /// Interaction logic for AdvancedWindow.xaml
  /// </summary>
  public partial class AdvancedWindow : Window
  {
    public AdvancedWindow()
    {
      InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
  }
}
