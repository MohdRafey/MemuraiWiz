using MemuraiWiz.Helpers;
using MemuraiWiz.Model;
using MemuraiWiz.View;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MemuraiWiz.ViewModel
{
  public class MainViewModel : INotifyPropertyChanged
  {
    // UI State Properties
    private string _statusMessage = "Ready";
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

    private Brush _statusColor = Brushes.Gray;
    public Brush StatusColor { get => _statusColor; set { _statusColor = value; OnPropertyChanged(); } }

    private bool _isPathValid;
    public bool IsPathValid { get => _isPathValid; set { _isPathValid = value; OnPropertyChanged(); } }

    public ProjectSettingsViewModel Settings { get; } = new ProjectSettingsViewModel();
    public ObservableCollection<ConfigParameter> Parameters { get; set; } = new ObservableCollection<ConfigParameter>();

    public MainViewModel()
    {
      // Subscribe to the settings verification event
      Settings.EnvironmentVerified += OnEnvironmentVerified;

      Settings.Load();

      // Commands
      BrowseCommand = new RelayCommand(Settings.ExecuteBrowse);
      VerifyCommand = new RelayCommand(Settings.ExecuteVerify);
      InstallCommand = new RelayCommand(ExecuteInstall, () => IsPathValid);

      // Initial Check
      Settings.ExecuteVerify();

      OpenAdvancedCommand = new RelayCommand(ExecuteOpenAdvanced);
    }

    private void ExecuteOpenAdvanced()
    {
      var advWindow = new AdvancedWindow();

      // Crucial: Share the same DataContext so the Advanced window 
      // sees the AdvancedParameters collection we already loaded.
      advWindow.DataContext = this;

      // Set the main window as the owner (good for centering and behavior)
      advWindow.Owner = System.Windows.Application.Current.MainWindow;

      advWindow.ShowDialog(); // ShowDialog blocks the main window until closed
    }

    private void OnEnvironmentVerified()
    {
      LoadConfiguration();
    }

    public IEnumerable<ConfigParameter> BasicParameters => Parameters.Where(p => !p.IsAdvanced);
    public IEnumerable<ConfigParameter> AdvancedParameters => Parameters.Where(p => p.IsAdvanced);

    public ICommand BrowseCommand { get; }
    public ICommand VerifyCommand { get; }
    public ICommand InstallCommand { get; }

    public ICommand OpenAdvancedCommand { get; }

    //private void LoadConfiguration()
    //{
    //  string confPath = Settings.FullConfigPath;
    //  if (!File.Exists(confPath)) return;

    //  var tempParams = new List<ConfigParameter>();
    //  var lines = File.ReadAllLines(confPath);
    //  string currentDescription = "";

    //  foreach (var line in lines)
    //  {
    //    string trimmed = line.Trim();

    //    // 1. If it's a section divider (like ####### NETWORK #######), 
    //    // we should probably clear the old header info and start fresh.
    //    if (trimmed.Contains("~~~~~") || trimmed.Contains("-----") || (trimmed.StartsWith("###") && trimmed.EndsWith("###")))
    //    {
    //      // If it's a major section like ### NETWORK ###, we reset to avoid "bloat" from previous sections
    //      if (trimmed.Contains("NETWORK") || trimmed.Contains("SECURITY") || trimmed.Contains("MEMORY"))
    //      {
    //        currentDescription = "";
    //      }
    //      continue;
    //    }

    //    if (trimmed.StartsWith("#"))
    //    {
    //      // 1. Remove the leading '#' and exactly ONE space if it exists
    //      string content = trimmed.Substring(1);
    //      if (content.StartsWith(" ")) content = content.Substring(1);

    //      // 2. Filter: Only ignore lines that are EXCLUSIVELY decorators (hashes or tildes)
    //      // This keeps the "~~~ WARNING ~~~" because it contains text.
    //      string superTrimmed = content.Trim();
    //      bool isDecorator = superTrimmed.All(c => c == '#' || c == '~' || c == '-' || c == ' ');

    //      if (isDecorator && !string.IsNullOrWhiteSpace(superTrimmed))
    //      {
    //        // If it's a section header like ########### NETWORK ###########, reset the bucket.
    //        if (superTrimmed.Contains("###")) currentDescription = "";
    //        continue;
    //      }

    //      // 3. Append and preserve the new line for visual spacing
    //      currentDescription += content + Environment.NewLine;
    //      continue;
    //    }

    //    if (string.IsNullOrWhiteSpace(trimmed)) continue;

    //    var match = Regex.Match(trimmed, @"^([^\s#]+)\s+(.+)$");
    //    if (match.Success)
    //    {
    //      var key = match.Groups[1].Value;

    //      // Final cleaning: If the description is huge, it means it's garbage 
    //      // from the top of the file. Let's keep only the last few lines or specific text.
    //      string cleanDescription = currentDescription.Trim();

    //      tempParams.Add(new ConfigParameter
    //      {
    //        Key = key,
    //        Value = match.Groups[2].Value,
    //        Description = SanitizeDescription(currentDescription),
    //        IsAdvanced = IsKeyAdvanced(key)
    //      });

    //      // CRITICAL: Clear the bucket for the next parameter
    //      currentDescription = "";
    //    }
    //  }
    //  Parameters.Clear();
    //  foreach (var p in tempParams) Parameters.Add(p);
    //  OnPropertyChanged(nameof(BasicParameters));
    //  OnPropertyChanged(nameof(AdvancedParameters));
    //}

    private void LoadConfiguration()
    {
      string confPath = Settings.FullConfigPath;

      // 1. Physical existence check
      if (!File.Exists(confPath))
      {
        IsPathValid = false;
        StatusMessage = $"✘ Config file missing: {Settings.ActiveConfigName}";
        StatusColor = Brushes.Crimson;
        Parameters.Clear();
        RefreshFilteredLists();
        return;
      }

      try
      {
        var tempParams = new List<ConfigParameter>();
        var lines = File.ReadAllLines(confPath);

        string currentDescription = "";
        string currentSection = "GENERAL";

        foreach (var line in lines)
        {
          string trimmed = line.Trim();

          // A. Section Header Detection (e.g., ### NETWORK ###)
          if (trimmed.StartsWith("###") && trimmed.Any(char.IsLetter))
          {
            string sectionName = trimmed.Replace("#", "").Trim();
            if (!string.IsNullOrWhiteSpace(sectionName))
            {
              currentSection = sectionName;
              currentDescription = "";
            }
            continue;
          }

          // B. Comment & Decorator Harvesting
          if (trimmed.StartsWith("#"))
          {
            string content = trimmed.Substring(1);
            if (content.StartsWith(" ")) content = content.Substring(1);

            // Filter out lines that are just symbol-decorators
            string superTrimmed = content.Trim();
            bool isDecorator = superTrimmed.All(c => c == '#' || c == '~' || c == '-' || c == ' ');

            if (isDecorator && !string.IsNullOrWhiteSpace(superTrimmed)) continue;

            currentDescription += content + Environment.NewLine;
            continue;
          }

          if (string.IsNullOrWhiteSpace(trimmed)) continue;

          // C. Parameter Extraction (Key Value)
          var match = Regex.Match(trimmed, @"^([^\s#]+)\s+(.+)$");
          if (match.Success)
          {
            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value.Trim('"');

            tempParams.Add(new ConfigParameter
            {
              Key = key,
              Value = value,
              Description = SanitizeDescription(currentDescription),
              Section = currentSection,
              IsAdvanced = IsKeyAdvanced(key)
            });

            // SMART CHECK: Peek at the next few lines. 
            // If the next active line is another key (no comments in between), 
            // we KEEP the currentDescription for it.
            bool nextIsKey = false;
            for (int j = lines.ToList().IndexOf(line) + 1; j < lines.Length; j++)
            {
              string nextLine = lines[j].Trim();
              if (string.IsNullOrWhiteSpace(nextLine)) continue;
              if (nextLine.StartsWith("#")) break; // Found a new comment block
              if (Regex.IsMatch(nextLine, @"^([^\s#]+)\s+(.+)$"))
              {
                nextIsKey = true;
                break;
              }
            }

            if (!nextIsKey)
            {
              currentDescription = ""; // Only clear if no more keys share this help text
            }
          }
        }

        // 2. Final UI Update
        Parameters.Clear();
        foreach (var p in tempParams) Parameters.Add(p);

        RefreshFilteredLists();

        // 3. Success State
        IsPathValid = true;
        StatusMessage = "✔ memurai.exe and configuration loaded.";
        StatusColor = Brushes.LimeGreen;
      }
      catch (IOException ex)
      {
        // Handles cases where the file is locked by another process
        IsPathValid = false;
        StatusMessage = "✘ Access Denied: File might be in use.";
        StatusColor = Brushes.Crimson;
      }
      catch (Exception ex)
      {
        IsPathValid = false;
        StatusMessage = "✘ Critical Error during parsing.";
        StatusColor = Brushes.Crimson;
      }
    }

    // Helper to trigger UI refresh for the two filtered lists
    private void RefreshFilteredLists()
    {
      OnPropertyChanged(nameof(BasicParameters));
      OnPropertyChanged(nameof(AdvancedParameters));
    }
    private string SanitizeDescription(string rawDescription)
    {
      if (string.IsNullOrWhiteSpace(rawDescription)) return "No description available.";

      // 1. Normalize line endings
      string text = rawDescription.Replace("\r\n", "\n").Trim();

      // 2. Identify paragraphs by looking for double new-lines
      // We split by double new-lines to keep actual paragraph breaks
      var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

      for (int i = 0; i < paragraphs.Length; i++)
      {
        // 3. For each paragraph, replace single new-lines with a space
        // This "unwraps" the text into a single continuous flow
        paragraphs[i] = paragraphs[i].Replace("\n", " ").Trim();

        // 4. Clean up double spaces that might have been created
        while (paragraphs[i].Contains("  "))
          paragraphs[i] = paragraphs[i].Replace("  ", " ");
      }

      // 5. Join paragraphs back together with a clean double-break
      return string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
    }

    private bool IsKeyAdvanced(string key)
    {
      return !Settings.MainParameterKeys.Contains(key.ToLower());
    }

    private void ExecuteInstall()
    {
      try
      {
        string confPath = Settings.FullConfigPath;
        var lines = File.ReadAllLines(confPath).ToList();

        for (int i = 0; i < lines.Count; i++)
        {
          var line = lines[i].Trim();
          if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;

          var match = Regex.Match(line, @"^([^\s#]+)\s+(.+)$");
          if (match.Success)
          {
            var key = match.Groups[1].Value;
            var param = Parameters.FirstOrDefault(p => p.Key == key);
            if (param != null) lines[i] = $"{key} {param.Value}";
          }
        }

        File.WriteAllLines(confPath, lines);

        Process.Start(new ProcessStartInfo
        {
          FileName = Path.Combine(Settings.InstallPath, "memurai.exe"),
          Arguments = $"--service-install --config-file \"{confPath}\"",
          Verb = "runas",
          UseShellExecute = true
        });

        MessageBox.Show("Service Initialized.");
      }
      catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
}