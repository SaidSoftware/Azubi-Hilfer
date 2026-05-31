using System.ComponentModel;
using System.Windows.Media;

namespace AzubiHilfer.Services;

public enum AppLanguage { DE, EN }

public class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    private AppLanguage _language = AppLanguage.DE;
    public AppLanguage Language
    {
        get => _language;
        set { _language = value; OnPropertyChanged(nameof(Language)); NotifyAll(); }
    }

    private bool _darkMode = false;
    public bool DarkMode
    {
        get => _darkMode;
        set { _darkMode = value; OnPropertyChanged(nameof(DarkMode)); NotifyAll(); }
    }

    public bool IsDE => _language == AppLanguage.DE;

    // Colors (change with dark mode)
    public SolidColorBrush BgBrush         => DarkMode ? new(Color.FromRgb(0x12,0x12,0x1E)) : new(Color.FromRgb(0xF5,0xF7,0xFA));
    public SolidColorBrush CardBrush       => DarkMode ? new(Color.FromRgb(0x1E,0x1E,0x30)) : new(Colors.White);
    public SolidColorBrush SidebarBrush    => DarkMode ? new(Color.FromRgb(0x08,0x08,0x14)) : new(Color.FromRgb(0x0F,0x23,0x41));
    public SolidColorBrush TextBrush       => DarkMode ? new(Color.FromRgb(0xE2,0xE8,0xF0)) : new(Color.FromRgb(0x1A,0x1A,0x2E));
    public SolidColorBrush TextMutedBrush  => DarkMode ? new(Color.FromRgb(0x94,0xA3,0xB8)) : new(Color.FromRgb(0x6B,0x72,0x80));
    public SolidColorBrush BorderBrush2    => DarkMode ? new(Color.FromRgb(0x2D,0x2D,0x44)) : new(Color.FromRgb(0xE5,0xE7,0xEB));
    public SolidColorBrush HeaderBrush     => DarkMode ? new(Color.FromRgb(0x1A,0x1A,0x2C)) : new(Colors.White);
    public SolidColorBrush InputBgBrush    => DarkMode ? new(Color.FromRgb(0x2A,0x2A,0x3E)) : new(Color.FromRgb(0xF9,0xFA,0xFB));
    public SolidColorBrush InputBorderBrush=> DarkMode ? new(Color.FromRgb(0x3D,0x3D,0x5C)) : new(Color.FromRgb(0xD1,0xD5,0xDB));

    // Navigation
    public string Nav_Tasks    => IsDE ? "Aufgaben" : "Tasks";
    public string Nav_Documents=> IsDE ? "Dokumente" : "Documents";
    public string Nav_AllDocs  => IsDE ? "Alle Dokumente" : "All Documents";
    public string AppSubtitle  => IsDE ? "Azubi-Lernplattform" : "Apprentice Learning Platform";

    // Task Detail
    public string Label_Description => IsDE ? "Beschreibung" : "Description";
    public string Label_Stories     => IsDE ? "🧑‍💻 Lösungswege von Azubis" : "🧑‍💻 Solutions from Azubis";
    public string Label_Tags        => IsDE ? "Themen" : "Tags";
    public string Label_BackToTasks => IsDE ? "← Zurück" : "← Back";
    public string Label_CopyCode    => IsDE ? "Kopieren" : "Copy";
    public string Label_Copied      => IsDE ? "✓ Kopiert!" : "✓ Copied!";
    public string Label_Comments    => IsDE ? "💬 Kommentare & Notizen" : "💬 Comments & Notes";
    public string Label_AddComment  => IsDE ? "Kommentar hinzufügen..." : "Add a comment...";
    public string Label_Submit      => IsDE ? "Senden" : "Submit";
    public string Label_Stories_Add => IsDE ? "+ Lösungsweg hinzufügen" : "+ Add Solution";
    public string Label_Edit        => IsDE ? "Bearbeiten" : "Edit";
    public string Label_Delete      => IsDE ? "Löschen" : "Delete";
    public string Label_Search_Placeholder => IsDE ? "🔍 Suchen..." : "🔍 Search...";
    public string Label_Filter_All  => IsDE ? "Alle" : "All";
    public string Label_Export      => IsDE ? "📤 Export" : "📤 Export";

    private void NotifyAll()
    {
        foreach (var p in GetType().GetProperties())
            OnPropertyChanged(p.Name);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
