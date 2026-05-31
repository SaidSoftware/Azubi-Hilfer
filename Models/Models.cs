namespace AzubiHilfer.Models;

public enum AzubiGroup
{
    FachinformatikerAnwendungsentwicklung,
    KaufmannDigitalisierungsmanagement
}

public enum Difficulty { Beginner, Intermediate, Advanced }

public enum Department
{
    Support,
    Entwicklung,
    Schnittstelle,
    Vertragsdatenbank,
    Verwaltung,
    Azubi,
    Technik,
    Vertrieb,
    Kundenbetreung,
    Report
}

public class Task
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public string Description { get; set; } = "";
    public string DescriptionEn { get; set; } = "";
    public AzubiGroup Group { get; set; }
    public List<Story> Stories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public Difficulty Difficulty { get; set; } = Difficulty.Beginner;
    public Department? Department { get; set; }
    public bool IsCompleted { get; set; } = false;
    public string AuthorName { get; set; } = "";
    public string ExternalLinks { get; set; } = ""; // JSON array of {title,url}
    public string Libraries { get; set; } = "";     // comma separated
    public string ScreenshotPath { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Story
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string AzubiName { get; set; } = "";
    public string Year { get; set; } = "";
    public string Title { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public string Content { get; set; } = "";
    public string ContentEn { get; set; } = "";
    public List<CodeExample> CodeExamples { get; set; } = new();
}

public class CodeExample
{
    public int Id { get; set; }
    public int StoryId { get; set; }
    public string Title { get; set; } = "";
    public string Language { get; set; } = "csharp";
    public string Code { get; set; } = "";
    public string Explanation { get; set; } = "";
    public string ExplanationEn { get; set; } = "";
}

public class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public string Content { get; set; } = "";
    public string ContentEn { get; set; } = "";
    public AzubiGroup? Group { get; set; }
    public string Category { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Comment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string AuthorName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class DashboardStats
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FITasks { get; set; }
    public int KDMTasks { get; set; }
    public int FICompleted { get; set; }
    public int KDMCompleted { get; set; }
    public Dictionary<Department, int> TasksByDepartment { get; set; } = new();
    public Dictionary<Difficulty, int> TasksByDifficulty { get; set; } = new();
    public double CompletionRate => TotalTasks == 0 ? 0 : (double)CompletedTasks / TotalTasks * 100;
}

public class AppSettings
{
    public bool IsDarkMode { get; set; } = false;
    public string Language { get; set; } = "DE";
}
