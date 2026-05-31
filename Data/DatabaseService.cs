using Microsoft.Data.Sqlite;
using AzubiHilfer.Models;

namespace AzubiHilfer.Data;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string dbPath = "azubi_hilfer.db")
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
        SeedData();
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        Exec(conn, @"
            CREATE TABLE IF NOT EXISTS Tasks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL, TitleEn TEXT NOT NULL,
                Description TEXT, DescriptionEn TEXT,
                GroupType INTEGER NOT NULL, Tags TEXT,
                Difficulty INTEGER DEFAULT 0, Department INTEGER,
                IsCompleted INTEGER DEFAULT 0, AuthorName TEXT DEFAULT '',
                ExternalLinks TEXT DEFAULT '', Libraries TEXT DEFAULT '',
                ScreenshotPath TEXT DEFAULT '', CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
            );
            CREATE TABLE IF NOT EXISTS Stories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TaskId INTEGER NOT NULL, AzubiName TEXT, Year TEXT,
                Title TEXT, TitleEn TEXT, Content TEXT, ContentEn TEXT,
                FOREIGN KEY(TaskId) REFERENCES Tasks(Id)
            );
            CREATE TABLE IF NOT EXISTS CodeExamples (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StoryId INTEGER NOT NULL, Title TEXT,
                Language TEXT DEFAULT 'csharp', Code TEXT,
                Explanation TEXT, ExplanationEn TEXT,
                FOREIGN KEY(StoryId) REFERENCES Stories(Id)
            );
            CREATE TABLE IF NOT EXISTS Documents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL, TitleEn TEXT NOT NULL,
                Content TEXT, ContentEn TEXT,
                GroupType INTEGER, Category TEXT,
                CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
            );
            CREATE TABLE IF NOT EXISTS Comments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TaskId INTEGER NOT NULL, AuthorName TEXT,
                Text TEXT, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(TaskId) REFERENCES Tasks(Id)
            );");

        // Migrations
        foreach (var col in new[] {
            ("Difficulty","INTEGER DEFAULT 0"), ("Department","INTEGER"),
            ("IsCompleted","INTEGER DEFAULT 0"), ("AuthorName","TEXT DEFAULT ''"),
            ("ExternalLinks","TEXT DEFAULT ''"), ("Libraries","TEXT DEFAULT ''"),
            ("ScreenshotPath","TEXT DEFAULT ''"), ("CreatedAt","TEXT DEFAULT CURRENT_TIMESTAMP")
        }) TryAddCol(conn, "Tasks", col.Item1, col.Item2);
    }

    private void Exec(SqliteConnection conn, string sql)
    {
        using var c = conn.CreateCommand(); c.CommandText = sql; c.ExecuteNonQuery();
    }

    private void TryAddCol(SqliteConnection conn, string table, string col, string def)
    {
        try { using var c = conn.CreateCommand(); c.CommandText = $"ALTER TABLE {table} ADD COLUMN {col} {def}"; c.ExecuteNonQuery(); }
        catch { }
    }

    private void SeedData()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var chk = conn.CreateCommand(); chk.CommandText = "SELECT COUNT(*) FROM Tasks";
        if ((long)(chk.ExecuteScalar() ?? 0) > 0) return;

        var t1 = IT(conn,
            "Kontaktverwaltung in Delphi mit MS SQL Server",
            "Contact Management in Delphi with MS SQL Server",
            "Entwickle eine Desktop-Anwendung zur Verwaltung von Kontakten mit Delphi und MS SQL Server. Enthält CRUD-Operationen, Suche und Datenexport.",
            "Develop a desktop contact management app using Delphi and MS SQL Server. Includes CRUD, search and export.",
            AzubiGroup.FachinformatikerAnwendungsentwicklung, "Delphi,SQL,Desktop,CRUD",
            Difficulty.Beginner, Department.Entwicklung, false, "Max Müller",
            "[{\"title\":\"SQL Tutorial W3Schools\",\"url\":\"https://www.w3schools.com/sql/\"},{\"title\":\"FireDAC Docs\",\"url\":\"https://docwiki.embarcadero.com/RADStudio/en/FireDAC\"}]",
            "FireDAC,MS SQL Server");
        var s1 = IS(conn, t1, "Max Müller", "2022",
            "Datenbankverbindung mit FireDAC TFDConnection",
            "Database connection with FireDAC TFDConnection",
            "Ich realisierte die Verbindung über FireDAC. Trick: ConnectionDefName in fdconnections.ini — keine Passwörter im Code! Parametrisierte Abfragen verhindern SQL-Injection.",
            "Used FireDAC for connection. Trick: store ConnectionDefName in fdconnections.ini — no passwords in code! Parameterized queries prevent SQL injection.");
        IC(conn, s1, "Delphi - TFDConnection Setup", "delphi",
@"procedure TdmData.DataModuleCreate(Sender: TObject);
begin
  FDConnection1.Params.DriverID  := 'MSSQL';
  FDConnection1.Params.Database  := 'KontakteDB';
  FDConnection1.Params.Server    := 'localhost\SQLEXPRESS';
  FDConnection1.Params.OSAuthent := osYes;
  FDConnection1.Connected := True;
end;

procedure TdmData.KontaktEinfuegen(const Name, Email, Tel: string);
begin
  with FDQuery1 do
  begin
    SQL.Text := 'INSERT INTO Kontakte (Name, Email, Telefon) VALUES (:name, :email, :tel)';
    ParamByName('name').AsString  := Name;
    ParamByName('email').AsString := Email;
    ParamByName('tel').AsString   := Tel;
    ExecSQL;
  end;
end;",
            "TFDConnection via Windows-Auth. :name, :email, :tel sind Parameter — kein SQL-Injection-Risiko!",
            "TFDConnection via Windows Auth. :name :email :tel are parameters — no SQL injection risk!");
        AddComment(conn, t1, "Sarah Koch", "Super erklärt! Ich hatte das gleiche Problem mit der Windows-Auth. Tipp: 'OSAuthent := osYes' funktioniert nur wenn der SQL Server Express läuft.");
        AddComment(conn, t1, "Tim Braun", "W3Schools SQL Tutorial hat mir sehr geholfen zum Üben: https://www.w3schools.com/sql/");

        var t2 = IT(conn,
            "DevExpress Desktop Anwendung (WinForms/WPF)",
            "DevExpress Desktop Application",
            "Lerne GridControl, RibbonControl, LayoutControl und ChartControl für professionelle Unternehmensanwendungen.",
            "Learn GridControl, RibbonControl, LayoutControl and ChartControl for professional enterprise apps.",
            AzubiGroup.FachinformatikerAnwendungsentwicklung, "DevExpress,WinForms,WPF,C#,Grid",
            Difficulty.Intermediate, Department.Entwicklung, true, "Sarah Koch",
            "[{\"title\":\"DevExpress Docs\",\"url\":\"https://docs.devexpress.com\"},{\"title\":\"DevExpress GitHub\",\"url\":\"https://github.com/DevExpress-Examples\"}]",
            "DevExpress,CommunityToolkit.Mvvm");
        var s2 = IS(conn, t2, "Sarah Koch", "2023",
            "GridControl Daten binden und anpassen",
            "GridControl data binding and customization",
            "Das GridControl ist das mächtigste DevExpress-Control. BindingList<T> sorgt für automatische Updates. BestFitColumns() immer nach dem Laden aufrufen!",
            "GridControl is DevExpress's most powerful control. BindingList<T> ensures automatic updates. Always call BestFitColumns() after loading!");
        IC(conn, s2, "C# - GridControl Data Binding", "csharp",
@"private BindingList<Kontakt> _kontakte = new();

private void Load(object sender, EventArgs e)
{
    _kontakte = new BindingList<Kontakt>(Service.GetAll());
    gridControl1.DataSource = _kontakte;

    var view = gridControl1.MainView as GridView;
    if (view != null)
    {
        view.BestFitColumns();
        view.OptionsView.ShowAutoFilterRow = true;
        view.OptionsView.ShowGroupPanel    = true;
    }
}

private void btnAdd_Click(object sender, EventArgs e)
    => _kontakte.Add(new Kontakt { Name = ""Neuer Kontakt"" });",
            "BindingList<T> = Grid aktualisiert automatisch. ShowAutoFilterRow = Filterzeile. BestFitColumns = optimale Spaltenbreite.",
            "BindingList<T> = grid auto-updates. ShowAutoFilterRow = filter row. BestFitColumns = optimal column width.");
        IC(conn, s2, "C# - RibbonControl", "csharp",
@"private void InitRibbon()
{
    var tab = new RibbonPage(""Start"");
    ribbonControl1.Pages.Add(tab);
    var grp = new RibbonPageGroup(""Kontakte"");
    tab.Groups.Add(grp);
    var btnNeu = new BarButtonItem { Caption = ""Neu"", RibbonStyle = RibbonItemStyles.Large };
    btnNeu.ItemClick += (s, e) => AddNewContact();
    grp.ItemLinks.Add(btnNeu);
}",
            "Pages = Tabs, PageGroups = Gruppen im Tab, Items = Buttons.", "Pages = Tabs, PageGroups = groups, Items = Buttons.");

        var t3 = IT(conn,
            "SQL Grundlagen für Azubis",
            "SQL Basics for Apprentices",
            "Die wichtigsten SQL-Befehle: SELECT, INSERT, UPDATE, DELETE und JOIN — mit Beispielen aus der Firmen-Datenbank.",
            "The most important SQL commands: SELECT, INSERT, UPDATE, DELETE and JOIN — with company database examples.",
            AzubiGroup.FachinformatikerAnwendungsentwicklung, "SQL,Datenbank,Grundlagen,JOIN",
            Difficulty.Beginner, Department.Entwicklung, false, "Tim Braun",
            "[{\"title\":\"SQL Tutorial W3Schools\",\"url\":\"https://www.w3schools.com/sql/\"},{\"title\":\"SQL online üben\",\"url\":\"https://sqliteonline.com\"},{\"title\":\"SQL Cheatsheet\",\"url\":\"https://www.sqltutorial.org/sql-cheat-sheet/\"}]",
            "MS SQL Server,SQLite");
        var s3 = IS(conn, t3, "Tim Braun", "2023",
            "Die 5 wichtigsten SQL-Befehle",
            "The 5 most important SQL commands",
            "Als Azubi war SQL anfangs verwirrend. Tipp: erst SELECT üben (W3Schools ist super!), dann die anderen Befehle. JOINs sind am Anfang schwer — einfach viel üben!",
            "SQL was confusing at first. Tip: practice SELECT first (W3Schools is great!), then other commands. JOINs are hard initially — just practice a lot!");
        IC(conn, s3, "SQL - Die wichtigsten Befehle", "sql",
@"-- 1. SELECT - Daten lesen
SELECT Name, Email FROM Kunden WHERE Stadt = 'Berlin';

-- 2. INSERT - Neuen Datensatz anlegen
INSERT INTO Kunden (Name, Email, Stadt)
VALUES ('Max Mustermann', 'max@firma.de', 'Berlin');

-- 3. UPDATE - Daten ändern  (IMMER mit WHERE!)
UPDATE Kunden SET Email = 'neu@firma.de' WHERE Id = 42;

-- 4. DELETE - Datensatz löschen (VORSICHT! Immer WHERE!)
DELETE FROM Kunden WHERE Id = 42;

-- 5. JOIN - Tabellen verknüpfen
SELECT k.Name, b.Datum, b.Betrag
FROM Kunden k
INNER JOIN Bestellungen b ON k.Id = b.KundenId
WHERE b.Betrag > 100
ORDER BY b.Datum DESC;",
            "WHERE filtert Zeilen. INNER JOIN verknüpft Tabellen. ORDER BY sortiert. IMMER WHERE bei DELETE/UPDATE verwenden!",
            "WHERE filters rows. INNER JOIN links tables. ORDER BY sorts. ALWAYS use WHERE with DELETE/UPDATE!");

        var t4 = IT(conn,
            "Digitale Prozessanalyse mit BPMN 2.0",
            "Digital Process Analysis with BPMN 2.0",
            "Analysiere Geschäftsprozesse mit BPMN 2.0. Erstelle Prozessmodelle und identifiziere Optimierungspotenziale durch Digitalisierung.",
            "Analyze business processes with BPMN 2.0. Create process models and identify optimization potential.",
            AzubiGroup.KaufmannDigitalisierungsmanagement, "BPMN,Prozess,Analyse,Camunda",
            Difficulty.Beginner, Department.Verwaltung, true, "Lena Bauer",
            "[{\"title\":\"Camunda Modeler\",\"url\":\"https://camunda.com/download/modeler/\"},{\"title\":\"BPMN Tutorial\",\"url\":\"https://www.bpmn.org\"}]",
            "Camunda Modeler");
        var s4 = IS(conn, t4, "Lena Bauer", "2023",
            "Bewerbungsprozess mit Camunda modellieren",
            "Modeling recruitment process with Camunda",
            "Ich modellierte unseren Bewerbungsprozess in BPMN 2.0 mit Swimlanes. Ergebnis: 3 manuelle Schritte automatisierbar → 40% Zeitersparnis.",
            "Modeled our recruitment process in BPMN 2.0 with swimlanes. Result: 3 manual steps automatable → 40% time saving.");
        IC(conn, s4, "BPMN XML Prozessausschnitt", "xml",
@"<process id=""Bewerbung"" isExecutable=""true"">
  <startEvent id=""start"" name=""Bewerbung eingegangen""/>
  <userTask id=""pruefe"" name=""Unterlagen prüfen"">
    <extensionElements>
      <camunda:assignee>HR</camunda:assignee>
    </extensionElements>
  </userTask>
  <exclusiveGateway id=""gw1"" name=""Vollständig?""/>
  <sequenceFlow sourceRef=""start""  targetRef=""pruefe""/>
  <sequenceFlow sourceRef=""pruefe"" targetRef=""gw1""/>
  <sequenceFlow name=""Ja""   sourceRef=""gw1"" targetRef=""weiterleiten""/>
  <sequenceFlow name=""Nein"" sourceRef=""gw1"" targetRef=""nachfordern""/>
</process>",
            "userTask = menschliche Aufgabe. exclusiveGateway = Entweder-oder. sequenceFlow = Verbindungspfeil.",
            "userTask = human task. exclusiveGateway = either/or. sequenceFlow = connecting arrow.");

        var t5 = IT(conn,
            "Support-Ticketsystem verstehen und nutzen",
            "Understanding and Using the Support Ticket System",
            "Lerne wie das interne Ticketsystem funktioniert: Tickets erstellen, priorisieren, eskalieren und lösen. SLA-Zeiten und Kundenkommunikation.",
            "Learn how the internal ticket system works: create, prioritize, escalate and resolve tickets. SLA times and customer communication.",
            AzubiGroup.KaufmannDigitalisierungsmanagement, "Support,Tickets,SLA,Kommunikation",
            Difficulty.Beginner, Department.Support, false, "Jonas Weber",
            "[{\"title\":\"ITIL Grundlagen\",\"url\":\"https://www.axelos.com/certifications/itil-service-management\"},{\"title\":\"Freshdesk\",\"url\":\"https://freshdesk.com\"}]",
            "Freshdesk,JIRA");
        var s5 = IS(conn, t5, "Jonas Weber", "2022",
            "Mein erster Tag im Support", "My first day in support",
            "Im Support war Priorität alles: P1 = sofort, P2 = 4h, P3 = 1 Tag. CRM zeigt alle Kundenkontakte. Immer freundlich, auch bei schwierigen Tickets!",
            "In support, priority was everything: P1 = immediately, P2 = 4h, P3 = 1 day. Always stay friendly, even with difficult tickets!");
        IC(conn, s5, "Ticket-Prioritäten (SLA-Zeiten)", "text",
@"TICKET PRIORITÄTEN:
════════════════════════════════════════
P1 KRITISCH  → Reaktion: 15 Min | Lösung: 2h
   Beispiel:  Server down, alle Nutzer betroffen

P2 HOCH      → Reaktion: 1h    | Lösung: 4h
   Beispiel:  Login-Problem für ganze Abteilung

P3 MITTEL    → Reaktion: 4h    | Lösung: 1 Tag
   Beispiel:  Einzelner Nutzer kann Feature nicht nutzen

P4 NIEDRIG   → Reaktion: 1 Tag | Lösung: 1 Woche
   Beispiel:  Kosmetischer Bug / Wunsch-Feature

REGEL: P1 und P2 immer sofort eskalieren!",
            "SLA = Service Level Agreement. Legt Reaktions- und Lösungszeiten fest.",
            "SLA = Service Level Agreement. Defines response and resolution times.");
        AddComment(conn, t5, "Lena Bauer", "Tipp: Bei P1-Tickets immer sofort den Team-Lead anrufen, nicht nur per Chat schreiben!");

        // Documents
        IDc(conn, "DevExpress - Erste Schritte", "DevExpress - Getting Started",
            "# DevExpress Guide\n\n## Was ist DevExpress?\nKommerzielle UI-Bibliothek mit 500+ Controls für WinForms, WPF, Delphi.\n\n## Installation\n1. Download von mydevexpress.com (Firmenkonto)\n2. Installer starten → WinForms/WPF auswählen\n3. Visual Studio neu starten → Controls in Toolbox\n\n## Top Controls\n- **GridControl** – Datentabelle mit Filter/Sort/Group\n- **RibbonControl** – Office-Menüleiste\n- **ChartControl** – Diagramme (Balken, Linie, Kreis)\n- **LayoutControl** – Auto-Layout für Formulare\n\n## Lizenz\nFirmenlizenz vorhanden → Ausbilder fragen!\n\n## Ressourcen\n- docs.devexpress.com\n- DevExpress Demo Center (mit Installation dabei!)",
            "# DevExpress Guide\n\n## What is DevExpress?\nCommercial UI library with 500+ controls for WinForms, WPF, Delphi.\n\n## Installation\n1. Download from mydevexpress.com (company account)\n2. Run installer → select WinForms/WPF\n3. Restart Visual Studio → controls in Toolbox\n\n## Top Controls\n- **GridControl** – Data table with filter/sort/group\n- **RibbonControl** – Office menu bar\n- **ChartControl** – Charts (bar, line, pie)\n- **LayoutControl** – Auto-layout for forms\n\n## License\nCompany license available → ask your trainer!\n\n## Resources\n- docs.devexpress.com\n- DevExpress Demo Center (included with installation!)",
            null, "DevExpress");
        IDc(conn, "Abteilungen der Firma", "Company Departments",
            "# Firmen-Abteilungen\n\n## Support\nErster Ansprechpartner für Kunden. Ticketsystem, SLA-Zeiten, Eskalation.\n\n## Entwicklung\nDesktop- und Web-Apps in C#, Delphi, DevExpress.\n\n## Schnittstelle / Online 24\nAPI-Anbindungen, Online-Portale, Webservices.\n\n## Vertragsdatenbank\nKundenverwaltung, SQL-Reports, Datenqualität.\n\n## Verwaltung\nInterne Prozesse, HR, Buchhaltung, BPMN.\n\n## Azubi\nBetreuung der Auszubildenden, IHK-Vorbereitung.\n\n## Technik\nServer, Netzwerk, IT-Infrastruktur.\n\n## Vertrieb\nKundenakquise, CRM, Angebote.\n\n## Kundenbetreung\nLangfristige Kundenbeziehungen, Onboarding.\n\n## Report\nAuswertungen, Dashboards, BI-Tools.",
            "# Company Departments\n\n## Support\nFirst contact for customers. Ticket system, SLA, escalation.\n\n## Development\nDesktop and web apps in C#, Delphi, DevExpress.\n\n## Interface / Online 24\nAPI connections, online portals, web services.\n\n## Contract Database\nCustomer management, SQL reports, data quality.\n\n## Administration\nInternal processes, HR, accounting, BPMN.\n\n## Azubi\nApprentice supervision, IHK preparation.\n\n## Technology\nServers, network, IT infrastructure.\n\n## Sales\nCustomer acquisition, CRM, offers.\n\n## Customer Care\nLong-term relations, onboarding.\n\n## Report\nEvaluations, dashboards, BI tools.",
            null, "Firma");
        IDc(conn, "Git & Versionskontrolle", "Git & Version Control",
            "# Git Grundlagen\n\n## Wichtigste Befehle\n```\ngit clone <url>      # Repo herunterladen\ngit status           # Änderungen anzeigen\ngit add .            # Alle Änderungen stagen\ngit commit -m \"msg\" # Commit erstellen\ngit push             # Hochladen\ngit pull             # Herunterladen\ngit branch feature   # Branch erstellen\ngit checkout feature # Branch wechseln\ngit merge feature    # Branch zusammenführen\n```\n\n## Workflow im Team\n1. Immer auf einem Feature-Branch arbeiten\n2. Vor dem Merge: git pull (Konflikte vermeiden)\n3. Aussagekräftige Commit-Messages schreiben\n4. Nie direkt auf main/master pushen!\n\n## Ressourcen\n- learngitbranching.js.org (interaktiv!)\n- git-scm.com/doc",
            "# Git Basics\n\n## Most Important Commands\n```\ngit clone <url>      # Download repo\ngit status           # Show changes\ngit add .            # Stage all changes\ngit commit -m \"msg\" # Create commit\ngit push             # Upload\ngit pull             # Download\ngit branch feature   # Create branch\ngit checkout feature # Switch branch\ngit merge feature    # Merge branch\n```\n\n## Team Workflow\n1. Always work on a feature branch\n2. Before merging: git pull (avoid conflicts)\n3. Write meaningful commit messages\n4. Never push directly to main/master!\n\n## Resources\n- learngitbranching.js.org (interactive!)\n- git-scm.com/doc",
            null, "Tools");
    }

    private long IT(SqliteConnection conn, string title, string titleEn, string desc, string descEn,
        AzubiGroup group, string tags, Difficulty diff, Department dept, bool done,
        string author, string links, string libs)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Tasks (Title,TitleEn,Description,DescriptionEn,GroupType,Tags,Difficulty,Department,IsCompleted,AuthorName,ExternalLinks,Libraries)
            VALUES (@t,@te,@d,@de,@g,@tags,@diff,@dept,@done,@author,@links,@libs); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@t", title); cmd.Parameters.AddWithValue("@te", titleEn);
        cmd.Parameters.AddWithValue("@d", desc);  cmd.Parameters.AddWithValue("@de", descEn);
        cmd.Parameters.AddWithValue("@g", (int)group); cmd.Parameters.AddWithValue("@tags", tags);
        cmd.Parameters.AddWithValue("@diff", (int)diff); cmd.Parameters.AddWithValue("@dept", (int)dept);
        cmd.Parameters.AddWithValue("@done", done ? 1 : 0); cmd.Parameters.AddWithValue("@author", author);
        cmd.Parameters.AddWithValue("@links", links); cmd.Parameters.AddWithValue("@libs", libs);
        return (long)cmd.ExecuteScalar()!;
    }

    private long IS(SqliteConnection conn, long taskId, string name, string year,
        string title, string titleEn, string content, string contentEn)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Stories (TaskId,AzubiName,Year,Title,TitleEn,Content,ContentEn)
            VALUES (@tid,@n,@y,@t,@te,@c,@ce); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@tid", taskId); cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@y", year); cmd.Parameters.AddWithValue("@t", title);
        cmd.Parameters.AddWithValue("@te", titleEn); cmd.Parameters.AddWithValue("@c", content);
        cmd.Parameters.AddWithValue("@ce", contentEn);
        return (long)cmd.ExecuteScalar()!;
    }

    private void IC(SqliteConnection conn, long storyId, string title, string lang,
        string code, string expl, string explEn)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO CodeExamples (StoryId,Title,Language,Code,Explanation,ExplanationEn)
            VALUES (@sid,@t,@l,@c,@e,@ee)";
        cmd.Parameters.AddWithValue("@sid", storyId); cmd.Parameters.AddWithValue("@t", title);
        cmd.Parameters.AddWithValue("@l", lang); cmd.Parameters.AddWithValue("@c", code);
        cmd.Parameters.AddWithValue("@e", expl); cmd.Parameters.AddWithValue("@ee", explEn);
        cmd.ExecuteNonQuery();
    }

    private void IDc(SqliteConnection conn, string title, string titleEn,
        string content, string contentEn, AzubiGroup? group, string category)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Documents (Title,TitleEn,Content,ContentEn,GroupType,Category)
            VALUES (@t,@te,@c,@ce,@g,@cat)";
        cmd.Parameters.AddWithValue("@t", title); cmd.Parameters.AddWithValue("@te", titleEn);
        cmd.Parameters.AddWithValue("@c", content); cmd.Parameters.AddWithValue("@ce", contentEn);
        cmd.Parameters.AddWithValue("@g", group.HasValue ? (object)(int)group.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@cat", category);
        cmd.ExecuteNonQuery();
    }

    private void AddComment(SqliteConnection conn, long taskId, string author, string text)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Comments (TaskId,AuthorName,Text) VALUES (@tid,@a,@t)";
        cmd.Parameters.AddWithValue("@tid", taskId);
        cmd.Parameters.AddWithValue("@a", author);
        cmd.Parameters.AddWithValue("@t", text);
        cmd.ExecuteNonQuery();
    }

    // ── READ ────────────────────────────────────────────────────────────────

    public List<Models.Task> GetTasksByGroup(AzubiGroup group)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Tasks WHERE GroupType=@g ORDER BY IsCompleted ASC, CreatedAt DESC";
        cmd.Parameters.AddWithValue("@g", (int)group);
        return ReadTasks(cmd);
    }

    public List<Models.Task> GetAllTasks()
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand(); cmd.CommandText = "SELECT * FROM Tasks ORDER BY CreatedAt DESC";
        return ReadTasks(cmd);
    }

    public List<Models.Task> SearchTasks(string query)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT * FROM Tasks WHERE 
            Title LIKE @q OR TitleEn LIKE @q OR Description LIKE @q OR 
            Tags LIKE @q OR AuthorName LIKE @q OR Libraries LIKE @q
            ORDER BY CreatedAt DESC";
        cmd.Parameters.AddWithValue("@q", $"%{query}%");
        return ReadTasks(cmd);
    }

    public List<Models.Task> FilterTasks(AzubiGroup? group = null, Department? dept = null, Difficulty? diff = null, string? tag = null)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var where = new List<string>();
        var cmd = conn.CreateCommand();
        if (group.HasValue) { where.Add("GroupType=@g"); cmd.Parameters.AddWithValue("@g", (int)group.Value); }
        if (dept.HasValue)  { where.Add("Department=@d"); cmd.Parameters.AddWithValue("@d", (int)dept.Value); }
        if (diff.HasValue)  { where.Add("Difficulty=@diff"); cmd.Parameters.AddWithValue("@diff", (int)diff.Value); }
        if (!string.IsNullOrEmpty(tag)) { where.Add("Tags LIKE @tag"); cmd.Parameters.AddWithValue("@tag", $"%{tag}%"); }
        cmd.CommandText = "SELECT * FROM Tasks" + (where.Any() ? " WHERE " + string.Join(" AND ", where) : "") + " ORDER BY IsCompleted ASC, CreatedAt DESC";
        return ReadTasks(cmd);
    }

    private List<Models.Task> ReadTasks(SqliteCommand cmd)
    {
        var list = new List<Models.Task>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            int fc = r.FieldCount;
            list.Add(new Models.Task
            {
                Id = r.GetInt32(0), Title = r.GetString(1), TitleEn = r.GetString(2),
                Description = r.IsDBNull(3) ? "" : r.GetString(3),
                DescriptionEn = r.IsDBNull(4) ? "" : r.GetString(4),
                Group = (AzubiGroup)r.GetInt32(5),
                Tags = r.IsDBNull(6) ? new() : r.GetString(6).Split(',').Where(t => t.Length > 0).ToList(),
                Difficulty = fc > 7 && !r.IsDBNull(7) ? (Difficulty)r.GetInt32(7) : Difficulty.Beginner,
                Department = fc > 8 && !r.IsDBNull(8) ? (Department?)r.GetInt32(8) : null,
                IsCompleted = fc > 9 && !r.IsDBNull(9) && r.GetInt32(9) == 1,
                AuthorName = fc > 10 && !r.IsDBNull(10) ? r.GetString(10) : "",
                ExternalLinks = fc > 11 && !r.IsDBNull(11) ? r.GetString(11) : "",
                Libraries = fc > 12 && !r.IsDBNull(12) ? r.GetString(12) : "",
                ScreenshotPath = fc > 13 && !r.IsDBNull(13) ? r.GetString(13) : "",
            });
        }
        return list;
    }

    public List<Story> GetStoriesByTask(int taskId)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Stories WHERE TaskId=@tid";
        cmd.Parameters.AddWithValue("@tid", taskId);
        var list = new List<Story>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var s = new Story
            {
                Id = r.GetInt32(0), TaskId = r.GetInt32(1),
                AzubiName = r.IsDBNull(2) ? "" : r.GetString(2),
                Year = r.IsDBNull(3) ? "" : r.GetString(3),
                Title = r.IsDBNull(4) ? "" : r.GetString(4),
                TitleEn = r.IsDBNull(5) ? "" : r.GetString(5),
                Content = r.IsDBNull(6) ? "" : r.GetString(6),
                ContentEn = r.IsDBNull(7) ? "" : r.GetString(7),
            };
            s.CodeExamples = GetCodeExamples(s.Id);
            list.Add(s);
        }
        return list;
    }

    public List<CodeExample> GetCodeExamples(int storyId)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM CodeExamples WHERE StoryId=@sid";
        cmd.Parameters.AddWithValue("@sid", storyId);
        var list = new List<CodeExample>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new CodeExample
            {
                Id = r.GetInt32(0), StoryId = r.GetInt32(1),
                Title = r.IsDBNull(2) ? "" : r.GetString(2),
                Language = r.IsDBNull(3) ? "csharp" : r.GetString(3),
                Code = r.IsDBNull(4) ? "" : r.GetString(4),
                Explanation = r.IsDBNull(5) ? "" : r.GetString(5),
                ExplanationEn = r.IsDBNull(6) ? "" : r.GetString(6),
            });
        return list;
    }

    public List<Comment> GetComments(int taskId)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Comments WHERE TaskId=@tid ORDER BY CreatedAt ASC";
        cmd.Parameters.AddWithValue("@tid", taskId);
        var list = new List<Comment>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Comment
            {
                Id = r.GetInt32(0), TaskId = r.GetInt32(1),
                AuthorName = r.IsDBNull(2) ? "" : r.GetString(2),
                Text = r.IsDBNull(3) ? "" : r.GetString(3),
                CreatedAt = r.IsDBNull(4) ? DateTime.Now : DateTime.Parse(r.GetString(4)),
            });
        return list;
    }

    public List<Document> GetDocuments()
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand(); cmd.CommandText = "SELECT * FROM Documents ORDER BY Category,Title";
        var list = new List<Document>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Document
            {
                Id = r.GetInt32(0), Title = r.GetString(1), TitleEn = r.GetString(2),
                Content = r.IsDBNull(3) ? "" : r.GetString(3),
                ContentEn = r.IsDBNull(4) ? "" : r.GetString(4),
                Group = r.IsDBNull(5) ? null : (AzubiGroup?)r.GetInt32(5),
                Category = r.IsDBNull(6) ? "" : r.GetString(6),
            });
        return list;
    }

    public DashboardStats GetStats()
    {
        var all = GetAllTasks();
        var s = new DashboardStats
        {
            TotalTasks = all.Count, CompletedTasks = all.Count(t => t.IsCompleted),
            FITasks = all.Count(t => t.Group == AzubiGroup.FachinformatikerAnwendungsentwicklung),
            KDMTasks = all.Count(t => t.Group == AzubiGroup.KaufmannDigitalisierungsmanagement),
            FICompleted = all.Count(t => t.Group == AzubiGroup.FachinformatikerAnwendungsentwicklung && t.IsCompleted),
            KDMCompleted = all.Count(t => t.Group == AzubiGroup.KaufmannDigitalisierungsmanagement && t.IsCompleted),
        };
        foreach (Department d in Enum.GetValues<Department>())
            s.TasksByDepartment[d] = all.Count(t => t.Department == d);
        foreach (Difficulty d in Enum.GetValues<Difficulty>())
            s.TasksByDifficulty[d] = all.Count(t => t.Difficulty == d);
        return s;
    }

    // ── WRITE ───────────────────────────────────────────────────────────────

    public void SetTaskCompleted(int taskId, bool completed)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Tasks SET IsCompleted=@v WHERE Id=@id";
        cmd.Parameters.AddWithValue("@v", completed ? 1 : 0);
        cmd.Parameters.AddWithValue("@id", taskId);
        cmd.ExecuteNonQuery();
    }

    public long AddTask(Models.Task task)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        return IT(conn, task.Title, task.TitleEn, task.Description, task.DescriptionEn,
            task.Group, string.Join(",", task.Tags), task.Difficulty,
            task.Department ?? Department.Entwicklung, task.IsCompleted,
            task.AuthorName, task.ExternalLinks, task.Libraries);
    }

    public void UpdateTask(Models.Task task)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Tasks SET Title=@t, TitleEn=@te, Description=@d, DescriptionEn=@de,
            GroupType=@g, Tags=@tags, Difficulty=@diff, Department=@dept,
            AuthorName=@author, ExternalLinks=@links, Libraries=@libs, ScreenshotPath=@sp
            WHERE Id=@id";
        cmd.Parameters.AddWithValue("@t", task.Title); cmd.Parameters.AddWithValue("@te", task.TitleEn);
        cmd.Parameters.AddWithValue("@d", task.Description); cmd.Parameters.AddWithValue("@de", task.DescriptionEn);
        cmd.Parameters.AddWithValue("@g", (int)task.Group);
        cmd.Parameters.AddWithValue("@tags", string.Join(",", task.Tags));
        cmd.Parameters.AddWithValue("@diff", (int)task.Difficulty);
        cmd.Parameters.AddWithValue("@dept", task.Department.HasValue ? (object)(int)task.Department.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@author", task.AuthorName);
        cmd.Parameters.AddWithValue("@links", task.ExternalLinks);
        cmd.Parameters.AddWithValue("@libs", task.Libraries);
        cmd.Parameters.AddWithValue("@sp", task.ScreenshotPath);
        cmd.Parameters.AddWithValue("@id", task.Id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteTask(int taskId)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        void E(string sql) { var c = conn.CreateCommand(); c.CommandText = sql; c.Parameters.AddWithValue("@id", taskId); c.ExecuteNonQuery(); }
        E("DELETE FROM Comments WHERE TaskId=@id");
        E("DELETE FROM CodeExamples WHERE StoryId IN (SELECT Id FROM Stories WHERE TaskId=@id)");
        E("DELETE FROM Stories WHERE TaskId=@id");
        E("DELETE FROM Tasks WHERE Id=@id");
    }

    public void AddComment(int taskId, string author, string text)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        AddComment(conn, taskId, author, text);
    }


    public List<Comment> GetCommentsByTask(int taskId)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, TaskId, AuthorName, Text, CreatedAt FROM Comments WHERE TaskId = @tid ORDER BY CreatedAt ASC";
        cmd.Parameters.AddWithValue("@tid", taskId);
        var list = new List<Comment>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Comment
            {
                Id = r.GetInt32(0), TaskId = r.GetInt32(1),
                AuthorName = r.IsDBNull(2) ? "" : r.GetString(2),
                Text = r.IsDBNull(3) ? "" : r.GetString(3),
                CreatedAt = r.IsDBNull(4) ? DateTime.Now : DateTime.Parse(r.GetString(4)),
            });
        return list;
    }

    public void DeleteComment(int commentId)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand(); cmd.CommandText = "DELETE FROM Comments WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", commentId); cmd.ExecuteNonQuery();
    }

    public long AddStory(int taskId, string azubiName, string year, string title, string titleEn, string content, string contentEn)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        return IS(conn, taskId, azubiName, year, title, titleEn, content, contentEn);
    }

    public long AddCodeExample(int storyId, string title, string lang, string code, string expl, string explEn)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        IC(conn, storyId, title, lang, code, expl, explEn);
        var cmd = conn.CreateCommand(); cmd.CommandText = "SELECT last_insert_rowid()";
        return (long)cmd.ExecuteScalar()!;
    }

    public void UpdateScreenshot(int taskId, string path)
    {
        using var conn = new SqliteConnection(_connectionString); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Tasks SET ScreenshotPath=@p WHERE Id=@id";
        cmd.Parameters.AddWithValue("@p", path); cmd.Parameters.AddWithValue("@id", taskId);
        cmd.ExecuteNonQuery();
    }

    public List<string> GetAllTags()
    {
        var all = GetAllTasks();
        return all.SelectMany(t => t.Tags).Distinct().OrderBy(t => t).ToList();
    }
}