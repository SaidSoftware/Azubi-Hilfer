using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using AzubiHilfer.Data;
using AzubiHilfer.Models;
using AzubiHilfer.Services;

namespace AzubiHilfer.Views;

public partial class MainWindow : Window
{
    private readonly DatabaseService _db;
    private readonly LocalizationService _loc = LocalizationService.Instance;

    private AzubiGroup _currentGroup = AzubiGroup.FachinformatikerAnwendungsentwicklung;
    private Models.Task? _selectedTask;
    private string _currentPanel = "dashboard";
    private bool _isEditMode = false;
    private int _editTaskId = -1;

    private static readonly Department[] Departments = Enum.GetValues<Department>();
    private static readonly Dictionary<Department, (string Icon, Color Color)> DeptInfo = new()
    {
        [Department.Support]          = ("🎧", Color.FromRgb(0x3B,0x82,0xF6)),
        [Department.Entwicklung]      = ("💻", Color.FromRgb(0x8B,0x5C,0xF6)),
        [Department.Schnittstelle]    = ("🔗", Color.FromRgb(0x06,0xB6,0xD4)),
        [Department.Vertragsdatenbank]= ("📋", Color.FromRgb(0xF5,0x9E,0x0B)),
        [Department.Verwaltung]       = ("🏛", Color.FromRgb(0x10,0xB9,0x81)),
        [Department.Azubi]            = ("🎓", Color.FromRgb(0xEC,0x48,0x99)),
        [Department.Technik]          = ("⚙",  Color.FromRgb(0x6B,0x72,0x80)),
        [Department.Vertrieb]         = ("📊", Color.FromRgb(0xEF,0x44,0x44)),
        [Department.Kundenbetreung]   = ("🤝", Color.FromRgb(0x14,0xB8,0xA6)),
        [Department.Report]           = ("📈", Color.FromRgb(0xF9,0x73,0x16)),
    };

    // ── INIT ────────────────────────────────────────────────────────────────

    public MainWindow()
    {
        InitializeComponent();
        _db = new DatabaseService();
        _loc.PropertyChanged += (_, _) => ApplyThemeAndLocale();
        ApplyThemeAndLocale();
        ShowDashboard();
        SetNav(btnDashboard);
    }

    // ── THEME + LOCALE ──────────────────────────────────────────────────────

    private void ApplyThemeAndLocale()
    {
        bool dk = _loc.DarkMode;

        // Window
        Background = _loc.BgBrush;

        // Sidebar
        SidebarBorder.Background = _loc.SidebarBrush;
        SidebarHeader.BorderBrush = dk ? new SolidColorBrush(Color.FromRgb(0x1A,0x1A,0x30)) : new SolidColorBrush(Color.FromRgb(0x1A,0x34,0x60));

        // Search box
        SearchBorder.Background = dk ? new SolidColorBrush(Color.FromRgb(0x1A,0x1A,0x2C)) : new SolidColorBrush(Color.FromRgb(0x1A,0x34,0x60));
        SearchBorder.BorderBrush = dk ? new SolidColorBrush(Color.FromRgb(0x2D,0x2D,0x44)) : new SolidColorBrush(Color.FromRgb(0x2E,0x50,0x90));
        tbSearch.Foreground = dk ? new SolidColorBrush(Color.FromRgb(0xB0,0xC4,0xDE)) : new SolidColorBrush(Color.FromRgb(0xB0,0xC4,0xDE));

        // Content background
        ContentGrid.Background = _loc.BgBrush;

        // Panel headers
        foreach (var hdr in new[] { TaskListHeader, DetailHeader, DeptsHeader, DocsHeader, SearchHeader })
        {
            if (hdr == null) continue;
            hdr.Background = _loc.HeaderBrush;
            hdr.BorderBrush = _loc.BorderBrush2;
        }

        // Doc list
        DocListBorder.Background = _loc.HeaderBrush;
        DocListBorder.BorderBrush = _loc.BorderBrush2;

        // Overlays
        AddTaskCard.Background = _loc.CardBrush;
        StoryCard.Background   = _loc.CardBrush;
        ExportCard.Background  = _loc.CardBrush;

        // TextBlock colors in detail
        txtDashSub.Foreground   = _loc.TextMutedBrush;
        txtSubtitle.Foreground  = _loc.TextMutedBrush;
        txtTaskCount.Foreground = _loc.TextMutedBrush;

        // moon button highlight
        btnDark.Content = dk ? "☀️" : "🌙";
        btnDark.Foreground = dk ? Brushes.Yellow : new SolidColorBrush(Color.FromRgb(0xB0,0xC4,0xDE));

        // Update text
        txtSubtitle.Text     = _loc.AppSubtitle;
        txtFILabel.Text      = _loc.Nav_Tasks;
        txtKDMLabel.Text     = _loc.Nav_Tasks;
        txtDocsLabel.Text    = _loc.Nav_AllDocs;
        txtDeptsLabel.Text   = _loc.IsDE ? "Übersicht" : "Overview";
        txtNavDashboard.Text = "Dashboard";
        tbSearch.Tag         = _loc.Label_Search_Placeholder;

        // Refresh current panel
        RefreshCurrentPanel();
    }

    private void RefreshCurrentPanel()
    {
        switch (_currentPanel)
        {
            case "dashboard":   ShowDashboard(); break;
            case "tasklist":    ShowTaskList(_currentGroup); break;
            case "taskdetail"  when _selectedTask != null: ShowTaskDetail(_selectedTask); break;
            case "departments": ShowDepartments(); break;
            case "documents":   ShowDocuments(); break;
            case "search":      DoSearch(tbSearch.Text); break;
        }
    }

    private void BtnDE_Click(object s, RoutedEventArgs e) => _loc.Language = AppLanguage.DE;
    private void BtnEN_Click(object s, RoutedEventArgs e) => _loc.Language = AppLanguage.EN;
    private void BtnDark_Click(object s, RoutedEventArgs e) => _loc.DarkMode = !_loc.DarkMode;

    // ── SEARCH ──────────────────────────────────────────────────────────────

    private void TbSearch_TextChanged(object s, TextChangedEventArgs e)
    {
        var q = tbSearch.Text.Trim();
        if (q.Length >= 2)
            DoSearch(q);
        else if (q.Length == 0 && _currentPanel == "search")
            ShowDashboard();
    }

    private void DoSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        _currentPanel = "search";
        ShowPanel("search");

