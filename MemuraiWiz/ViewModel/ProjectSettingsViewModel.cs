using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Xml.Linq;

namespace MemuraiWiz.ViewModel
{
  public class ProjectSettingsViewModel : INotifyPropertyChanged
  {
    private string _installPath = @"C:\Program Files\Memurai\";
    private string _activeConfigName = "memurai.conf";
    private string _settingsFile = Path.Combine(
    AppContext.BaseDirectory,
    "MemuraiWiz.config.xml"
      );

    private List<string> _mainParameterKeys = new List<string>();
    public List<string> MainParameterKeys
    {
      get => _mainParameterKeys;
      set { _mainParameterKeys = value; OnPropertyChanged(); }
    }

    public event Action EnvironmentVerified;

    public string InstallPath
    {
      get => _installPath;
      set { _installPath = value; OnPropertyChanged(); }
    }

    public string ActiveConfigName
    {
      get => _activeConfigName;
      set { _activeConfigName = value; OnPropertyChanged(); }
    }

    public string FullConfigPath => Path.Combine(InstallPath, ActiveConfigName);

    public void ExecuteBrowse()
    {
      var dialog = new OpenFileDialog { Filter = "Memurai Executable|memurai.exe" };
      if (dialog.ShowDialog() == true)
      {
        InstallPath = Path.GetDirectoryName(dialog.FileName) + "\\";
        ExecuteVerify();
      }
    }

    public void ExecuteVerify()
    {
      bool isValid = Directory.Exists(InstallPath) &&
                    File.Exists(Path.Combine(InstallPath, "memurai.exe"));

      if (isValid)
      {
        Save();
        EnvironmentVerified?.Invoke(); // Trigger the reload in MainViewModel
      }
    }

    public void Save()
    {
      var doc = new XDocument(
          new XElement("Settings",
              new XElement("InstallPath", InstallPath),
              new XElement("ActiveConfigName", ActiveConfigName),
              new XElement("MainParameters",
                  MainParameterKeys.Select(k => new XElement("Key", k))
              )
          )
      );
      doc.Save(_settingsFile);
    }

    public void Load()
    {
      if (!File.Exists(_settingsFile)) { Save(); return; }

      try
      {
        var doc = XDocument.Load(_settingsFile);
        InstallPath = doc.Root.Element("InstallPath")?.Value ?? _installPath;
        ActiveConfigName = doc.Root.Element("ActiveConfigName")?.Value ?? _activeConfigName;

        // Load the keys from XML
        var keys = doc.Root.Element("MainParameters")?
                          .Elements("Key")
                          .Select(x => x.Value.ToLower())
                          .ToList();

        if (keys != null && keys.Any())
        {
          MainParameterKeys = keys;
        }
        else
        {
          // Fallback to hardcoded defaults if XML section is missing
          MainParameterKeys = new List<string> { "port", "bind", "requirepass" };
        }
      }
      catch { Save(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
  }

}