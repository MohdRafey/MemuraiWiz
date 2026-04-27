using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MemuraiWiz.Model
{
  public class ConfigParameter : INotifyPropertyChanged
  {
    public string Key { get; set; }
    private string _value;
    public string Value
    {
      get => _value;
      set { _value = value; OnPropertyChanged(); }
    }
    public string Description { get; set; }
    public string Section { get; set; }
    public bool IsAdvanced { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
  }
}