        var allTasks = _db.GetAllTasks();
        var ql = query.ToLower();
        var results = allTasks.Where(t =>
            (_loc.IsDE ? t.Title : t.TitleEn).Contains(ql, StringComparison.OrdinalIgnoreCase) ||
            (_loc.IsDE ? t.Description : t.DescriptionEn).Contains(ql, StringComparison.OrdinalIgnoreCase) ||
            t.Tags.Any(tag => tag.Contains(ql, StringComparison.OrdinalIgnoreCase)) ||
            t.AuthorName.Contains(ql, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        txtSearchTitle.Text = _loc.IsDE
            ? $"🔍 Suche: \"{query}\"  —  {results.Count} Ergebnis(se)"
            : $"🔍 Search: \"{query}\"  —  {results.Count} result(s)";
        txtSearchTitle.Foreground = _loc.TextBrush;

        SearchResultsContainer.Children.Clear();
        if (!results.Any())
        {
            SearchResultsContainer.Children.Add(new TextBlock
            {
                Text = _loc.IsDE ? "Keine Ergebnisse gefunden." : "No results found.",
                FontSize = 15, Foreground = _loc.TextMutedBrush, Margin = new Thickness(0, 20, 0, 0)
            });
        }
        else
        {
            foreach (var t in results)
                SearchResultsContainer.Children.Add(CreateTaskCard(t));
        }
    }

    // ── NAV ─────────────────────────────────────────────────────────────────

    private void SetNav(Button sel)
    {
        foreach (var b in new[] { btnDashboard, btnFI, btnKDM, btnDepts, btnDocs })
            b.Style = (Style)FindResource("SidebarNavItem");
        sel.Style = (Style)FindResource("SidebarNavItemSelected");
    }

    private void ShowPanel(string name)
    {
        PanelSearch.Visibility     = Visibility.Collapsed;
        PanelDashboard.Visibility  = Visibility.Collapsed;
        PanelTaskList.Visibility   = Visibility.Collapsed;
        PanelTaskDetail.Visibility = Visibility.Collapsed;
        PanelDepts.Visibility      = Visibility.Collapsed;
        PanelDocs.Visibility       = Visibility.Collapsed;
        _currentPanel = name;
        switch (name)
        {
            case "search":     PanelSearch.Visibility     = Visibility.Visible; break;
            case "dashboard":  PanelDashboard.Visibility  = Visibility.Visible; break;
            case "tasklist":   PanelTaskList.Visibility   = Visibility.Visible; break;
            case "taskdetail": PanelTaskDetail.Visibility = Visibility.Visible; break;
            case "departments":PanelDepts.Visibility      = Visibility.Visible; break;
            case "documents":  PanelDocs.Visibility       = Visibility.Visible; break;
        }
    }

    private void BtnDashboard_Click(object s, RoutedEventArgs e) { SetNav(btnDashboard); ShowDashboard(); }
    private void BtnFI_Click(object s, RoutedEventArgs e)        { SetNav(btnFI);        ShowTaskList(AzubiGroup.FachinformatikerAnwendungsentwicklung); }
    private void BtnKDM_Click(object s, RoutedEventArgs e)       { SetNav(btnKDM);       ShowTaskList(AzubiGroup.KaufmannDigitalisierungsmanagement); }
    private void BtnDepts_Click(object s, RoutedEventArgs e)     { SetNav(btnDepts);     ShowDepartments(); }
    private void BtnDocs_Click(object s, RoutedEventArgs e)      { SetNav(btnDocs);      ShowDocuments(); }

    // ── DASHBOARD ───────────────────────────────────────────────────────────

    private void ShowDashboard()
    {
        ShowPanel("dashboard");
        var stats = _db.GetStats();

        txtDashTitle.Text = "📊 Dashboard";
        txtDashTitle.Foreground = _loc.TextBrush;
        txtDashSub.Text = _loc.IsDE ? "Dein Lernfortschritt auf einen Blick" : "Your learning progress at a glance";

        BuildStatCard(StatCard1, "📚", stats.TotalTasks.ToString(), _loc.IsDE ? "Aufgaben gesamt" : "Total Tasks", Color.FromRgb(0x3B,0x82,0xF6));
        BuildStatCard(StatCard2, "✅", stats.CompletedTasks.ToString(), _loc.IsDE ? "Abgeschlossen" : "Completed", Color.FromRgb(0x10,0xB9,0x81));
        BuildStatCard(StatCard3, "⏳", (stats.TotalTasks - stats.CompletedTasks).ToString(), _loc.IsDE ? "Noch offen" : "Still open", Color.FromRgb(0xF5,0x9E,0x0B));
        BuildStatCard(StatCard4, "🎯", stats.TotalTasks > 0 ? $"{stats.CompletionRate:0}%" : "0%", _loc.IsDE ? "Fortschritt" : "Progress", Color.FromRgb(0x8B,0x5C,0xF6));

        txtProgressTitle.Text = _loc.IsDE ? "Fortschritt nach Gruppe" : "Progress by Group";
        txtProgressTitle.Foreground = _loc.TextBrush;
        ProgressBarsContainer.Children.Clear();
        AddProgressBar(ProgressBarsContainer, "FI Anwendungsentwicklung", stats.FICompleted, stats.FITasks, Color.FromRgb(0x3B,0x82,0xF6));
        AddProgressBar(ProgressBarsContainer, "Kaufmann Digitalisierung",  stats.KDMCompleted, stats.KDMTasks, Color.FromRgb(0x10,0xB9,0x81));
        AddProgressBar(ProgressBarsContainer, _loc.IsDE ? "Gesamt" : "Total", stats.CompletedTasks, stats.TotalTasks, Color.FromRgb(0x8B,0x5C,0xF6));

        txtDiffTitle.Text = _loc.IsDE ? "Nach Schwierigkeitsgrad" : "By Difficulty";
        txtDiffTitle.Foreground = _loc.TextBrush;
        DifficultyContainer.Children.Clear();
        var dc = new[] { Color.FromRgb(0x10,0xB9,0x81), Color.FromRgb(0xF5,0x9E,0x0B), Color.FromRgb(0xEF,0x44,0x44) };
        var dn = _loc.IsDE ? new[] { "🟢 Anfänger", "🟡 Mittel", "🔴 Fortgeschritten" } : new[] { "🟢 Beginner", "🟡 Intermediate", "🔴 Advanced" };
        int di = 0;
        foreach (Difficulty d in Enum.GetValues<Difficulty>())
            AddDiffRow(DifficultyContainer, dn[di++], stats.TasksByDifficulty.GetValueOrDefault(d), stats.TotalTasks, dc[di - 1]);

        txtDashDeptTitle.Text = _loc.IsDE ? "Aufgaben nach Abteilung" : "Tasks by Department";
        txtDashDeptTitle.Foreground = _loc.TextBrush;
        DashDeptContainer.Children.Clear();
        foreach (Department d in Enum.GetValues<Department>())
        {
            int cnt = stats.TasksByDepartment.GetValueOrDefault(d);
            if (cnt == 0) continue;
            var info = DeptInfo[d];
            var mini = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, info.Color.R, info.Color.G, info.Color.B)),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(12,8,12,8),
                Margin = new Thickness(0,0,10,10),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, info.Color.R, info.Color.G, info.Color.B)),
                BorderThickness = new Thickness(1)
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(Tb(info.Icon + " ", 14, null, new Thickness(0,0,6,0)));
            sp.Children.Add(Tb(d.ToString() + " ", 12.5, new SolidColorBrush(info.Color), fontWeight: FontWeights.SemiBold));
            sp.Children.Add(new Border
            {
                Background = new SolidColorBrush(info.Color), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(7,2,7,2),
                Child = Tb(cnt.ToString(), 11, Brushes.White, fontWeight: FontWeights.Bold)
            });
            mini.Child = sp;
            DashDeptContainer.Children.Add(mini);
        }
    }

    private void BuildStatCard(Border card, string icon, string value, string label, Color accent)
    {
        var sp = new StackPanel { Margin = new Thickness(4) };
        sp.Children.Add(new Border
        {
            Width = 38, Height = 38, CornerRadius = new CornerRadius(10), Margin = new Thickness(0,0,0,10),
            Background = new SolidColorBrush(Color.FromArgb(30, accent.R, accent.G, accent.B)),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = Tb(icon, 18, null, align: HorizontalAlignment.Center, vAlign: VerticalAlignment.Center)
        });
        sp.Children.Add(Tb(value, 26, new SolidColorBrush(accent), fontWeight: FontWeights.Bold));
        sp.Children.Add(Tb(label, 12, _loc.TextMutedBrush, new Thickness(0,2,0,0)));
        card.Background = _loc.CardBrush;
        card.Child = sp;
    }

    private void AddProgressBar(StackPanel p, string label, int done, int total, Color color)
    {
        double pct = total == 0 ? 0 : (double)done / total;
        var sp = new StackPanel { Margin = new Thickness(0,0,0,14) };
        var row = new DockPanel { Margin = new Thickness(0,0,0,5) };
        row.Children.Add(Tb(label, 12.5, _loc.TextBrush));
        var right = Tb($"{done}/{total} ({pct:P0})", 12, _loc.TextMutedBrush);
        right.HorizontalAlignment = HorizontalAlignment.Right;
        DockPanel.SetDock(right, Dock.Right);
        row.Children.Insert(0, right);
        var barTrack = new Border { Height = 9, CornerRadius = new CornerRadius(5), Background = _loc.BorderBrush2 };
        var barFill  = new Border { Height = 9, CornerRadius = new CornerRadius(5), Background = new SolidColorBrush(color), HorizontalAlignment = HorizontalAlignment.Left };
        barTrack.SizeChanged += (_, _) => barFill.Width = Math.Max(0, barTrack.ActualWidth * pct);
        var g = new Grid(); g.Children.Add(barTrack); g.Children.Add(barFill);
        sp.Children.Add(row); sp.Children.Add(g);
        p.Children.Add(sp);
    }

    private void AddDiffRow(StackPanel p, string label, int cnt, int total, Color color)
    {
        double pct = total == 0 ? 0 : (double)cnt / total;
        var row = new DockPanel { Margin = new Thickness(0,0,0,10) };
        var lbl = Tb(label, 12.5, _loc.TextBrush); lbl.Width = 160;
        var num = Tb(cnt.ToString(), 12.5, new SolidColorBrush(color), fontWeight: FontWeights.Bold); num.Width = 24; num.TextAlignment = TextAlignment.Right;
        DockPanel.SetDock(lbl, Dock.Left); DockPanel.SetDock(num, Dock.Right);
        var barTrack = new Border { Height = 8, CornerRadius = new CornerRadius(4), Background = _loc.BorderBrush2 };
        var barFill  = new Border { Height = 8, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(color), HorizontalAlignment = HorizontalAlignment.Left };
        barTrack.SizeChanged += (_, _) => barFill.Width = Math.Max(0, barTrack.ActualWidth * pct);
        var g = new Grid(); g.Children.Add(barTrack); g.Children.Add(barFill);
        row.Children.Add(lbl); row.Children.Add(num); row.Children.Add(g);
        p.Children.Add(row);
    }

    // ── TASK LIST ───────────────────────────────────────────────────────────

    private void ShowTaskList(AzubiGroup group)
    {
        _currentGroup = group;
        ShowPanel("tasklist");
        bool isFI = group == AzubiGroup.FachinformatikerAnwendungsentwicklung;

        txtGroupTitle.Text = isFI
            ? (_loc.IsDE ? "FI Anwendungsentwicklung" : "FI Application Development")
            : (_loc.IsDE ? "Kaufmann Digitalisierungsmanagement" : "KDM");
        txtGroupTitle.Foreground = _loc.TextBrush;
        GroupColorBar.Background = isFI ? new SolidColorBrush(Color.FromRgb(0x3B,0x82,0xF6)) : new SolidColorBrush(Color.FromRgb(0x10,0xB9,0x81));
        txtAddTask.Text = _loc.IsDE ? "Aufgabe hinzufügen" : "Add Task";
        txtFilterLabel.Text = _loc.IsDE ? "Filter:" : "Filter:";
        txtFilterLabel.Foreground = _loc.TextMutedBrush;
        txtResetFilter.Text = _loc.IsDE ? "✕ Reset" : "✕ Reset";

        // Populate filter combos
        var allDiffs = new[] { _loc.IsDE ? "Alle Stufen" : "All Levels", "🟢 " + (_loc.IsDE ? "Anfänger" : "Beginner"), "🟡 " + (_loc.IsDE ? "Mittel" : "Intermediate"), "🔴 " + (_loc.IsDE ? "Fortgeschritten" : "Advanced") };
        cbFilterDiff.ItemsSource = allDiffs; cbFilterDiff.SelectedIndex = 0;

        var deptList = new[] { _loc.IsDE ? "Alle Abteilungen" : "All Depts" }.Concat(Departments.Select(d => d.ToString())).ToArray();
        cbFilterDept.ItemsSource = deptList; cbFilterDept.SelectedIndex = 0;

        var statusList = new[] { _loc.IsDE ? "Alle Status" : "All Status", _loc.IsDE ? "Offen" : "Open", _loc.IsDE ? "Erledigt" : "Done" };
        cbFilterStatus.ItemsSource = statusList; cbFilterStatus.SelectedIndex = 0;

        var tags = new[] { _loc.IsDE ? "Alle Tags" : "All Tags" }.Concat(_db.GetAllTags()).ToArray();
        cbFilterTag.ItemsSource = tags; cbFilterTag.SelectedIndex = 0;

        RefreshTaskList();
    }

    private void FilterChanged(object s, SelectionChangedEventArgs e) => RefreshTaskList();

    private void BtnResetFilter_Click(object s, RoutedEventArgs e)
    {
        cbFilterDiff.SelectedIndex = cbFilterDept.SelectedIndex = cbFilterStatus.SelectedIndex = cbFilterTag.SelectedIndex = 0;
    }

    private void RefreshTaskList()
    {
        var tasks = _db.GetTasksByGroup(_currentGroup);

        // Apply filters
        if (cbFilterDiff.SelectedIndex > 0)
            tasks = tasks.Where(t => (int)t.Difficulty == cbFilterDiff.SelectedIndex - 1).ToList();
        if (cbFilterDept.SelectedIndex > 0)
            tasks = tasks.Where(t => t.Department.HasValue && t.Department.Value == Departments[cbFilterDept.SelectedIndex - 1]).ToList();
        if (cbFilterStatus.SelectedIndex == 1) tasks = tasks.Where(t => !t.IsCompleted).ToList();
        if (cbFilterStatus.SelectedIndex == 2) tasks = tasks.Where(t => t.IsCompleted).ToList();
        if (cbFilterTag.SelectedIndex > 0 && cbFilterTag.SelectedItem is string tag && !tag.StartsWith(_loc.IsDE ? "Alle" : "All"))
            tasks = tasks.Where(t => t.Tags.Any(tg => tg == tag)).ToList();

        int done = tasks.Count(t => t.IsCompleted);
        txtTaskCount.Text = _loc.IsDE ? $"{tasks.Count} Aufgabe(n) • {done} abgeschlossen" : $"{tasks.Count} task(s) • {done} completed";

        TaskListContainer.Children.Clear();
        if (!tasks.Any())
            TaskListContainer.Children.Add(Tb(_loc.IsDE ? "Keine Aufgaben mit diesem Filter." : "No tasks match this filter.", 14, _loc.TextMutedBrush, new Thickness(0,20,0,0)));
        else
            foreach (var t in tasks)
                TaskListContainer.Children.Add(CreateTaskCard(t));
    }

    private UIElement CreateTaskCard(Models.Task task)
    {
        bool isFI = task.Group == AzubiGroup.FachinformatikerAnwendungsentwicklung;
        var groupColor = isFI ? Color.FromRgb(0x3B,0x82,0xF6) : Color.FromRgb(0x10,0xB9,0x81);
        var diffColors = new[] { Color.FromRgb(0x10,0xB9,0x81), Color.FromRgb(0xF5,0x9E,0x0B), Color.FromRgb(0xEF,0x44,0x44) };
        var diffLabels = _loc.IsDE ? new[] { "Anfänger","Mittel","Fortgeschritten" } : new[] { "Beginner","Intermediate","Advanced" };

        var outer = new Border
        {
            Background = _loc.CardBrush, CornerRadius = new CornerRadius(10),
            Margin = new Thickness(0,0,0,11), Cursor = Cursors.Hand, Effect = Shdw(0.07)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new Border { Background = new SolidColorBrush(groupColor), CornerRadius = new CornerRadius(10,0,0,10) });

        var content = new StackPanel { Margin = new Thickness(16,14,12,14) };

        // Title + badges
        content.Children.Add(Tb(_loc.IsDE ? task.Title : task.TitleEn, 14.5, _loc.TextBrush, fontWeight: FontWeights.SemiBold));

        var badges = new WrapPanel { Margin = new Thickness(0,6,0,6) };
        badges.Children.Add(Badge(diffLabels[(int)task.Difficulty], diffColors[(int)task.Difficulty]));
        if (task.Department.HasValue) { var di = DeptInfo[task.Department.Value]; badges.Children.Add(Badge(di.Icon + " " + task.Department.Value, di.Color)); }
        if (!string.IsNullOrEmpty(task.AuthorName)) badges.Children.Add(Badge("👤 " + task.AuthorName, Color.FromRgb(0x6B,0x72,0x80)));
        content.Children.Add(badges);

        var desc = Tb(_loc.IsDE ? task.Description : task.DescriptionEn, 12.5, _loc.TextMutedBrush);
        desc.TextWrapping = TextWrapping.Wrap; desc.MaxHeight = 36; desc.TextTrimming = TextTrimming.CharacterEllipsis;
        desc.Margin = new Thickness(0,0,0,8);
        content.Children.Add(desc);

        var tagWrap = new WrapPanel();
        foreach (var tag in task.Tags.Take(5)) tagWrap.Children.Add(Badge(tag, Color.FromRgb(0x60,0x7B,0xD4), 0.1));
        content.Children.Add(tagWrap);

        Grid.SetColumn(content, 1);
        grid.Children.Add(content);

        // Right: Checkbox
        var checkSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,14,0) };
        bool done = task.IsCompleted;
        var cb = new Border
        {
            Width = 26, Height = 26, CornerRadius = new CornerRadius(13),
            Background = done ? new SolidColorBrush(Color.FromRgb(0x10,0xB9,0x81)) : Brushes.Transparent,
            BorderBrush = done ? new SolidColorBrush(Color.FromRgb(0x10,0xB9,0x81)) : new SolidColorBrush(Color.FromRgb(0xD1,0xD5,0xDB)),
            BorderThickness = new Thickness(2), Cursor = Cursors.Hand,
            Child = Tb(done ? "✓" : "", 13, Brushes.White, align: HorizontalAlignment.Center, vAlign: VerticalAlignment.Center, fontWeight: FontWeights.Bold)
        };
        var capturedTask = task;
        cb.MouseLeftButtonUp += (s, e) =>
        {
            e.Handled = true;
            _db.SetTaskCompleted(capturedTask.Id, !capturedTask.IsCompleted);
            if (_currentPanel == "search") DoSearch(tbSearch.Text);
            else RefreshTaskList();
        };
        checkSp.Children.Add(cb);
        checkSp.Children.Add(Tb(done ? (_loc.IsDE?"Erledigt":"Done") : (_loc.IsDE?"Offen":"Open"), 10,
            done ? new SolidColorBrush(Color.FromRgb(0x10,0xB9,0x81)) : _loc.TextMutedBrush,
            new Thickness(0,3,0,0)));
        Grid.SetColumn(checkSp, 2);
        grid.Children.Add(checkSp);

        outer.Child = grid;
        outer.MouseEnter += (s, e) => outer.Effect = Shdw(0.15);
        outer.MouseLeave += (s, e) => outer.Effect = Shdw(0.07);
        outer.MouseLeftButtonUp += (s, e) => { _selectedTask = capturedTask; ShowTaskDetail(capturedTask); };

        return outer;
    }




    // ── TASK DETAIL ─────────────────────────────────────────────────────────

    private void ShowTaskDetail(Models.Task task)
    {
        ShowPanel("taskdetail");
        DetailScroll.ScrollToTop();

        // Header
        txtBack.Text = _loc.Label_BackToTasks;
        txtDetailTitle.Text = _loc.IsDE ? task.Title : task.TitleEn;
        txtDetailTitle.Foreground = _loc.TextBrush;
        txtEditBtn.Text   = "✏️ " + _loc.Label_Edit;
        txtDeleteBtn.Text = "🗑 " + _loc.Label_Delete;

        DetailContainer.Children.Clear();

        // === INFO CARD ===
        var infoCard = MakeCard();
        var infoSp = new StackPanel();

        var dBadges = new WrapPanel { Margin = new Thickness(0,0,0,12) };
        var dl = _loc.IsDE ? new[]{"🟢 Anfänger","🟡 Mittel","🔴 Fortgeschritten"} : new[]{"🟢 Beginner","🟡 Intermediate","🔴 Advanced"};
        var dc = new[]{Color.FromRgb(0x10,0xB9,0x81),Color.FromRgb(0xF5,0x9E,0x0B),Color.FromRgb(0xEF,0x44,0x44)};
        dBadges.Children.Add(Badge(dl[(int)task.Difficulty], dc[(int)task.Difficulty]));
        if (task.Department.HasValue) { var di = DeptInfo[task.Department.Value]; dBadges.Children.Add(Badge(di.Icon+" "+task.Department.Value, di.Color)); }
        if (!string.IsNullOrEmpty(task.AuthorName)) dBadges.Children.Add(Badge("👤 "+task.AuthorName, Color.FromRgb(0x6B,0x72,0x80)));
        if (task.IsCompleted) dBadges.Children.Add(Badge("✅ "+(_loc.IsDE?"Abgeschlossen":"Completed"), Color.FromRgb(0x10,0xB9,0x81)));
        infoSp.Children.Add(dBadges);

        infoSp.Children.Add(Tb("📋 "+_loc.Label_Description, 13.5, _loc.TextBrush, new Thickness(0,0,0,7), fontWeight: FontWeights.SemiBold));
        infoSp.Children.Add(Tb(_loc.IsDE?task.Description:task.DescriptionEn, 13.5, _loc.TextBrush, lineHeight: 22));

        // Screenshot
        if (!string.IsNullOrEmpty(task.ScreenshotPath) && File.Exists(task.ScreenshotPath))
        {
            infoSp.Children.Add(Tb(_loc.IsDE?"📸 Screenshot:":"📸 Screenshot:", 13, _loc.TextBrush, new Thickness(0,14,0,7), fontWeight: FontWeights.SemiBold));
            try
            {
                var img = new Image
                {
                    Source = new BitmapImage(new Uri(task.ScreenshotPath)),
                    MaxHeight = 260, MaxWidth = 520,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Stretch = Stretch.Uniform,
                };
                var imgBorder = new Border
                {
                    CornerRadius = new CornerRadius(8), ClipToBounds = true,
                    BorderBrush = _loc.BorderBrush2, BorderThickness = new Thickness(1)
                };
                imgBorder.Child = img;
                infoSp.Children.Add(imgBorder);
            }
            catch { }
        }

        // Tags
        if (task.Tags.Any())
        {
            infoSp.Children.Add(Tb(_loc.Label_Tags+":", 12, _loc.TextMutedBrush, new Thickness(0,14,0,6), fontWeight: FontWeights.SemiBold));
            var tagWrap = new WrapPanel();
            foreach (var t in task.Tags) tagWrap.Children.Add(Badge(t, Color.FromRgb(0x60,0x7B,0xD4), 0.1));
            infoSp.Children.Add(tagWrap);
        }

        // Libraries
        if (!string.IsNullOrEmpty(task.Libraries))
            infoSp.Children.Add(Tb("📦 "+(_loc.IsDE?"Bibliotheken: ":"Libraries: ")+task.Libraries, 12.5, _loc.TextMutedBrush, new Thickness(0,12,0,0)));

        // External links
        BuildLinks(infoSp, task.ExternalLinks);

        infoCard.Child = infoSp;
        DetailContainer.Children.Add(infoCard);

        // === ADD STORY BUTTON ===
        var addStoryRow = new DockPanel { Margin = new Thickness(0,0,0,14) };
        addStoryRow.Children.Add(Tb(_loc.Label_Stories, 17, _loc.TextBrush, fontWeight: FontWeights.Bold));
        var addStoryBtn = new Button
        {
            Content = _loc.Label_Stories_Add,
            Style = (Style)FindResource("AccentButton"),
            Padding = new Thickness(12,6,12,6), FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        DockPanel.SetDock(addStoryBtn, Dock.Right);
        addStoryBtn.Click += (s, e) => OpenAddStoryOverlay(task.Id);
        addStoryRow.Children.Insert(0, addStoryBtn);
        DetailContainer.Children.Add(addStoryRow);

        // Stories
        var stories = _db.GetStoriesByTask(task.Id);
        foreach (var story in stories)
            DetailContainer.Children.Add(CreateStoryCard(story));

        // === COMMENTS SECTION ===
        DetailContainer.Children.Add(BuildCommentsSection(task));
    }

    private void BuildLinks(StackPanel parent, string linksJson)
    {
        if (string.IsNullOrEmpty(linksJson)) return;
        try
        {
            var links = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(linksJson);
            if (links == null || !links.Any()) return;
            parent.Children.Add(Tb("🔗 "+(_loc.IsDE?"Hilfreiche Links:":"Helpful Links:"), 13, _loc.TextBrush, new Thickness(0,14,0,8), fontWeight: FontWeights.SemiBold));
            foreach (var link in links)
            {
                if (!link.TryGetValue("url", out var url) || !link.TryGetValue("title", out var title)) continue;
                var lb = new Border
                {
                    Background = _loc.DarkMode ? new SolidColorBrush(Color.FromRgb(0x1A,0x2A,0x44)) : new SolidColorBrush(Color.FromRgb(0xEF,0xF6,0xFF)),
                    CornerRadius = new CornerRadius(6), Padding = new Thickness(12,8,12,8),
                    Margin = new Thickness(0,0,0,6), Cursor = Cursors.Hand,
                    BorderBrush = _loc.DarkMode ? new SolidColorBrush(Color.FromRgb(0x2D,0x4A,0x80)) : new SolidColorBrush(Color.FromRgb(0xBA,0xD4,0xFF)),
                    BorderThickness = new Thickness(1)
                };
                var row = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(Tb("🌐  ", 13));
                row.Children.Add(Tb(title, 13, new SolidColorBrush(Color.FromRgb(0x1D,0x6F,0xE5)), textDecoration: TextDecorations.Underline));
                lb.Child = row;
                var u = url;
                lb.MouseLeftButtonUp += (s, e) => { try { Process.Start(new ProcessStartInfo(u) { UseShellExecute = true }); } catch { } };
                lb.MouseEnter += (s,e) => lb.Opacity = 0.8;
                lb.MouseLeave += (s,e) => lb.Opacity = 1;
                parent.Children.Add(lb);
            }
        }
        catch { }
    }

    private UIElement CreateStoryCard(Story story)
    {
        var card = MakeCard();
        var sp = new StackPanel();

        var hRow = new DockPanel { Margin = new Thickness(0,0,0,10) };
        var nb = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xF0,0xA5,0x00)),
            CornerRadius = new CornerRadius(20), Padding = new Thickness(12,4,12,4),
            Child = Tb($"👤 {story.AzubiName}  •  {story.Year}", 12, Brushes.White, fontWeight: FontWeights.SemiBold)
        };
        DockPanel.SetDock(nb, Dock.Left);
        hRow.Children.Add(nb);
        sp.Children.Add(hRow);
        sp.Children.Add(Tb(_loc.IsDE ? story.Title : story.TitleEn, 15, _loc.TextBrush, new Thickness(0,0,0,7), fontWeight: FontWeights.SemiBold));
        sp.Children.Add(Tb(_loc.IsDE ? story.Content : story.ContentEn, 13.5, _loc.TextMutedBrush, new Thickness(0,0,0,12), lineHeight: 22));
        foreach (var ce in story.CodeExamples) sp.Children.Add(CreateCodeBlock(ce));
        card.Child = sp;
        return card;
    }

    private UIElement CreateCodeBlock(CodeExample ex)
    {
        var outer = new Border { BorderBrush = _loc.BorderBrush2, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Margin = new Thickness(0,0,0,12), ClipToBounds = true };
        var sp = new StackPanel();

        var hdr = new Border { Background = new SolidColorBrush(Color.FromRgb(0x2A,0x2A,0x3E)), Padding = new Thickness(14,9,14,9) };
        var hDock = new DockPanel();
        var btnCopy = new Button { Style = (Style)FindResource("AccentButton"), Padding = new Thickness(10,4,10,4), FontSize = 11, Content = _loc.Label_CopyCode };
        DockPanel.SetDock(btnCopy, Dock.Right);
        var capturedCode = ex.Code;
        btnCopy.Click += async (s, e) => { Clipboard.SetText(capturedCode); btnCopy.Content = _loc.Label_Copied; await System.Threading.Tasks.Task.Delay(2000); btnCopy.Content = _loc.Label_CopyCode; };
        var tr = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        tr.Children.Add(new Border { Width=8,Height=8,CornerRadius=new CornerRadius(4),Background=new SolidColorBrush(Color.FromRgb(0xF0,0xA5,0x00)),Margin=new Thickness(0,0,8,0),VerticalAlignment=VerticalAlignment.Center });
        tr.Children.Add(Tb(ex.Title, 12.5, Brushes.White, fontWeight: FontWeights.SemiBold));
        tr.Children.Add(new Border { Background=new SolidColorBrush(Color.FromRgb(0x3A,0x3A,0x5E)),CornerRadius=new CornerRadius(3),Padding=new Thickness(6,2,6,2),Margin=new Thickness(8,0,0,0),Child=Tb(ex.Language.ToUpper(),10,new SolidColorBrush(Color.FromRgb(0x9A,0xB0,0xD4))) });
        hDock.Children.Add(btnCopy); hDock.Children.Add(tr);
        hdr.Child = hDock;

        var codeBox = new TextBox
        {
            Text = ex.Code,
            Background=new SolidColorBrush(Color.FromRgb(0x1E,0x1E,0x2E)),
            Foreground=new SolidColorBrush(Color.FromRgb(0xCD,0xD6,0xF4)),
            FontFamily=new FontFamily("Cascadia Code,Consolas,Courier New"),
            FontSize=12.5, IsReadOnly=true, BorderThickness=new Thickness(0),
            Padding=new Thickness(16), TextWrapping=TextWrapping.NoWrap,
            HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,
            AcceptsReturn=true, MaxHeight=360
        };

        sp.Children.Add(hdr);
        sp.Children.Add(codeBox);

        if (!string.IsNullOrEmpty(ex.Explanation))
        {
            var eBorder = new Border
            {
                Background = _loc.DarkMode ? new SolidColorBrush(Color.FromRgb(0x0E,0x2A,0x44)) : new SolidColorBrush(Color.FromRgb(0xF0,0xF9,0xFF)),
                Padding = new Thickness(14,10,14,10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xBA,0xE6,0xFD)), BorderThickness = new Thickness(0,1,0,0)
            };
            var eRow = new StackPanel { Orientation = Orientation.Horizontal };
            eRow.Children.Add(Tb("💡 ", 13, null, new Thickness(0,1,6,0), vAlign: VerticalAlignment.Top));
            eRow.Children.Add(Tb(_loc.IsDE ? ex.Explanation : ex.ExplanationEn, 12.5, new SolidColorBrush(Color.FromRgb(0x0E,0x4F,0x7A)), lineHeight: 20));
            eBorder.Child = eRow;
            sp.Children.Add(eBorder);
        }

        outer.Child = sp;
        return outer;
    }

    // === COMMENTS ===

    private UIElement BuildCommentsSection(Models.Task task)
    {
        var card = MakeCard();
        var sp = new StackPanel();
        sp.Children.Add(Tb(_loc.Label_Comments, 15, _loc.TextBrush, new Thickness(0,0,0,14), fontWeight: FontWeights.Bold));

        var commentsSp = new StackPanel();
        void LoadComments()
        {
            commentsSp.Children.Clear();
            var comments = _db.GetCommentsByTask(task.Id);
            if (!comments.Any())
                commentsSp.Children.Add(Tb(_loc.IsDE ? "Noch keine Kommentare." : "No comments yet.", 13, _loc.TextMutedBrush, new Thickness(0,0,0,12), FontStyles.Italic));
            else
                foreach (var c in comments)
                {
                    var cBorder = new Border
                    {
                        Background = _loc.DarkMode ? new SolidColorBrush(Color.FromRgb(0x1A,0x1A,0x2C)) : new SolidColorBrush(Color.FromRgb(0xF9,0xFA,0xFB)),
                        CornerRadius = new CornerRadius(8), Padding = new Thickness(14,10,14,10),
                        Margin = new Thickness(0,0,0,8),
                        BorderBrush = _loc.BorderBrush2, BorderThickness = new Thickness(1)
                    };
                    var cGrid = new Grid();
                    cGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    cGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var cContent = new StackPanel();
                    cContent.Children.Add(Tb($"👤 {c.AuthorName}  •  {c.CreatedAt:dd.MM.yyyy HH:mm}", 11, _loc.TextMutedBrush, new Thickness(0,0,0,4)));
                    cContent.Children.Add(Tb(c.Text, 13, _loc.TextBrush, lineHeight: 20));
                    Grid.SetColumn(cContent, 0);
                    var capturedId = c.Id;
                    var delBtn = new Button
                    {
                        Content = "🗑", Style = (Style)FindResource("GhostButton"),
                        Padding = new Thickness(6,4,6,4), FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    delBtn.Click += (s, e) => { _db.DeleteComment(capturedId); LoadComments(); };
                    Grid.SetColumn(delBtn, 1);
                    cGrid.Children.Add(cContent); cGrid.Children.Add(delBtn);
                    cBorder.Child = cGrid;
                    commentsSp.Children.Add(cBorder);
                }
        }
        LoadComments();
        sp.Children.Add(commentsSp);

        // Add comment
        var divider = new Border { Height = 1, Background = _loc.BorderBrush2, Margin = new Thickness(0,4,0,12) };
        sp.Children.Add(divider);

        var authorBox = new TextBox { Style = (Style)FindResource("FormInput"), Margin = new Thickness(0,0,0,8) };
        authorBox.Tag = _loc.IsDE ? "Dein Name" : "Your name";
        var commentBox = new TextBox
        {
            Style = (Style)FindResource("FormInput"), Height = 64,
            TextWrapping = TextWrapping.Wrap, AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0,0,0,8)
        };
        commentBox.Tag = _loc.Label_AddComment;

        var submitBtn = new Button { Style = (Style)FindResource("AccentButton"), Padding = new Thickness(16,7,16,7), HorizontalAlignment = HorizontalAlignment.Right };
        submitBtn.Content = _loc.Label_Submit;
        submitBtn.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(commentBox.Text)) return;
            _db.AddComment(task.Id, string.IsNullOrWhiteSpace(authorBox.Text) ? "Azubi" : authorBox.Text, commentBox.Text);
            commentBox.Text = ""; authorBox.Text = "";
            LoadComments();
        };

        sp.Children.Add(Tb(_loc.IsDE?"Name:":"Name:", 12, _loc.TextMutedBrush, new Thickness(0,0,0,4), fontWeight: FontWeights.SemiBold));
        sp.Children.Add(authorBox);
        sp.Children.Add(Tb(_loc.IsDE?"Kommentar:":"Comment:", 12, _loc.TextMutedBrush, new Thickness(0,0,0,4), fontWeight: FontWeights.SemiBold));
        sp.Children.Add(commentBox);
        sp.Children.Add(submitBtn);

        card.Child = sp;
        return card;
    }

    private void BtnBack_Click(object s, RoutedEventArgs e)
    {
        ShowTaskList(_currentGroup);
        SetNav(_currentGroup == AzubiGroup.FachinformatikerAnwendungsentwicklung ? btnFI : btnKDM);
    }

    // ── EDIT / DELETE TASK ──────────────────────────────────────────────────

    private void BtnEditTask_Click(object s, RoutedEventArgs e)
    {
        if (_selectedTask == null) return;
        _isEditMode = true;
        _editTaskId = _selectedTask.Id;
        var t = _selectedTask;

        txtOverlayTitle.Text = _loc.IsDE ? "Aufgabe bearbeiten" : "Edit Task";
        txtOverlayTitle.Foreground = _loc.TextBrush;
        tbTitle.Text   = t.Title;
        tbTitleEn.Text = t.TitleEn;
        tbDesc.Text    = t.Description;
        cbGroup.SelectedIndex  = (int)t.Group;
        cbDiff.SelectedIndex   = (int)t.Difficulty;
        cbDept.SelectedIndex   = t.Department.HasValue ? (int)t.Department.Value : 1;
        tbAuthor.Text  = t.AuthorName;
        tbTags.Text    = string.Join(",", t.Tags);
        tbLibs.Text    = t.Libraries;
        tbScreenshot.Text = t.ScreenshotPath;

        // Reconstruct links text
        try
        {
            var links = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(t.ExternalLinks) ?? new();
            tbLinks.Text = string.Join("\n", links.Select(l => l.GetValueOrDefault("url", "")));
        }
        catch { tbLinks.Text = ""; }

        OverlayAddTask.Visibility = Visibility.Visible;
    }

    private void BtnDeleteTask_Click(object s, RoutedEventArgs e)
    {
        if (_selectedTask == null) return;
        var result = MessageBox.Show(
            _loc.IsDE ? $"Aufgabe \"{_selectedTask.Title}\" wirklich löschen?" : $"Really delete task \"{_selectedTask.Title}\"?",
            "Azubi-Hilfer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        _db.DeleteTask(_selectedTask.Id);
        ShowTaskList(_currentGroup);
        SetNav(_currentGroup == AzubiGroup.FachinformatikerAnwendungsentwicklung ? btnFI : btnKDM);
    }

    // ── ADD / EDIT TASK OVERLAY ─────────────────────────────────────────────

    private void BtnAddTask_Click(object s, RoutedEventArgs e)
    {
        _isEditMode = false; _editTaskId = -1;
        txtOverlayTitle.Text = _loc.IsDE ? "Neue Aufgabe erstellen" : "Create New Task";
        txtOverlayTitle.Foreground = _loc.TextBrush;
        tbTitle.Text = tbTitleEn.Text = tbDesc.Text = tbAuthor.Text = tbTags.Text = tbLibs.Text = tbLinks.Text = tbScreenshot.Text = "";
        cbGroup.SelectedIndex = _currentGroup == AzubiGroup.FachinformatikerAnwendungsentwicklung ? 0 : 1;
        cbDiff.SelectedIndex = cbDept.SelectedIndex = 0;
        OverlayAddTask.Visibility = Visibility.Visible;
    }

    private void BtnCloseOverlay_Click(object s, RoutedEventArgs e) => OverlayAddTask.Visibility = Visibility.Collapsed;

    private void BtnPickScreenshot_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Bilder|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp", Title = "Screenshot wählen" };
        if (dlg.ShowDialog() == true) tbScreenshot.Text = dlg.FileName;
    }

    private void BtnSaveTask_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(tbTitle.Text))
        {
            MessageBox.Show(_loc.IsDE ? "Bitte Titel eingeben!" : "Please enter a title!", "Azubi-Hilfer", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var deptMap = Departments;
        var linksJson = BuildLinksJson(tbLinks.Text);

        var task = new Models.Task
        {
            Title         = tbTitle.Text.Trim(),
            TitleEn       = string.IsNullOrWhiteSpace(tbTitleEn.Text) ? tbTitle.Text.Trim() : tbTitleEn.Text.Trim(),
            Description   = tbDesc.Text.Trim(),
            DescriptionEn = tbDesc.Text.Trim(),
            Group         = cbGroup.SelectedIndex == 0 ? AzubiGroup.FachinformatikerAnwendungsentwicklung : AzubiGroup.KaufmannDigitalisierungsmanagement,
            Difficulty    = (Difficulty)cbDiff.SelectedIndex,
            Department    = deptMap[Math.Clamp(cbDept.SelectedIndex, 0, deptMap.Length - 1)],
            AuthorName    = tbAuthor.Text.Trim(),
            Tags          = tbTags.Text.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList(),
            Libraries     = tbLibs.Text.Trim(),
            ExternalLinks = linksJson,
            ScreenshotPath = tbScreenshot.Text.Trim(),
        };

        if (_isEditMode && _editTaskId > 0)
        {
            task.Id = _editTaskId;
            _db.UpdateTask(task);
        }
        else
        {
            _db.AddTask(task);
        }

        OverlayAddTask.Visibility = Visibility.Collapsed;
        _currentGroup = task.Group;
        SetNav(task.Group == AzubiGroup.FachinformatikerAnwendungsentwicklung ? btnFI : btnKDM);
        ShowTaskList(task.Group);
    }

    // ── ADD STORY OVERLAY ───────────────────────────────────────────────────

    private int _storyTaskId = -1;

    private void OpenAddStoryOverlay(int taskId)
    {
        _storyTaskId = taskId;
        txtStoryOverlayTitle.Text = _loc.IsDE ? "Lösungsweg hinzufügen" : "Add Solution Story";
        txtStoryOverlayTitle.Foreground = _loc.TextBrush;
        tbStoryAuthor.Text = tbStoryTitle.Text = tbStoryTitleEn.Text = tbStoryContent.Text = tbStoryContentEn.Text = "";
        tbCodeTitle.Text = tbCode.Text = tbCodeExpl.Text = "";
        tbStoryYear.Text = DateTime.Now.Year.ToString();
        cbCodeLang.SelectedIndex = 0;
        OverlayStory.Visibility = Visibility.Visible;
    }

    private void BtnCloseStory_Click(object s, RoutedEventArgs e) => OverlayStory.Visibility = Visibility.Collapsed;

    private void BtnSaveStory_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(tbStoryContent.Text))
        {
            MessageBox.Show(_loc.IsDE ? "Bitte Beschreibung eingeben!" : "Please enter a description!", "Azubi-Hilfer", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var storyId = _db.AddStory(_storyTaskId,
            string.IsNullOrWhiteSpace(tbStoryAuthor.Text) ? "Azubi" : tbStoryAuthor.Text.Trim(),
            tbStoryYear.Text.Trim(),
            tbStoryTitle.Text.Trim(), tbStoryTitleEn.Text.Trim(),
            tbStoryContent.Text.Trim(), tbStoryContentEn.Text.Trim());

        if (!string.IsNullOrWhiteSpace(tbCode.Text))
        {
            var lang = (cbCodeLang.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "csharp";
            _db.AddCodeExample((int)storyId, tbCodeTitle.Text.Trim(), lang, tbCode.Text, tbCodeExpl.Text.Trim(), tbCodeExpl.Text.Trim());
        }

        OverlayStory.Visibility = Visibility.Collapsed;
        if (_selectedTask != null) ShowTaskDetail(_selectedTask);
    }

    // ── EXPORT ──────────────────────────────────────────────────────────────

    private void BtnExport_Click(object s, RoutedEventArgs e)
    {
        txtExportTitle.Text = _loc.Label_Export;
        txtExportTitle.Foreground = _loc.TextBrush;
        txtExportWhat.Text = _loc.IsDE ? "Alle Aufgaben exportieren:" : "Export all tasks:";
        txtExportWhat.Foreground = _loc.TextMutedBrush;
        txtExportPdf.Text    = _loc.IsDE ? "Als PDF exportieren" : "Export as PDF";
        txtExportPdfSub.Text = _loc.IsDE ? "Alle Aufgaben als PDF-Dokument" : "All tasks as PDF document";
        txtExportCsv.Text    = _loc.IsDE ? "Als CSV exportieren" : "Export as CSV";
        txtExportCsvSub.Text = _loc.IsDE ? "Für Excel, LibreOffice Calc etc." : "For Excel, LibreOffice Calc etc.";
        txtExportClose.Text  = _loc.IsDE ? "Schließen" : "Close";
        ExportCard.Background = _loc.CardBrush;
        OverlayExport.Visibility = Visibility.Visible;
    }

    private void BtnCloseExport_Click(object s, RoutedEventArgs e) => OverlayExport.Visibility = Visibility.Collapsed;

    private void BtnExportPdf_Click(object s, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "PDF|*.pdf", FileName = "AzubiHilfer_Aufgaben.pdf", Title = "PDF speichern" };
        if (dlg.ShowDialog() != true) return;

        var tasks = _db.GetAllTasks();
        var sb = new StringBuilder();

        // Build an HTML file that can be printed to PDF (simple approach without PDF library)
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>body{font-family:Segoe UI,Arial;max-width:900px;margin:30px auto;color:#1a1a2e}");
        sb.AppendLine("h1{color:#1e3a5f;border-bottom:3px solid #f0a500;padding-bottom:8px}");
        sb.AppendLine("h2{color:#1e3a5f;margin-top:30px}.badge{display:inline-block;background:#eef2ff;color:#3b82f6;padding:2px 8px;border-radius:4px;font-size:11px;margin:2px}");
        sb.AppendLine(".completed{color:#10b981}.open{color:#f59e0b}");
        sb.AppendLine("pre{background:#1e1e2e;color:#cdd6f4;padding:14px;border-radius:6px;overflow-x:auto;font-size:12px}");
        sb.AppendLine(".card{border:1px solid #e5e7eb;border-radius:8px;padding:16px;margin:16px 0}</style></head><body>");
        sb.AppendLine($"<h1>🎓 Azubi-Hilfer — Export {DateTime.Now:dd.MM.yyyy}</h1>");
        sb.AppendLine($"<p>Gesamt: {tasks.Count} Aufgaben • Abgeschlossen: {tasks.Count(t => t.IsCompleted)}</p>");

        foreach (var t in tasks)
        {
            sb.AppendLine($"<div class='card'><h2>{t.Title}</h2>");
            sb.AppendLine($"<span class='{(t.IsCompleted?"completed":"open")}'>{(t.IsCompleted?"✅ Abgeschlossen":"⏳ Offen")}</span>");
            if (!string.IsNullOrEmpty(t.AuthorName)) sb.AppendLine($" &nbsp; 👤 {t.AuthorName}");
            sb.AppendLine($"<p>{t.Description}</p>");
            foreach (var tag in t.Tags) sb.AppendLine($"<span class='badge'>{tag}</span>");
            if (!string.IsNullOrEmpty(t.Libraries)) sb.AppendLine($"<p>📦 {t.Libraries}</p>");
            var stories = _db.GetStoriesByTask(t.Id);
            foreach (var story in stories)
            {
                sb.AppendLine($"<h3>👤 {story.AzubiName} ({story.Year}) — {story.Title}</h3>");
                sb.AppendLine($"<p>{story.Content}</p>");
                foreach (var ce in story.CodeExamples) sb.AppendLine($"<h4>{ce.Title}</h4><pre>{System.Net.WebUtility.HtmlEncode(ce.Code)}</pre><p>💡 {ce.Explanation}</p>");
            }
            sb.AppendLine("</div>");
        }
        sb.AppendLine("</body></html>");

        // Save as HTML (can be opened in browser and printed as PDF)
        var htmlPath = dlg.FileName.Replace(".pdf", ".html");
        File.WriteAllText(htmlPath, sb.ToString(), Encoding.UTF8);

        MessageBox.Show(
            _loc.IsDE
                ? $"Als HTML exportiert (Browser öffnen → Drucken → PDF):\n{htmlPath}"
                : $"Exported as HTML (open in browser → Print → PDF):\n{htmlPath}",
            "Export", MessageBoxButton.OK, MessageBoxImage.Information);

        try { Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true }); } catch { }
        OverlayExport.Visibility = Visibility.Collapsed;
    }

    private void BtnExportCsv_Click(object s, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "AzubiHilfer_Aufgaben.csv", Title = "CSV speichern" };
        if (dlg.ShowDialog() != true) return;

        var tasks = _db.GetAllTasks();
        var sb = new StringBuilder();
        sb.AppendLine("Id;Titel;Gruppe;Schwierigkeit;Abteilung;Autor;Tags;Bibliotheken;Erledigt;Erstellt");
        foreach (var t in tasks)
        {
            var diff = new[]{"Anfänger","Mittel","Fortgeschritten"}[(int)t.Difficulty];
            sb.AppendLine($"{t.Id};\"{t.Title}\";\"{t.Group}\";\"{diff}\";\"{t.Department}\";\"{t.AuthorName}\";\"{string.Join("|",t.Tags)}\";\"{t.Libraries}\";\"{(t.IsCompleted?"Ja":"Nein")}\";\"{t.CreatedAt:dd.MM.yyyy}\"");
        }
        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show(_loc.IsDE ? $"CSV gespeichert:\n{dlg.FileName}" : $"CSV saved:\n{dlg.FileName}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        try { Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); } catch { }
        OverlayExport.Visibility = Visibility.Collapsed;
    }

    // ── DEPARTMENTS ─────────────────────────────────────────────────────────

    private void ShowDepartments()
    {
        ShowPanel("departments");
        txtDeptsTitle.Text = _loc.IsDE ? "🏢 Abteilungen" : "🏢 Departments";
        txtDeptsTitle.Foreground = _loc.TextBrush;

        var deptDescs = new Dictionary<Department, string>
        {
            [Department.Support]          = _loc.IsDE ? "Erster Ansprechpartner für Kunden, Ticketsystem, SLA-Zeiten" : "First contact for customers, ticket system, SLA times",
            [Department.Entwicklung]      = _loc.IsDE ? "Desktop- und Web-Apps in C#, Delphi, DevExpress" : "Desktop and web apps in C#, Delphi, DevExpress",
            [Department.Schnittstelle]    = _loc.IsDE ? "API-Anbindungen, Online-Portale, REST-Webservices" : "API connections, online portals, REST web services",
            [Department.Vertragsdatenbank]= _loc.IsDE ? "Kundenverwaltung, SQL-Reports, Datenqualität" : "Customer management, SQL reports, data quality",
            [Department.Verwaltung]       = _loc.IsDE ? "Interne Prozesse, HR, Buchhaltung, BPMN-Optimierung" : "Internal processes, HR, accounting, BPMN optimization",
            [Department.Azubi]            = _loc.IsDE ? "Betreuung der Auszubildenden, IHK-Prüfungsvorbereitung" : "Apprentice supervision, IHK exam preparation",
            [Department.Technik]          = _loc.IsDE ? "Server, Netzwerk, IT-Infrastruktur, Monitoring" : "Servers, network, IT infrastructure, monitoring",
            [Department.Vertrieb]         = _loc.IsDE ? "Kundenakquise, CRM-System, Angebotserstellung" : "Customer acquisition, CRM, offer creation",
            [Department.Kundenbetreung]   = _loc.IsDE ? "Langfristige Kundenbeziehungen, Onboarding, Schulungen" : "Long-term customer relations, onboarding, training",
            [Department.Report]           = _loc.IsDE ? "Auswertungen, Dashboards, BI-Tools für das Management" : "Reports, dashboards, BI tools for management",
        };

        var allTasks = _db.GetAllTasks();
        DeptsContainer.Children.Clear();

        foreach (Department dept in Enum.GetValues<Department>())
        {
            var dTasks = allTasks.Where(t => t.Department == dept).ToList();
            var info   = DeptInfo[dept];

            var card = new Border
            {
                Background = _loc.CardBrush, CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0,0,16,16), Width = 285,
                Padding = new Thickness(20,18,20,18), Effect = Shdw(0.07), Cursor = Cursors.Hand
            };

            var sp = new StackPanel();

            var topRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,8) };
            topRow.Children.Add(new Border
            {
                Width = 40, Height = 40, CornerRadius = new CornerRadius(20), Margin = new Thickness(0,0,10,0),
                Background = new SolidColorBrush(Color.FromArgb(30, info.Color.R, info.Color.G, info.Color.B)),
                Child = Tb(info.Icon, 18, null, align: HorizontalAlignment.Center, vAlign: VerticalAlignment.Center)
            });
            var nameStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            nameStack.Children.Add(Tb(dept.ToString(), 15, new SolidColorBrush(info.Color), fontWeight: FontWeights.Bold));
            nameStack.Children.Add(Tb(_loc.IsDE?$"{dTasks.Count} Aufgabe(n)":$"{dTasks.Count} task(s)", 11.5, _loc.TextMutedBrush));
            topRow.Children.Add(nameStack);
            sp.Children.Add(topRow);

            sp.Children.Add(Tb(deptDescs[dept], 12, _loc.TextMutedBrush, new Thickness(0,4,0,12), lineHeight: 18));

            if (dTasks.Any())
            {
                sp.Children.Add(new Border { Height = 1, Background = _loc.BorderBrush2, Margin = new Thickness(0,0,0,10) });
                foreach (var t in dTasks.Take(3))
                {
                    var tr = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,5) };
                    tr.Children.Add(Tb(t.IsCompleted ? "✅ " : "⬜ ", 11));
                    tr.Children.Add(Tb(_loc.IsDE ? t.Title : t.TitleEn, 11.5, _loc.TextBrush, maxWidth: 200));
                    sp.Children.Add(tr);
                }
                if (dTasks.Count > 3)
                    sp.Children.Add(Tb(_loc.IsDE ? $"+ {dTasks.Count-3} weitere..." : $"+ {dTasks.Count-3} more...", 11, _loc.TextMutedBrush, new Thickness(0,2,0,0)));
            }
            else
                sp.Children.Add(Tb(_loc.IsDE?"Noch keine Aufgaben":"No tasks yet", 11.5, _loc.TextMutedBrush, FontStyles: FontStyles.Italic));

            card.Child = sp;
            card.MouseEnter += (s, e) => card.Effect = Shdw(0.15);
            card.MouseLeave += (s, e) => card.Effect = Shdw(0.07);
            DeptsContainer.Children.Add(card);
        }
    }

    // ── DOCUMENTS ───────────────────────────────────────────────────────────

    private void ShowDocuments()
    {
        ShowPanel("documents");
        txtDocsTitle.Text = _loc.IsDE ? "📁 Dokumente" : "📁 Documents";
        txtDocsTitle.Foreground = _loc.TextBrush;
        var docs = _db.GetDocuments();
        DocListContainer.Children.Clear(); DocContentContainer.Children.Clear();

        foreach (var grp in docs.GroupBy(d => d.Category))
        {
            if (!string.IsNullOrEmpty(grp.Key))
                DocListContainer.Children.Add(Tb(grp.Key.ToUpper(), 10, _loc.TextMutedBrush, new Thickness(12,12,12,4), fontWeight: FontWeights.SemiBold));
            foreach (var doc in grp)
            {
                var btn = new Border
                {
                    Padding = new Thickness(12,9,12,9), CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(4,1,4,1), Cursor = Cursors.Hand
                };
                btn.Child = Tb(_loc.IsDE ? doc.Title : doc.TitleEn, 13, _loc.TextBrush, wrap: true);
                var d = doc;
                btn.MouseLeftButtonUp += (s, e) => ShowDocContent(d);
                btn.MouseEnter += (s,e) => btn.Background = _loc.DarkMode ? new SolidColorBrush(Color.FromRgb(0x2A,0x2A,0x3E)) : new SolidColorBrush(Color.FromRgb(0xF3,0xF4,0xF6));
                btn.MouseLeave += (s,e) => btn.Background = Brushes.Transparent;
                DocListContainer.Children.Add(btn);
            }
        }
        if (docs.Any()) ShowDocContent(docs.First());
    }

    private void ShowDocContent(Document doc)
    {
        DocContentContainer.Children.Clear();
        DocContentContainer.Children.Add(Tb(_loc.IsDE?doc.Title:doc.TitleEn, 22, _loc.TextBrush, new Thickness(0,0,0,16), fontWeight: FontWeights.Bold, wrap: true));
        DocContentContainer.Children.Add(new TextBox
        {
            Text = _loc.IsDE ? doc.Content : doc.ContentEn,
            FontSize = 13.5, Background = Brushes.Transparent,
            Foreground = _loc.TextBrush, IsReadOnly = true, BorderThickness = new Thickness(0),
            TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, MinHeight = 22
        });
    }

    // ── HELPERS ─────────────────────────────────────────────────────────────

    private TextBlock Tb(string text, double size = 13, SolidColorBrush? fg = null,
        Thickness margin = default, FontStyle? style = null,
        HorizontalAlignment align = HorizontalAlignment.Left,
        VerticalAlignment vAlign = VerticalAlignment.Top,
        FontWeight? fontWeight = null, double lineHeight = 0,
        TextDecorationCollection? textDecoration = null, double maxWidth = 0, bool wrap = false, FontStyle FontStyles = default)
    {
        var tb = new TextBlock
        {
            Text = text, FontSize = size,
            Foreground = fg ?? _loc.TextBrush,
            Margin = margin, HorizontalAlignment = align, VerticalAlignment = vAlign,
        };
        if (style.HasValue) tb.FontStyle = style.Value;
        if (fontWeight.HasValue) tb.FontWeight = fontWeight.Value;
        if (lineHeight > 0) tb.LineHeight = lineHeight;
        if (textDecoration != null) tb.TextDecorations = textDecoration;
        if (maxWidth > 0) { tb.MaxWidth = maxWidth; tb.TextTrimming = TextTrimming.CharacterEllipsis; }
        if (wrap) tb.TextWrapping = TextWrapping.Wrap;
        return tb;
    }

    private Border Badge(string text, Color color, double alpha = 0.15)
    {
        var b = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb((byte)(alpha * 255), color.R, color.G, color.B)),
            CornerRadius = new CornerRadius(4), Padding = new Thickness(8,3,8,3), Margin = new Thickness(0,0,6,4)
        };
        b.Child = new TextBlock { Text = text, FontSize = 11.5, Foreground = new SolidColorBrush(color), FontWeight = FontWeights.SemiBold };
        return b;
    }

    private Border MakeCard() => new Border
    {
        Background = _loc.CardBrush, CornerRadius = new CornerRadius(10),
        Margin = new Thickness(0,0,0,16), Padding = new Thickness(22,18,22,18), Effect = Shdw(0.07)
    };

    private DropShadowEffect Shdw(double op) => new DropShadowEffect
    {
        ShadowDepth = 1, BlurRadius = 10, Color = Colors.Black, Opacity = op, Direction = 270
    };

    private string BuildLinksJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "[]";
        var list = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("http"))
            .Select(l => $"{{\"title\":\"{l}\",\"url\":\"{l}\"}}")
            .ToList();
        return "[" + string.Join(",", list) + "]";
    }
}
