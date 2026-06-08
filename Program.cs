using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CentauriCarbonDownloader;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class PrinterFile
{
    public bool Selected { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Status { get; set; } = string.Empty;
}

internal sealed class TimelapseItem
{
    public bool Selected { get; set; }
    public string TaskId { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string RemoteMp4Path { get; set; } = string.Empty;
    public string RemoteFolderPath { get; set; } = string.Empty;
    public int FrameCount { get; set; }
    public List<string> FramePaths { get; set; } = new();
    public bool NeedsExport { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

internal sealed class RemoteDirectoryEntry
{
    public string Href { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string RemotePath { get; set; } = string.Empty;
    public string RowHtml { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public bool IsImage { get; set; }
    public bool IsMp4 { get; set; }
    public long? ModifiedUnix { get; set; }
}

internal sealed class ScanProgressInfo
{
    public string CurrentFolder { get; set; } = string.Empty;
    public int VisitedFolders { get; set; }
    public int QueuedFolders { get; set; }
    public int ImagesFound { get; set; }

    public int Percent
    {
        get
        {
            var total = VisitedFolders + QueuedFolders;
            if (total <= 0) return 0;
            return Math.Max(0, Math.Min(99, (int)Math.Round(VisitedFolders * 100.0 / total)));
        }
    }
}


internal sealed class ThemedButton : Button
{
    public Color ThemedDisabledForeColor { get; set; } = Color.FromArgb(148, 163, 184);
    public Color ThemedBorderColor { get; set; } = Color.FromArgb(75, 85, 99);
    public Color ThemedHoverBackColor { get; set; } = Color.FromArgb(45, 55, 72);
    public Color ThemedDownBackColor { get; set; } = Color.FromArgb(24, 32, 43);
    private bool _hover;
    private bool _down;

    public ThemedButton()
    {
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs mevent) { if (mevent.Button == MouseButtons.Left) _down = true; Invalidate(); base.OnMouseDown(mevent); }
    protected override void OnMouseUp(MouseEventArgs mevent) { _down = false; Invalidate(); base.OnMouseUp(mevent); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var back = !Enabled ? BackColor : _down ? ThemedDownBackColor : _hover ? ThemedHoverBackColor : BackColor;
        using var backBrush = new SolidBrush(back);
        using var borderPen = new Pen(ThemedBorderColor);
        e.Graphics.FillRectangle(backBrush, ClientRectangle);
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
        var textColor = Enabled ? ForeColor : ThemedDisabledForeColor;
        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, textColor, flags);
    }
}

internal sealed class ThemedProgressBar : ProgressBar
{
    public Color ThemeBackColor { get; set; } = Color.FromArgb(17, 24, 39);
    public Color ThemeBorderColor { get; set; } = Color.FromArgb(75, 85, 99);
    public Color ThemeBarColor { get; set; } = Color.FromArgb(59, 130, 246);

    public ThemedProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var backBrush = new SolidBrush(ThemeBackColor);
        using var barBrush = new SolidBrush(ThemeBarColor);
        using var borderPen = new Pen(ThemeBorderColor);
        e.Graphics.FillRectangle(backBrush, ClientRectangle);
        if (Maximum > Minimum && Value > Minimum)
        {
            var ratio = Math.Max(0, Math.Min(1, (Value - Minimum) / (double)(Maximum - Minimum)));
            var width = Math.Max(1, (int)Math.Round((ClientRectangle.Width - 2) * ratio));
            e.Graphics.FillRectangle(barBrush, 1, 1, width, ClientRectangle.Height - 2);
        }
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }
}

internal sealed class ThemedTabControl : TabControl
{
    public bool DarkMode { get; set; }
    public Color ThemeBackColor { get; set; } = Color.FromArgb(13, 17, 23);
    public Color ThemeSurfaceColor { get; set; } = Color.FromArgb(24, 32, 43);
    public Color ThemeTabBackColor { get; set; } = Color.FromArgb(17, 24, 39);
    public Color ThemeSelectedTabColor { get; set; } = Color.FromArgb(24, 32, 43);
    public Color ThemeTextColor { get; set; } = Color.FromArgb(243, 244, 246);
    public Color ThemeMutedTextColor { get; set; } = Color.FromArgb(156, 163, 175);
    public Color ThemeBorderColor { get; set; } = Color.FromArgb(75, 85, 99);

    public ThemedTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Normal;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var backBrush = new SolidBrush(ThemeBackColor);
        using var surfaceBrush = new SolidBrush(ThemeSurfaceColor);
        using var borderPen = new Pen(ThemeBorderColor);
        e.Graphics.FillRectangle(backBrush, ClientRectangle);

        var display = DisplayRectangle;
        if (display.Width > 0 && display.Height > 0)
        {
            var body = new Rectangle(display.Left - 1, display.Top - 1, display.Width + 2, display.Height + 2);
            e.Graphics.FillRectangle(surfaceBrush, body);
            e.Graphics.DrawRectangle(borderPen, body.X, body.Y, body.Width - 1, body.Height - 1);
        }

        for (int i = 0; i < TabCount; i++)
        {
            var rect = GetTabRect(i);
            var selected = i == SelectedIndex;
            using var tabBrush = new SolidBrush(selected ? ThemeSelectedTabColor : ThemeTabBackColor);
            e.Graphics.FillRectangle(tabBrush, rect);
            e.Graphics.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            var color = selected ? ThemeTextColor : ThemeMutedTextColor;
            TextRenderer.DrawText(e.Graphics, TabPages[i].Text, Font, rect, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        Invalidate();
        base.OnSelectedIndexChanged(e);
    }
}

internal sealed class MainForm : Form
{
    private readonly TextBox _ipBox = new();
    private readonly Button _connectButton = new ThemedButton();
    private readonly Button _openFolderButton = new ThemedButton();
    private readonly Button _chooseFolderButton = new ThemedButton();
    private const string AppVersion = "v0.5.4";
    private const string CreatorCredit = "Créé par ashemka";

    // Créé par ashemka : branding volontairement présent dans l’UI, les logs et les métadonnées.
    private readonly ProgressBar _progress = new ThemedProgressBar();
    private readonly Label _statusLabel = new();
    private readonly Label _titleLabel = new();
    private readonly Label _printerIpLabel = new();
    private readonly Label _languageLabel = new();
    private readonly Label _fpsLabel = new();
    private readonly Label _parallelLabel = new();
    private readonly Label _footerLabel = new();
    private readonly Label _downloadFolderLabel = new();
    private readonly ComboBox _languageBox = new();
    private readonly CheckBox _darkModeCheckBox = new();
    private readonly TabControl _tabs = new ThemedTabControl();

    private readonly Button _refreshLocalButton = new ThemedButton();
    private readonly Button _selectAllLocalButton = new ThemedButton();
    private readonly Button _downloadLocalButton = new ThemedButton();
    private readonly Button _deleteLocalButton = new ThemedButton();
    private readonly TextBox _filterLocalBox = new();
    private readonly DataGridView _localGrid = new();
    private readonly BindingSource _localBinding = new();
    private readonly List<PrinterFile> _localFiles = new();

    private readonly Button _refreshTimelapseButton = new ThemedButton();
    private readonly Button _selectAllTimelapseButton = new ThemedButton();
    private readonly Button _downloadTimelapseButton = new ThemedButton();
    private readonly Button _exportPrinterTimelapseButton = new ThemedButton();
    private readonly TextBox _filterTimelapseBox = new();
    private readonly TextBox _fpsBox = new();
    private readonly TextBox _parallelBox = new();
    private readonly CheckBox _keepFramesCheckBox = new();
    private readonly DataGridView _timelapseGrid = new();
    private readonly BindingSource _timelapseBinding = new();
    private readonly List<TimelapseItem> _timelapses = new();

    private readonly Button _cancelButton = new ThemedButton();

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _receiveCts;
    private CancellationTokenSource? _downloadCts;
    private readonly Dictionary<string, TaskCompletionSource<bool>> _timelapseExportWaiters = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _timelapseExportWaitersLock = new();
    private readonly Dictionary<string, TaskCompletionSource<JsonElement>> _responseWaiters = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _responseWaitersLock = new();
    private string _host = string.Empty;
    private string _baseDownloadFolder = string.Empty;
    private string _gcodeFolder = string.Empty;
    private string _timelapseFolder = string.Empty;
    private bool _busy;
    private string _currentLang = "fr";
    private bool _darkMode;
    private readonly string _settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CentauriCarbonDownloader", "settings.json");

    // Durées vérifiées / cadrées pour éviter les blocages longs au login/handshake.
    private static readonly TimeSpan WebSocketHandshakeTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan WebSocketCommandTimeout = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan HttpProbeTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan LongTransferTimeout = TimeSpan.FromMinutes(45);
    private static readonly TimeSpan PrinterExportTimeout = TimeSpan.FromMinutes(10);

    public MainForm()
    {
        Text = $"Centauri Carbon Downloader {AppVersion} — {CreatorCredit}";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(980, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        SetDownloadBaseFolder(Path.Combine(GetDownloadsFolder(), "Centauri_Downloads"), updateUi: false);

        BuildUi();
        LoadUserSettings();
        ApplyLanguage();
        ApplyTheme();
        WireEvents();
        SetDisconnectedState();
    }

    private void BuildUi()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(12)
        };
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(main);

        var titlePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 6,
            RowCount = 2,
            AutoSize = true
        };
        titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _titleLabel.Text = $"Centauri Carbon Downloader {AppVersion} — {CreatorCredit}";
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.AutoSize = true;
        _titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        _titleLabel.Padding = new Padding(0, 0, 0, 8);
        titlePanel.Controls.Add(_titleLabel, 0, 0);

        _languageLabel.Text = "Langue :";
        _languageLabel.AutoSize = true;
        _languageLabel.Anchor = AnchorStyles.Left;
        _languageLabel.Padding = new Padding(8, 4, 4, 0);
        titlePanel.Controls.Add(_languageLabel, 1, 0);

        _languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageBox.Width = 140;
        _languageBox.Items.AddRange(new object[] { "Français", "English", "Italiano", "Español", "Deutsch", "日本語", "中文", "한국어" });
        _languageBox.SelectedIndex = 0;
        titlePanel.Controls.Add(_languageBox, 2, 0);

        _darkModeCheckBox.Text = "Mode sombre";
        _darkModeCheckBox.AutoSize = true;
        _darkModeCheckBox.Anchor = AnchorStyles.Left;
        _darkModeCheckBox.Padding = new Padding(8, 2, 0, 0);
        titlePanel.Controls.Add(_darkModeCheckBox, 3, 0);

        main.Controls.Add(titlePanel, 0, 0);

        var connectionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 6,
            RowCount = 2,
            AutoSize = true
        };
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _printerIpLabel.Text = "IP imprimante :";
        _printerIpLabel.AutoSize = true;
        _printerIpLabel.Anchor = AnchorStyles.Left;
        _printerIpLabel.Padding = new Padding(0, 0, 8, 0);
        connectionPanel.Controls.Add(_printerIpLabel, 0, 0);

        _ipBox.Text = "10.7.1.137";
        _ipBox.Dock = DockStyle.Fill;
        _ipBox.PlaceholderText = "ex : 10.7.1.137";
        connectionPanel.Controls.Add(_ipBox, 1, 0);

        _connectButton.Text = Tr("connect");
        _connectButton.AutoSize = true;
        connectionPanel.Controls.Add(_connectButton, 2, 0);

        _chooseFolderButton.Text = "Choisir dossier";
        _chooseFolderButton.AutoSize = true;
        connectionPanel.Controls.Add(_chooseFolderButton, 3, 0);

        _openFolderButton.Text = "Ouvrir dossier";
        _openFolderButton.AutoSize = true;
        connectionPanel.Controls.Add(_openFolderButton, 4, 0);

        _cancelButton.Text = "Annuler téléchargement";
        _cancelButton.AutoSize = true;
        connectionPanel.Controls.Add(_cancelButton, 5, 0);

        _downloadFolderLabel.AutoSize = false;
        _downloadFolderLabel.Dock = DockStyle.Fill;
        _downloadFolderLabel.Height = 26;
        _downloadFolderLabel.Padding = new Padding(0, 6, 0, 0);
        _downloadFolderLabel.TextAlign = ContentAlignment.MiddleLeft;
        connectionPanel.Controls.Add(_downloadFolderLabel, 0, 1);
        connectionPanel.SetColumnSpan(_downloadFolderLabel, 6);

        main.Controls.Add(connectionPanel, 0, 1);

        _tabs.Dock = DockStyle.Fill;
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.TabPages.Add(BuildLocalTab());
        _tabs.TabPages.Add(BuildTimelapseTab());
        main.Controls.Add(_tabs, 0, 2);

        _progress.Dock = DockStyle.Top;
        _progress.Height = 18;
        main.Controls.Add(_progress, 0, 3);

        _statusLabel.Text = "Prêt.";
        _statusLabel.AutoSize = false;
        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Height = 40;
        _statusLabel.Padding = new Padding(0, 8, 0, 0);
        main.Controls.Add(_statusLabel, 0, 4);

        _footerLabel.Text = $"{CreatorCredit} — build {AppVersion}";
        _footerLabel.AutoSize = false;
        _footerLabel.Dock = DockStyle.Top;
        _footerLabel.Height = 24;
        _footerLabel.TextAlign = ContentAlignment.MiddleRight;
        main.Controls.Add(_footerLabel, 0, 5);
    }

    private TabPage BuildLocalTab()
    {
        var page = new TabPage("Fichiers G-code");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(layout);

        var tools = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 6,
            RowCount = 1,
            AutoSize = true
        };
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _refreshLocalButton.Text = "Actualiser";
        _refreshLocalButton.AutoSize = true;
        tools.Controls.Add(_refreshLocalButton, 0, 0);

        _selectAllLocalButton.Text = "Tout cocher";
        _selectAllLocalButton.AutoSize = true;
        tools.Controls.Add(_selectAllLocalButton, 1, 0);

        _filterLocalBox.PlaceholderText = "Filtrer les fichiers...";
        _filterLocalBox.Dock = DockStyle.Fill;
        tools.Controls.Add(_filterLocalBox, 2, 0);

        _downloadLocalButton.Text = "Télécharger sélection";
        _downloadLocalButton.AutoSize = true;
        tools.Controls.Add(_downloadLocalButton, 3, 0);

        _deleteLocalButton.Text = "Supprimer sélection";
        _deleteLocalButton.AutoSize = true;
        tools.Controls.Add(_deleteLocalButton, 4, 0);

        layout.Controls.Add(tools, 0, 0);

        PrepareGrid(_localGrid);
        _localGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(PrinterFile.Selected),
            HeaderText = "",
            Width = 42
        });
        _localGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PrinterFile.Name),
            HeaderText = "Fichier",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly = true
        });
        _localGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PrinterFile.Size),
            HeaderText = "Taille",
            Width = 95,
            ReadOnly = true
        });
        _localGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PrinterFile.Status),
            HeaderText = "Statut",
            Width = 220,
            ReadOnly = true
        });
        _localBinding.DataSource = _localFiles;
        _localGrid.DataSource = _localBinding;
        layout.Controls.Add(_localGrid, 0, 1);

        return page;
    }

    private TabPage BuildTimelapseTab()
    {
        var page = new TabPage("Timelapses vidéo");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(layout);

        var tools = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 10,
            RowCount = 1,
            AutoSize = true
        };
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _refreshTimelapseButton.Text = "Scanner timelapses";
        _refreshTimelapseButton.AutoSize = true;
        tools.Controls.Add(_refreshTimelapseButton, 0, 0);

        _selectAllTimelapseButton.Text = "Tout cocher";
        _selectAllTimelapseButton.AutoSize = true;
        tools.Controls.Add(_selectAllTimelapseButton, 1, 0);

        _filterTimelapseBox.PlaceholderText = "Filtrer les timelapses...";
        _filterTimelapseBox.Dock = DockStyle.Fill;
        tools.Controls.Add(_filterTimelapseBox, 2, 0);

        _fpsLabel.Text = "FPS";
        _fpsLabel.AutoSize = true;
        _fpsLabel.Anchor = AnchorStyles.Left;
        _fpsLabel.Padding = new Padding(8, 4, 4, 0);
        tools.Controls.Add(_fpsLabel, 3, 0);
        _fpsBox.Text = "30";
        _fpsBox.Width = 42;
        tools.Controls.Add(_fpsBox, 4, 0);

        _parallelLabel.Text = "Flux";
        _parallelLabel.AutoSize = true;
        _parallelLabel.Anchor = AnchorStyles.Left;
        _parallelLabel.Padding = new Padding(8, 4, 4, 0);
        tools.Controls.Add(_parallelLabel, 5, 0);
        _parallelBox.Text = "6";
        _parallelBox.Width = 42;
        tools.Controls.Add(_parallelBox, 6, 0);

        _keepFramesCheckBox.Text = "Garder images";
        _keepFramesCheckBox.AutoSize = true;
        tools.Controls.Add(_keepFramesCheckBox, 7, 0);

        _downloadTimelapseButton.Text = "Créer vidéos sur PC";
        _downloadTimelapseButton.AutoSize = true;
        tools.Controls.Add(_downloadTimelapseButton, 8, 0);

        _exportPrinterTimelapseButton.Text = "Export imprimante";
        _exportPrinterTimelapseButton.AutoSize = true;
        tools.Controls.Add(_exportPrinterTimelapseButton, 9, 0);

        layout.Controls.Add(tools, 0, 0);

        PrepareGrid(_timelapseGrid);
        _timelapseGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(TimelapseItem.Selected),
            HeaderText = "",
            Width = 42
        });
        _timelapseGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TimelapseItem.DateText),
            HeaderText = "Date",
            Width = 145,
            ReadOnly = true
        });
        _timelapseGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TimelapseItem.TaskName),
            HeaderText = "Impression",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly = true
        });
        _timelapseGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TimelapseItem.DurationText),
            HeaderText = "Durée",
            Width = 90,
            ReadOnly = true
        });
        _timelapseGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TimelapseItem.FrameCount),
            HeaderText = "Images",
            Width = 75,
            ReadOnly = true
        });
        _timelapseGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TimelapseItem.Status),
            HeaderText = "Statut",
            Width = 240,
            ReadOnly = true
        });
        _timelapseBinding.DataSource = _timelapses;
        _timelapseGrid.DataSource = _timelapseBinding;
        layout.Controls.Add(_timelapseGrid, 0, 1);

        return page;
    }

    private static void PrepareGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = true;
        grid.AutoGenerateColumns = false;
        grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
    }

    private void WireEvents()
    {
        FormClosing += async (_, _) => await DisconnectAsync();
        _connectButton.Click += async (_, _) => await ConnectOrDisconnectAsync();
        _chooseFolderButton.Click += (_, _) => ChooseDownloadFolder();
        _openFolderButton.Click += (_, _) => OpenDownloadFolder(_baseDownloadFolder);
        _cancelButton.Click += (_, _) => _downloadCts?.Cancel();

        _refreshLocalButton.Click += async (_, _) => await FetchLocalFilesAsync();
        _selectAllLocalButton.Click += (_, _) => ToggleAllVisible(_localGrid, _selectAllLocalButton, _localBinding, typeof(PrinterFile));
        _downloadLocalButton.Click += async (_, _) => await DownloadLocalSelectedAsync();
        _deleteLocalButton.Click += async (_, _) => await DeleteSelectedAsync();
        _filterLocalBox.TextChanged += (_, _) => ApplyLocalFilter();

        _refreshTimelapseButton.Click += async (_, _) => await FetchTimelapsesAsync();
        _selectAllTimelapseButton.Click += (_, _) => ToggleAllVisible(_timelapseGrid, _selectAllTimelapseButton, _timelapseBinding, typeof(TimelapseItem));
        _downloadTimelapseButton.Click += async (_, _) => await GenerateTimelapseVideosOnPcSelectedAsync();
        _exportPrinterTimelapseButton.Click += async (_, _) => await DownloadTimelapseSelectedAsync();
        _filterTimelapseBox.TextChanged += (_, _) => ApplyTimelapseFilter();
        _languageBox.SelectedIndexChanged += (_, _) =>
        {
            _currentLang = LangCodeFromIndex(_languageBox.SelectedIndex);
            ApplyLanguage();
            SaveUserSettings();
        };
        _darkModeCheckBox.CheckedChanged += (_, _) =>
        {
            _darkMode = _darkModeCheckBox.Checked;
            ApplyTheme();
            SaveUserSettings();
        };
        _tabs.DrawItem += Tabs_DrawItem;

        _localGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_localGrid.IsCurrentCellDirty) _localGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _timelapseGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_timelapseGrid.IsCurrentCellDirty) _timelapseGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _localGrid.CellFormatting += (_, e) =>
        {
            if (_localGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(PrinterFile.Size) && e.Value is long size)
            {
                e.Value = FormatSize(size);
                e.FormattingApplied = true;
            }
        };
    }

    private async Task ConnectOrDisconnectAsync()
    {
        if (_webSocket is { State: WebSocketState.Open })
        {
            await DisconnectAsync();
            return;
        }

        await ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        if (_busy) return;

        var rawHost = NormalizeHost(_ipBox.Text);
        if (string.IsNullOrWhiteSpace(rawHost))
        {
            MessageBox.Show(Tr("msgEnterIp"), Tr("titleMissingIp"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _host = rawHost;
        SetBusy(true, string.Format(Tr("statusConnecting"), WebSocketHandshakeTimeout.TotalSeconds));

        try
        {
            await DisconnectAsync(clearFiles: false);
            _webSocket = new ClientWebSocket();
            _receiveCts = new CancellationTokenSource();
            using var handshakeCts = CancellationTokenSource.CreateLinkedTokenSource(_receiveCts.Token);
            handshakeCts.CancelAfter(WebSocketHandshakeTimeout);
            var wsUri = new Uri($"ws://{_host}/websocket");
            await _webSocket.ConnectAsync(wsUri, handshakeCts.Token);

            _ = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token));
            SetConnectedState();
            await FetchLocalFilesAsync();
        }
        catch (Exception ex)
        {
            await DisconnectAsync();
            MessageBox.Show($"{Tr("msgConnectImpossible")}\n\n{Tr("detail")} {ex.Message}", Tr("titleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetDisconnectedState();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task DisconnectAsync(bool clearFiles = true)
    {
        try
        {
            _receiveCts?.Cancel();
            if (_webSocket is { State: WebSocketState.Open or WebSocketState.CloseReceived })
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            }
        }
        catch
        {
            // ignored: disconnection must remain silent for the user.
        }
        finally
        {
            _webSocket?.Dispose();
            _webSocket = null;
            _receiveCts?.Dispose();
            _receiveCts = null;
            if (clearFiles)
            {
                _localFiles.Clear();
                _timelapses.Clear();
                _localBinding.ResetBindings(false);
                _timelapseBinding.ResetBindings(false);
            }
            SetDisconnectedState();
        }
    }

    private async Task FetchLocalFilesAsync()
    {
        if (!EnsureConnected()) return;

        SetBusy(true, "Chargement de la liste locale...");
        try
        {
            await SendCommandAsync(258, new JsonObject { ["Url"] = "/local" });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Impossible de demander la liste des fichiers.\n\nDétail : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task FetchTimelapsesAsync()
    {
        if (!EnsureConnected()) return;

        _timelapses.Clear();
        _timelapseBinding.ResetBindings(false);
        SetBusy(true, "Scan rapide du dossier timelapse...");

        try
        {
            var found = await FetchTimelapseFolderListAsync();
            foreach (var item in found) _timelapses.Add(item);

            if (_timelapses.Count > 0)
            {
                _timelapses.Sort((a, b) => string.Compare(a.TaskName, b.TaskName, StringComparison.CurrentCultureIgnoreCase));
                _timelapseBinding.DataSource = _timelapses;
                _timelapseBinding.ResetBindings(false);
                ApplyTimelapseFilter();
                SetStatus($"{_timelapses.Count} timelapse item(s) detected — {CreatorCredit}.");
                return;
            }

            SetStatus("Aucun dossier timelapse trouvé dans /local/aic_tlp/. Tentative via l'historique...");
            await SendCommandAsync(320, new JsonObject());
        }
        catch (Exception ex)
        {
            SetStatus("Scan dossier impossible. Tentative via l'historique...");
            try
            {
                await SendCommandAsync(320, new JsonObject());
            }
            catch
            {
                MessageBox.Show($"Impossible de scanner les timelapses.\n\nDétail : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (!EnsureConnected()) return;

        _localGrid.EndEdit();
        var selected = GetSelectedLocalFiles().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Aucun fichier sélectionné.", "Suppression", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Supprimer {selected.Count} fichier(s) de l'imprimante ?\n\nCette action est définitive.",
            "Confirmation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes) return;

        SetBusy(true, "Suppression en cours...");
        try
        {
            var arr = new JsonArray();
            foreach (var file in selected) arr.Add(file.Name);
            await SendCommandAsync(259, new JsonObject { ["FileList"] = arr });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Impossible d'envoyer la demande de suppression.\n\nDétail : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task DownloadLocalSelectedAsync()
    {
        _localGrid.EndEdit();
        var selected = GetSelectedLocalFiles().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Aucun fichier sélectionné.", "Téléchargement", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await DownloadSequenceAsync(
            selected,
            _gcodeFolder,
            itemName: f => f.Name,
            urlBuilder: f => BuildLocalDownloadUrl(f.Name),
            pathBuilder: f => BuildLocalGcodePath(f.Name),
            setStatus: (f, s) => f.Status = s,
            resetBindings: () => _localBinding.ResetBindings(false),
            logName: "centauri_gcode_download_log.txt");
    }

    private async Task GenerateTimelapseVideosOnPcSelectedAsync()
    {
        if (!EnsureConnected()) return;

        _timelapseGrid.EndEdit();
        var selected = GetSelectedTimelapses().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Aucune vidéo sélectionnée.", "Timelapses", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var ffmpegPath = FindFfmpegExecutable();
        if (string.IsNullOrWhiteSpace(ffmpegPath))
        {
            var fallback = MessageBox.Show(
                "ffmpeg.exe est introuvable.\n\nNormalement, la version distribuée doit l'intégrer directement dans l'application. Cette build semble avoir été compilée sans FFmpeg intégré.\n\nUtiliser l'export imprimante à la place ?",
                "FFmpeg introuvable",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (fallback == DialogResult.Yes)
                await ExportAndDownloadTimelapseSequenceAsync(selected);
            return;
        }

        var fps = GetConfiguredFps();
        var parallelism = GetConfiguredParallelism();
        await GenerateTimelapseVideosOnPcSequenceAsync(selected, ffmpegPath, fps, parallelism, _keepFramesCheckBox.Checked);
    }

    private async Task GenerateTimelapseVideosOnPcSequenceAsync(List<TimelapseItem> selected, string ffmpegPath, int fps, int parallelism, bool keepFrames)
    {
        Directory.CreateDirectory(_timelapseFolder);
        _downloadCts?.Dispose();
        _downloadCts = new CancellationTokenSource();
        var token = _downloadCts.Token;

        _progress.Value = 0;
        _progress.Maximum = selected.Count;
        SetDownloadUiState(true);

        var ok = 0;
        var ko = 0;
        var logLines = new List<string>
        {
            $"Centauri Carbon Downloader - Timelapses PC rapide - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Imprimante : {_host}",
            $"Dossier : {_timelapseFolder}",
            $"FFmpeg : {ffmpegPath}",
            $"FPS : {fps}",
            $"Flux parallèles : {parallelism}",
            new string('-', 70)
        };

        using var http = new HttpClient { Timeout = LongTransferTimeout };

        for (var i = 0; i < selected.Count; i++)
        {
            var item = selected[i];
            if (token.IsCancellationRequested)
            {
                item.Status = "Annulé";
                logLines.Add($"ANNULÉ : {item.TaskName}");
                break;
            }

            try
            {
                SetStatus($"Timelapse {i + 1}/{selected.Count} : {item.TaskName}");

                if (item.FramePaths.Count == 0)
                {
                    // Si la vidéo MP4 existe déjà, on la télécharge directement.
                    // Sinon, on analyse le dossier d'images seulement pour l'élément sélectionné.
                    if (!string.IsNullOrWhiteSpace(item.VideoUrl) && await TryHttpExistsAsync(http, item.VideoUrl, token))
                    {
                        item.Status = "MP4 prêt, téléchargement";
                        _timelapseBinding.ResetBindings(false);
                        var directPath = BuildTimelapsePath(item);
                        Directory.CreateDirectory(Path.GetDirectoryName(directPath)!);
                        await DownloadFileWithRetriesAsync(http, item.VideoUrl, directPath, token, 5);
                        var directInfo = new FileInfo(directPath);
                        if (!directInfo.Exists || directInfo.Length == 0) throw new IOException("Le fichier MP4 reçu est vide.");
                        item.Status = "OK";
                        ok++;
                        logLines.Add($"OK DIRECT : {item.TaskName} -> {directPath} ({directInfo.Length} octets)");
                        continue;
                    }

                    item.Status = "Analyse images";
                    _timelapseBinding.ResetBindings(false);
                    SetStatus($"Analyse du dossier images : {item.TaskName}");

                    var frames = await ResolveTimelapseFramesAsync(item, token);
                    item.FramePaths = frames.Select(f => f.RemotePath).ToList();
                    item.FrameCount = item.FramePaths.Count;
                    item.Source = item.FrameCount >= 2 ? "Images" : item.Source;
                    item.Status = item.FrameCount >= 2 ? $"{item.FrameCount} images" : "Aucune image";
                    _timelapseBinding.ResetBindings(false);

                    if (item.FramePaths.Count < 2)
                        throw new InvalidOperationException($"Aucune image source détectée pour générer la vidéo localement. Dossier testé : {item.RemoteFolderPath}");
                }

                var safeName = SanitizeFileName(item.TaskName);
                if (safeName.Length > 80) safeName = safeName[..80].Trim();
                if (string.IsNullOrWhiteSpace(safeName)) safeName = "timelapse";

                var frameFolder = Path.Combine(_timelapseFolder, "_frames", $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeName}_{unchecked((uint)item.TaskId.GetHashCode()):X8}");
                Directory.CreateDirectory(frameFolder);

                if (item.FramePaths.Count < 2)
                {
                    item.Status = "Analyse images";
                    _timelapseBinding.ResetBindings(false);
                    SetStatus($"Analyse du dossier images : {item.TaskName}");

                    var frames = await ResolveTimelapseFramesAsync(item, token);
                    item.FramePaths = frames.Select(f => f.RemotePath).ToList();
                    item.FrameCount = item.FramePaths.Count;
                    item.Source = item.FrameCount >= 2 ? "Images" : item.Source;
                    item.Status = item.FrameCount >= 2 ? $"{item.FrameCount} images" : "Aucune image";
                    _timelapseBinding.ResetBindings(false);
                }

                ReportTimelapsePhase(item, "Téléchargement", 0, $"0/{item.FramePaths.Count} image(s)");

                var frameFiles = await DownloadTimelapseFramesAsync(http, item, frameFolder, parallelism, token);
                if (frameFiles.Count < 2)
                    throw new InvalidOperationException("Pas assez d'images téléchargées pour créer une vidéo.");

                ReportTimelapsePhase(item, "Encodage", 0, "préparation FFmpeg");

                var outputPath = BuildTimelapsePath(item);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                if (File.Exists(outputPath)) File.Delete(outputPath);

                await EncodeFramesWithFfmpegAsync(ffmpegPath, frameFolder, frameFiles, outputPath, fps, token, item);

                var info = new FileInfo(outputPath);
                if (!info.Exists || info.Length == 0)
                    throw new IOException("FFmpeg n'a pas créé de MP4 exploitable.");

                if (!keepFrames)
                {
                    try { Directory.Delete(frameFolder, true); } catch { }
                }

                item.Status = "OK";
                ok++;
                logLines.Add($"OK PC : {item.TaskName} -> {outputPath} ({info.Length} octets, {frameFiles.Count} images)");
            }
            catch (OperationCanceledException)
            {
                item.Status = "Annulé";
                logLines.Add($"ANNULÉ : {item.TaskName}");
                break;
            }
            catch (Exception ex)
            {
                item.Status = "Échec";
                ko++;
                logLines.Add($"ÉCHEC : {item.TaskName} | {ex.Message}");
            }
            finally
            {
                _progress.Value = Math.Min(i + 1, _progress.Maximum);
                _timelapseBinding.ResetBindings(false);
            }
        }

        logLines.Add(new string('-', 70));
        logLines.Add($"Résultat : {ok} réussi(s), {ko} échec(s)");
        var logPath = Path.Combine(_timelapseFolder, "centauri_timelapse_pc_fast_log.txt");
        await File.WriteAllLinesAsync(logPath, logLines, Encoding.UTF8);

        SetDownloadUiState(false);
        SetStatus($"Terminé : {ok} vidéo(s), {ko} échec(s). Dossier : {_timelapseFolder}");

        var icon = ko == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
        var result = MessageBox.Show(
            $"Création vidéo PC terminée.\n\nRéussis : {ok}\nÉchecs : {ko}\n\nOuvrir le dossier ?",
            "Timelapses",
            MessageBoxButtons.YesNo,
            icon);
        if (result == DialogResult.Yes) OpenDownloadFolder(_timelapseFolder);
    }

    private async Task<List<string>> DownloadTimelapseFramesAsync(HttpClient http, TimelapseItem item, string frameFolder, int parallelism, CancellationToken token)
    {
        var orderedPaths = item.FramePaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, Comparer<string>.Create(NaturalCompare))
            .ToList();

        var outputFiles = new string[orderedPaths.Count];
        var completed = 0;
        using var semaphore = new SemaphoreSlim(parallelism);
        var tasks = new List<Task>();

        for (var index = 0; index < orderedPaths.Count; index++)
        {
            var localIndex = index;
            var remotePath = orderedPaths[index];
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync(token);
                try
                {
                    token.ThrowIfCancellationRequested();
                    var ext = Path.GetExtension(remotePath.Split('?', '#')[0]);
                    if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8) ext = ".jpg";
                    var localPath = Path.Combine(frameFolder, $"frame_{localIndex + 1:000000}{ext.ToLowerInvariant()}");
                    outputFiles[localIndex] = localPath;

                    if (!File.Exists(localPath) || new FileInfo(localPath).Length == 0)
                    {
                        var url = BuildPrinterHttpUrl(remotePath);
                        await DownloadFileWithRetriesAsync(http, url, localPath, token, 4);
                    }

                    var done = Interlocked.Increment(ref completed);
                    var step = Math.Max(1, orderedPaths.Count / 100);
                    if (done == orderedPaths.Count || done % step == 0 || done <= Math.Min(5, orderedPaths.Count))
                    {
                        var percent = orderedPaths.Count == 0 ? 100 : (int)Math.Round(done * 100.0 / orderedPaths.Count);
                        ReportTimelapsePhase(item, "Téléchargement", percent, $"{done}/{orderedPaths.Count} image(s)");
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }, token));
        }

        await Task.WhenAll(tasks);
        return outputFiles.Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f)).ToList();
    }

    private async Task EncodeFramesWithFfmpegAsync(string ffmpegPath, string frameFolder, List<string> frameFiles, string outputPath, int fps, CancellationToken token, TimelapseItem item)
    {
        var listPath = Path.Combine(frameFolder, "frames.txt");
        var duration = 1.0 / Math.Max(1, fps);

        await using (var writer = new StreamWriter(listPath, false, new UTF8Encoding(false)))
        {
            foreach (var frame in frameFiles)
            {
                var fileName = Path.GetFileName(frame).Replace("'", "'\\''");
                await writer.WriteLineAsync($"file '{fileName}'");
                await writer.WriteLineAsync($"duration {duration.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }
            var last = Path.GetFileName(frameFiles[^1]).Replace("'", "'\\''");
            await writer.WriteLineAsync($"file '{last}'");
        }

        var args = $"-y -hide_banner -loglevel error -progress pipe:1 -nostats -f concat -safe 0 -i \"{listPath}\" -vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2,fps={fps},format=yuv420p\" -c:v libx264 -preset veryfast -crf 22 -movflags +faststart \"{outputPath}\"";
        await RunFfmpegWithProgressAsync(ffmpegPath, args, frameFolder, token, item, frameFiles.Count);
    }

    private async Task RunFfmpegWithProgressAsync(string exePath, string args, string workingDirectory, CancellationToken token, TimelapseItem item, int totalFrames)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        if (!process.Start()) throw new InvalidOperationException("Impossible de lancer FFmpeg.");

        var stderrTask = process.StandardError.ReadToEndAsync();
        var stdoutTask = Task.Run(async () =>
        {
            while (await process.StandardOutput.ReadLineAsync() is { } line)
            {
                token.ThrowIfCancellationRequested();
                if (line.StartsWith("frame=", StringComparison.OrdinalIgnoreCase))
                {
                    var value = line[6..].Trim();
                    if (int.TryParse(value, out var frame))
                    {
                        var percent = totalFrames <= 0 ? 0 : Math.Min(99, (int)Math.Round(frame * 100.0 / totalFrames));
                        ReportTimelapsePhase(item, "Encodage", percent, $"{Math.Min(frame, totalFrames)}/{totalFrames} image(s)");
                    }
                }
                else if (line.Equals("progress=end", StringComparison.OrdinalIgnoreCase))
                {
                    ReportTimelapsePhase(item, "Encodage", 100, $"{totalFrames}/{totalFrames} image(s)");
                }
            }
        }, token);

        try
        {
            await process.WaitForExitAsync(token);
            await stdoutTask;
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch { }
            throw;
        }

        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            var details = stderr.Trim();
            if (details.Length > 1200) details = details[^1200..];
            throw new InvalidOperationException("FFmpeg a échoué." + (string.IsNullOrWhiteSpace(details) ? string.Empty : $" Détail : {details}"));
        }

        ReportTimelapsePhase(item, "Encodage", 100, $"{totalFrames}/{totalFrames} image(s)");
    }

    private async Task DownloadTimelapseSelectedAsync()
    {
        if (!EnsureConnected()) return;

        _timelapseGrid.EndEdit();
        var selected = GetSelectedTimelapses().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Aucune vidéo sélectionnée.", "Téléchargement", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await ExportAndDownloadTimelapseSequenceAsync(selected);
    }

    private async Task ExportAndDownloadTimelapseSequenceAsync(List<TimelapseItem> selected)
    {
        Directory.CreateDirectory(_timelapseFolder);
        _downloadCts?.Dispose();
        _downloadCts = new CancellationTokenSource();
        var token = _downloadCts.Token;

        _progress.Value = 0;
        _progress.Maximum = selected.Count;
        SetDownloadUiState(true);

        var ok = 0;
        var ko = 0;
        var logLines = new List<string>
        {
            $"Centauri Carbon Downloader - Timelapses - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Imprimante : {_host}",
            $"Dossier : {_timelapseFolder}",
            new string('-', 70)
        };

        using var http = new HttpClient { Timeout = LongTransferTimeout };

        for (var i = 0; i < selected.Count; i++)
        {
            var item = selected[i];
            if (token.IsCancellationRequested)
            {
                item.Status = "Annulé";
                logLines.Add($"ANNULÉ : {item.TaskName}");
                break;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(item.VideoUrl) && !string.IsNullOrWhiteSpace(item.RemoteMp4Path))
                    item.VideoUrl = BuildPrinterHttpUrl(item.RemoteMp4Path);

                if (string.IsNullOrWhiteSpace(item.VideoUrl))
                    throw new InvalidOperationException("URL vidéo absente.");

                item.Status = item.NeedsExport ? "Export demandé" : "Vérification";
                _timelapseBinding.ResetBindings(false);
                SetStatus($"Timelapse {i + 1}/{selected.Count} : {item.TaskName}");

                if (item.NeedsExport || !await TryHttpExistsAsync(http, item.VideoUrl, token))
                {
                    if (string.IsNullOrWhiteSpace(item.RemoteMp4Path))
                        item.RemoteMp4Path = RemotePathFromHttpUrl(item.VideoUrl);

                    if (string.IsNullOrWhiteSpace(item.RemoteMp4Path))
                        throw new InvalidOperationException("Chemin MP4 distant introuvable.");

                    var ready = await ExportTimelapseAndWaitAsync(item, http, PrinterExportTimeout, token);
                    if (!ready) throw new TimeoutException("Export trop long ou non confirmé par l'imprimante.");
                }

                item.Status = "Téléchargement";
                _timelapseBinding.ResetBindings(false);

                var localPath = BuildTimelapsePath(item);
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                await DownloadFileWithRetriesAsync(http, item.VideoUrl, localPath, token, 5);

                var info = new FileInfo(localPath);
                if (!info.Exists || info.Length == 0)
                    throw new IOException("Le fichier reçu est vide.");

                item.Status = "OK";
                item.NeedsExport = false;
                ok++;
                logLines.Add($"OK : {item.TaskName} -> {localPath} ({info.Length} octets)");
            }
            catch (OperationCanceledException)
            {
                item.Status = "Annulé";
                logLines.Add($"ANNULÉ : {item.TaskName}");
                break;
            }
            catch (Exception ex)
            {
                item.Status = "Échec";
                ko++;
                logLines.Add($"ÉCHEC : {item.TaskName} | {ex.Message}");
            }
            finally
            {
                _progress.Value = Math.Min(i + 1, _progress.Maximum);
                _timelapseBinding.ResetBindings(false);
            }
        }

        logLines.Add(new string('-', 70));
        logLines.Add($"Résultat : {ok} réussi(s), {ko} échec(s)");
        var logPath = Path.Combine(_timelapseFolder, "centauri_timelapse_export_download_log.txt");
        await File.WriteAllLinesAsync(logPath, logLines, Encoding.UTF8);

        SetDownloadUiState(false);
        SetStatus($"Terminé : {ok} vidéo(s), {ko} échec(s). Dossier : {_timelapseFolder}");

        var icon = ko == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
        var result = MessageBox.Show(
            $"Export + téléchargement terminé.\n\nRéussis : {ok}\nÉchecs : {ko}\n\nOuvrir le dossier ?",
            "Timelapses",
            MessageBoxButtons.YesNo,
            icon);
        if (result == DialogResult.Yes) OpenDownloadFolder(_timelapseFolder);
    }

    private async Task<bool> ExportTimelapseAndWaitAsync(TimelapseItem item, HttpClient http, TimeSpan timeout, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(item.RemoteMp4Path)) return false;

        if (await TryHttpExistsAsync(http, item.VideoUrl, token))
        {
            item.Status = "MP4 déjà prêt";
            _timelapseBinding.ResetBindings(false);
            return true;
        }

        var key = NormalizeRemotePathKey(item.RemoteMp4Path);
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_timelapseExportWaitersLock)
        {
            _timelapseExportWaiters[key] = tcs;
        }

        try
        {
            var arr = new JsonArray { item.RemoteMp4Path };
            await SendCommandAsync(323, new JsonObject { ["Url"] = arr });

            var start = DateTime.UtcNow;
            var nextPing = DateTime.UtcNow.AddSeconds(15);
            var pollDelay = TimeSpan.FromSeconds(2);

            while (DateTime.UtcNow - start < timeout)
            {
                token.ThrowIfCancellationRequested();

                item.Status = $"Export en cours ({Math.Max(0, (int)(timeout - (DateTime.UtcNow - start)).TotalSeconds)}s)";
                _timelapseBinding.ResetBindings(false);

                var waitTask = Task.Delay(pollDelay, token);
                var completed = await Task.WhenAny(tcs.Task, waitTask);
                if (completed == tcs.Task && await tcs.Task)
                {
                    await Task.Delay(1200, token);
                    return true;
                }

                if (await TryHttpExistsAsync(http, item.VideoUrl, token)) return true;

                if (DateTime.UtcNow >= nextPing)
                {
                    await SendRawTextAsync("ping", token);
                    nextPing = DateTime.UtcNow.AddSeconds(15);
                }

                pollDelay = TimeSpan.FromMilliseconds(Math.Min(pollDelay.TotalMilliseconds * 1.35, 5000));
            }

            return await TryHttpExistsAsync(http, item.VideoUrl, token);
        }
        finally
        {
            lock (_timelapseExportWaitersLock)
            {
                _timelapseExportWaiters.Remove(key);
            }
        }
    }

    private async Task<List<TimelapseItem>> FetchTimelapseFolderListAsync()
    {
        // v0.4.2 : FFmpeg peut être intégré dans l'EXE ; v0.4.1 : scan rapide. On ne descend plus récursivement dans tous les dossiers au chargement,
        // car /local/aic_tlp/ peut contenir beaucoup d'images. On liste seulement la racine en WebSocket,
        // puis on analysera les images d'un timelapse uniquement quand l'utilisateur le sélectionne.
        var results = new List<TimelapseItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var rootEntries = await ListDirectoryViaWsAsync("/local/aic_tlp/", CancellationToken.None);
            AddTimelapseItemsFromDirectory("/local/aic_tlp/", rootEntries, results, seen, allowFolderPlaceholders: true);

            // Si la racine ne contient qu'un dossier conteneur ou si les images sont directement à la racine,
            // ce premier passage suffit à afficher quelque chose immédiatement. Les sous-dossiers seront détaillés à la demande.
            if (results.Count > 0) return results;
        }
        catch (Exception ex)
        {
            SetStatus($"Scan rapide WebSocket impossible ({ex.Message}). Fallback HTTP limité...");
        }

        // Fallback limité : un seul niveau HTTP pour éviter les scans interminables.
        using var http = new HttpClient { Timeout = HttpProbeTimeout };
        try
        {
            var html = await http.GetStringAsync(BuildPrinterHttpUrl("/local/aic_tlp/"));
            var entries = ParseRemoteDirectoryListing(html, "/local/aic_tlp/");
            AddTimelapseItemsFromDirectory("/local/aic_tlp/", entries, results, seen, allowFolderPlaceholders: true);
        }
        catch
        {
            // Le fallback historique prendra la suite côté appelant.
        }

        return results;
    }

    private void AddTimelapseItemsFromDirectory(string folderPath, List<RemoteDirectoryEntry> entries, List<TimelapseItem> results, HashSet<string> seen, bool allowFolderPlaceholders)
    {
        var images = entries
            .Where(e => e.IsImage)
            .Select(e => e.RemotePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, Comparer<string>.Create(NaturalCompare))
            .ToList();

        foreach (var mp4 in entries.Where(e => e.IsMp4))
        {
            var key = NormalizeRemotePathKey(mp4.RemotePath);
            if (!seen.Add(key)) continue;

            var name = CleanTimelapseDisplayName(mp4.Label, mp4.RemotePath);
            results.Add(new TimelapseItem
            {
                Selected = false,
                TaskId = key,
                TaskName = name,
                DateText = mp4.ModifiedUnix.HasValue ? FormatUnixDate(mp4.ModifiedUnix.Value) : string.Empty,
                DurationText = string.Empty,
                VideoUrl = BuildPrinterHttpUrl(mp4.RemotePath),
                RemoteMp4Path = mp4.RemotePath,
                RemoteFolderPath = Path.GetDirectoryName(mp4.RemotePath)?.Replace('\\', '/') ?? folderPath,
                FrameCount = 0,
                FramePaths = new List<string>(),
                NeedsExport = false,
                Source = "MP4",
                Status = "MP4 disponible"
            });
        }

        if (images.Count >= 2)
        {
            var folderKey = NormalizeRemotePathKey(folderPath);
            var remoteMp4Path = folderPath.TrimEnd('/') + ".mp4";
            var alreadyHasExpectedMp4 = entries.Any(e => e.IsMp4 && NormalizeRemotePathKey(e.RemotePath) == NormalizeRemotePathKey(remoteMp4Path));
            if (!alreadyHasExpectedMp4 && seen.Add(folderKey + "#frames"))
            {
                var displayName = CleanTimelapseDisplayName(Path.GetFileName(folderPath.TrimEnd('/')), folderPath);
                var modified = entries.Where(e => e.IsImage && e.ModifiedUnix.HasValue).Select(e => e.ModifiedUnix!.Value).DefaultIfEmpty(0).Max();

                results.Add(new TimelapseItem
                {
                    Selected = false,
                    TaskId = folderKey,
                    TaskName = displayName,
                    DateText = modified > 0 ? FormatUnixDate(modified) : string.Empty,
                    DurationText = string.Empty,
                    VideoUrl = BuildPrinterHttpUrl(remoteMp4Path),
                    RemoteMp4Path = remoteMp4Path,
                    RemoteFolderPath = folderPath,
                    FrameCount = images.Count,
                    FramePaths = images,
                    NeedsExport = false,
                    Source = "Images",
                    Status = $"{images.Count} images"
                });
            }
        }

        if (!allowFolderPlaceholders) return;

        foreach (var dir in entries.Where(e => e.IsDirectory))
        {
            var next = dir.RemotePath;
            if (string.IsNullOrWhiteSpace(next)) continue;
            if (!next.EndsWith('/')) next += "/";
            if (NormalizeRemotePathKey(next) == NormalizeRemotePathKey(folderPath)) continue;

            var key = NormalizeRemotePathKey(next);
            if (!seen.Add(key + "#folder")) continue;

            var displayName = CleanTimelapseDisplayName(dir.Label, next);
            var remoteMp4Path = next.TrimEnd('/') + ".mp4";
            results.Add(new TimelapseItem
            {
                Selected = false,
                TaskId = key,
                TaskName = displayName,
                DateText = dir.ModifiedUnix.HasValue ? FormatUnixDate(dir.ModifiedUnix.Value) : string.Empty,
                DurationText = string.Empty,
                VideoUrl = BuildPrinterHttpUrl(remoteMp4Path),
                RemoteMp4Path = remoteMp4Path,
                RemoteFolderPath = next,
                FrameCount = 0,
                FramePaths = new List<string>(),
                NeedsExport = false,
                Source = "Dossier images",
                Status = "À analyser"
            });
        }
    }

    private async Task<List<RemoteDirectoryEntry>> ResolveTimelapseFramesAsync(TimelapseItem item, CancellationToken token)
    {
        // v0.4.5 : résolution plus tolérante. Certains firmwares exposent les frames sous un sous-dossier,
        // d'autres renvoient seulement un nom de timelapse depuis la racine. On essaye plusieurs dossiers candidats.
        var candidateFolders = new List<string>();

        void AddCandidate(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            path = path.Replace('\\', '/').Trim();
            if (path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                path = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "/local/aic_tlp/";
            if (!path.StartsWith("/")) path = "/" + path;
            if (!path.EndsWith('/')) path += "/";
            if (!candidateFolders.Any(p => NormalizeRemotePathKey(p) == NormalizeRemotePathKey(path))) candidateFolders.Add(path);
        }

        AddCandidate(item.RemoteFolderPath);
        AddCandidate(item.RemoteMp4Path);
        AddCandidate($"/local/aic_tlp/{item.TaskName}");
        AddCandidate($"/local/aic_tlp/{Path.GetFileName(item.TaskId.TrimEnd('/'))}");

        var debugLines = new List<string>
        {
            new string('-', 70),
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} — {item.TaskName}",
            $"TaskId={item.TaskId}",
            $"RemoteFolderPath={item.RemoteFolderPath}",
            $"RemoteMp4Path={item.RemoteMp4Path}"
        };

        foreach (var folder in candidateFolders)
        {
            token.ThrowIfCancellationRequested();
            ReportTimelapsePhase(item, "Analyse", 0, $"dossier {folder}");
            var entries = await ListDirectoryTreeViaWsAsync(
                folder,
                maxDepth: 3,
                token,
                progress: p => ReportTimelapsePhase(item, "Analyse", p.Percent, $"{p.VisitedFolders} dossier(s), {p.ImagesFound} image(s)"),
                stopWhenImagesFound: true);
            var images = ExtractImageEntries(entries);
            debugLines.Add($"Candidate={folder} | entries={entries.Count} | images={images.Count}");
            if (images.Count >= 2)
            {
                ReportTimelapsePhase(item, "Analyse", 100, $"{images.Count} image(s) détectée(s)");
                return images;
            }
        }

        // Dernier recours contrôlé : scan de la racine, puis filtrage par nom/suffixe du timelapse.
        ReportTimelapsePhase(item, "Analyse", 0, "recherche de secours");
        var rootEntries = await ListDirectoryTreeViaWsAsync(
            "/local/aic_tlp/",
            maxDepth: 2,
            token,
            progress: p => ReportTimelapsePhase(item, "Analyse", p.Percent, $"secours : {p.VisitedFolders} dossier(s), {p.ImagesFound} image(s)"),
            stopWhenImagesFound: false);
        var needle = NormalizeSearchNeedle(item.TaskName);
        var idNeedle = NormalizeSearchNeedle(Path.GetFileName(item.TaskId.TrimEnd('/')));
        var allRootImages = ExtractImageEntries(rootEntries);
        debugLines.Add($"Root fallback /local/aic_tlp/ | entries={rootEntries.Count} | allImages={allRootImages.Count} | needle={needle} | idNeedle={idNeedle}");
        var filtered = allRootImages
            .Where(e =>
            {
                var p = NormalizeSearchNeedle(e.RemotePath);
                return (!string.IsNullOrWhiteSpace(needle) && p.Contains(needle, StringComparison.OrdinalIgnoreCase)) ||
                       (!string.IsNullOrWhiteSpace(idNeedle) && p.Contains(idNeedle, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();
        debugLines.Add($"FilteredImages={filtered.Count}");

        if (filtered.Count >= 2) return filtered;

        try
        {
            Directory.CreateDirectory(_timelapseFolder);
            File.AppendAllLines(Path.Combine(_timelapseFolder, "centauri_timelapse_scan_debug.txt"), debugLines, Encoding.UTF8);
        }
        catch { }

        return new List<RemoteDirectoryEntry>();
    }

    private static List<RemoteDirectoryEntry> ExtractImageEntries(IEnumerable<RemoteDirectoryEntry> entries)
    {
        return entries.Where(e => e.IsImage && !string.IsNullOrWhiteSpace(e.RemotePath))
            .GroupBy(e => NormalizeRemotePathKey(e.RemotePath), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(e => e.RemotePath, Comparer<string>.Create(NaturalCompare))
            .ToList();
    }

    private async Task<List<RemoteDirectoryEntry>> ListDirectoryTreeViaWsAsync(string rootFolder, int maxDepth, CancellationToken token, Action<ScanProgressInfo>? progress = null, bool stopWhenImagesFound = false)
    {
        // v0.4.5 : scan hybride WS + HTTP.
        // Sur certains firmwares, Cmd 258 répond sur la racine mais renvoie une liste vide ou mal typée
        // dans les sous-dossiers /local/aic_tlp/. On tente donc aussi le listing HTTP quand le résultat WS
        // ne contient pas d'image ou pas d'entrée exploitable.
        var all = new List<RemoteDirectoryEntry>();
        var allSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<(string Path, int Depth)>();
        queue.Enqueue((rootFolder, 0));

        while (queue.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            var (folder, depth) = queue.Dequeue();
            if (!folder.EndsWith('/')) folder += "/";
            var key = NormalizeRemotePathKey(folder);
            if (!visited.Add(key)) continue;

            progress?.Invoke(new ScanProgressInfo
            {
                CurrentFolder = folder,
                VisitedFolders = visited.Count,
                QueuedFolders = queue.Count,
                ImagesFound = all.Count(e => e.IsImage)
            });

            var entries = new List<RemoteDirectoryEntry>();
            try
            {
                entries = await ListDirectoryViaWsAsync(folder, token);
            }
            catch
            {
                // Le fallback HTTP est tenté juste après.
            }

            if (entries.Count == 0 || !entries.Any(e => e.IsImage || e.IsDirectory || e.IsMp4))
            {
                var httpEntries = await ListDirectoryViaHttpAsync(folder, token);
                entries = MergeRemoteEntries(entries, httpEntries);
            }
            else if (!entries.Any(e => e.IsImage))
            {
                // Même si WS renvoie des dossiers, HTTP peut exposer les fichiers image directement.
                var httpEntries = await ListDirectoryViaHttpAsync(folder, token);
                if (httpEntries.Any(e => e.IsImage)) entries = MergeRemoteEntries(entries, httpEntries);
            }

            foreach (var entry in entries)
            {
                if (allSeen.Add(NormalizeRemotePathKey(entry.RemotePath))) all.Add(entry);
            }

            progress?.Invoke(new ScanProgressInfo
            {
                CurrentFolder = folder,
                VisitedFolders = visited.Count,
                QueuedFolders = queue.Count,
                ImagesFound = all.Count(e => e.IsImage)
            });

            if (stopWhenImagesFound && entries.Any(e => e.IsImage))
                return all;

            if (depth >= maxDepth) continue;
            foreach (var dir in entries.Where(e => e.IsDirectory))
            {
                var next = dir.RemotePath;
                if (string.IsNullOrWhiteSpace(next)) continue;
                if (!next.EndsWith('/')) next += "/";
                if (!visited.Contains(NormalizeRemotePathKey(next))) queue.Enqueue((next, depth + 1));
            }
        }

        return all;
    }

    private static List<RemoteDirectoryEntry> MergeRemoteEntries(params IEnumerable<RemoteDirectoryEntry>[] lists)
    {
        var merged = new List<RemoteDirectoryEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var list in lists)
        {
            foreach (var entry in list)
            {
                if (string.IsNullOrWhiteSpace(entry.RemotePath)) continue;
                if (seen.Add(NormalizeRemotePathKey(entry.RemotePath))) merged.Add(entry);
            }
        }
        return merged;
    }

    private async Task<List<RemoteDirectoryEntry>> ListDirectoryViaHttpAsync(string folderPath, CancellationToken token)
    {
        try
        {
            using var http = new HttpClient { Timeout = HttpProbeTimeout };
            var html = await http.GetStringAsync(BuildPrinterHttpUrl(folderPath), token);
            return ParseRemoteDirectoryListing(html, folderPath);
        }
        catch
        {
            return new List<RemoteDirectoryEntry>();
        }
    }

    private async Task<List<RemoteDirectoryEntry>> ListDirectoryViaWsAsync(string folderPath, CancellationToken token)
    {
        if (!folderPath.StartsWith("/")) folderPath = "/" + folderPath;
        if (!folderPath.EndsWith('/')) folderPath += "/";

        var rootData = await SendCommandForResponseAsync(258, new JsonObject { ["Url"] = folderPath.TrimEnd('/') }, WebSocketCommandTimeout, token);
        var entries = new List<RemoteDirectoryEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (rootData.TryGetProperty("Data", out var payload) &&
            payload.TryGetProperty("FileList", out var list) &&
            list.ValueKind == JsonValueKind.Array)
        {
            foreach (var file in list.EnumerateArray())
            {
                if (TryCreateRemoteEntryFromFileList(folderPath, file, out var entry) && seen.Add(entry.RemotePath))
                    entries.Add(entry);
            }
        }

        return entries;
    }

    private bool TryCreateRemoteEntryFromFileList(string listPath, JsonElement item, out RemoteDirectoryEntry entry)
    {
        entry = new RemoteDirectoryEntry();
        var name = GetStringProperty(item, "name")
            ?? GetStringProperty(item, "Name")
            ?? GetStringProperty(item, "filename")
            ?? GetStringProperty(item, "FileName")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name)) return false;

        var remotePath = name.Replace('\\', '/').Trim();
        try { remotePath = Uri.UnescapeDataString(remotePath); } catch { }

        if (!remotePath.StartsWith("/local/", StringComparison.OrdinalIgnoreCase) &&
            !remotePath.StartsWith("/usb/", StringComparison.OrdinalIgnoreCase) &&
            !remotePath.StartsWith("local/", StringComparison.OrdinalIgnoreCase) &&
            !remotePath.StartsWith("usb/", StringComparison.OrdinalIgnoreCase))
        {
            remotePath = ResolveRemotePath(listPath, remotePath);
        }
        else if (!remotePath.StartsWith("/"))
        {
            remotePath = "/" + remotePath;
        }

        remotePath = remotePath.Split('?', '#')[0].Replace('\\', '/');
        var type = GetIntProperty(item, "type") ?? GetIntProperty(item, "Type");
        var ext = Path.GetExtension(remotePath).ToLowerInvariant();
        var isImage = IsImageExtension(ext) || LooksLikeTimelapseFrameName(remotePath);
        var isMp4 = ext == ".mp4";

        // v0.4.5 : ne jamais transformer un fichier image/MP4 en dossier à cause du champ type.
        // Le champ type n'est pas fiable selon les firmwares et peut valoir 0 sur des fichiers.
        var isKnownFile = isImage || isMp4;
        var isDirectory = !isKnownFile && (
            remotePath.EndsWith('/') ||
            string.IsNullOrWhiteSpace(ext) ||
            (type.HasValue && type.Value != 1)
        );
        if (isDirectory && !remotePath.EndsWith('/')) remotePath += "/";

        entry = new RemoteDirectoryEntry
        {
            Href = name,
            Label = Path.GetFileName(remotePath.TrimEnd('/')),
            RemotePath = remotePath,
            RowHtml = string.Empty,
            IsDirectory = isDirectory,
            IsImage = isImage,
            IsMp4 = isMp4,
            ModifiedUnix = GetLongProperty(item, "time")
                ?? GetLongProperty(item, "Time")
                ?? GetLongProperty(item, "mTime")
                ?? GetLongProperty(item, "mtime")
                ?? GetLongProperty(item, "lastModified")
        };
        return true;
    }

    private List<RemoteDirectoryEntry> ParseRemoteDirectoryListing(string html, string listPath)
    {
        var entries = new List<RemoteDirectoryEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rowMatches = Regex.Matches(html, "<tr[^>]*>(?<row>[\\s\\S]*?)</tr>", RegexOptions.IgnoreCase);

        if (rowMatches.Count > 0)
        {
            foreach (Match rowMatch in rowMatches)
            {
                var row = rowMatch.Groups["row"].Value;
                foreach (Match link in Regex.Matches(row, "<a[^>]+href=[\"'](?<href>[^\"']+)[\"'][^>]*>(?<label>[\\s\\S]*?)</a>", RegexOptions.IgnoreCase))
                {
                    if (TryCreateRemoteEntry(listPath, row, link.Groups["href"].Value, link.Groups["label"].Value, out var entry) && seen.Add(entry.RemotePath))
                        entries.Add(entry);
                }
            }
        }

        if (entries.Count == 0)
        {
            foreach (Match link in Regex.Matches(html, "<a[^>]+href=[\"'](?<href>[^\"']+)[\"'][^>]*>(?<label>[\\s\\S]*?)</a>", RegexOptions.IgnoreCase))
            {
                if (TryCreateRemoteEntry(listPath, string.Empty, link.Groups["href"].Value, link.Groups["label"].Value, out var entry) && seen.Add(entry.RemotePath))
                    entries.Add(entry);
            }
        }

        return entries;
    }

    private static bool RowLooksLikeDirectory(string rowHtml, string href, string label)
    {
        if (!string.IsNullOrWhiteSpace(href) && href.Trim().EndsWith('/')) return true;
        if (!string.IsNullOrWhiteSpace(label) && label.Trim().Equals("[DIR]", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.IsNullOrWhiteSpace(rowHtml)) return false;

        var clean = StripHtml(WebUtility.HtmlDecode(rowHtml)).Replace('\u00a0', ' ');
        if (clean.Contains("[DIR]", StringComparison.OrdinalIgnoreCase)) return true;

        // Si la ligne contient explicitement une taille numérique, c'est un fichier, pas un dossier.
        // Exemple observé : tlp_layer_1 1770763422 38111
        var tokens = Regex.Matches(clean, @"\b\d+\b").Select(m => m.Value).ToList();
        if (tokens.Count >= 2) return false;

        return false;
    }

    private bool TryCreateRemoteEntry(string listPath, string rowHtml, string hrefRaw, string labelRaw, out RemoteDirectoryEntry entry)
    {
        entry = new RemoteDirectoryEntry();
        var href = WebUtility.HtmlDecode(hrefRaw ?? string.Empty).Trim();
        var label = StripHtml(WebUtility.HtmlDecode(labelRaw ?? string.Empty)).Trim();

        if (string.IsNullOrWhiteSpace(href)) return false;
        if (href is "." or ".." or "./" or "../") return false;
        if (href.StartsWith("#") || href.StartsWith("?")) return false;
        if (label.Equals("Parent Directory", StringComparison.OrdinalIgnoreCase) || label.Contains("parent", StringComparison.OrdinalIgnoreCase)) return false;

        var remotePath = ResolveRemotePath(listPath, href).Split('?', '#')[0].Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(remotePath)) return false;

        var ext = Path.GetExtension(remotePath).ToLowerInvariant();
        var isImage = IsImageExtension(ext) || LooksLikeTimelapseFrameName(remotePath);
        var isMp4 = ext == ".mp4";

        // Mongoose affiche les dossiers avec [DIR]. Les frames tlp_layer_* n'ont pas d'extension,
        // mais elles ont une taille numérique : il ne faut donc pas leur ajouter un slash.
        var rowSaysDirectory = RowLooksLikeDirectory(rowHtml, href, label);
        var isDirectory = !isImage && !isMp4 && rowSaysDirectory;

        // Pour la racine /local/aic_tlp/, les vrais dossiers de timelapse apparaissent souvent
        // sans slash dans le href ; on les garde comme dossiers quand le listing dit [DIR].
        if (isDirectory && !remotePath.EndsWith('/')) remotePath += "/";

        entry = new RemoteDirectoryEntry
        {
            Href = href,
            Label = label,
            RemotePath = remotePath,
            RowHtml = rowHtml,
            IsDirectory = isDirectory,
            IsImage = isImage,
            IsMp4 = isMp4,
            ModifiedUnix = ExtractUnixTimestampFromRow(rowHtml)
        };
        return true;
    }

    private void HandleTimelapseExportAck(JsonElement rootData)
    {
        if (!rootData.TryGetProperty("Data", out var payload)) return;
        if (!payload.TryGetProperty("Url", out var urls) || urls.ValueKind != JsonValueKind.Array) return;

        foreach (var urlElement in urls.EnumerateArray())
        {
            if (urlElement.ValueKind != JsonValueKind.String) continue;
            var key = NormalizeRemotePathKey(urlElement.GetString() ?? string.Empty);
            TaskCompletionSource<bool>? waiter = null;
            lock (_timelapseExportWaitersLock)
            {
                _timelapseExportWaiters.TryGetValue(key, out waiter);
            }
            waiter?.TrySetResult(true);
        }
    }

    private async Task DownloadFileWithRetriesAsync(HttpClient http, string url, string localPath, CancellationToken token, int retries)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();
                await using var source = await response.Content.ReadAsStreamAsync(token);
                await using var destination = File.Create(localPath);
                await source.CopyToAsync(destination, token);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
                if (attempt < retries)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(8, attempt * 1.5)), token);
            }
        }

        throw lastError ?? new IOException("Téléchargement impossible.");
    }

    private async Task<bool> TryHttpExistsAsync(HttpClient http, string url, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(0, 0);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            return ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300);
        }
        catch
        {
            return false;
        }
    }

    private async Task DownloadSequenceAsync<T>(
        List<T> selected,
        string targetFolder,
        Func<T, string> itemName,
        Func<T, string> urlBuilder,
        Func<T, string> pathBuilder,
        Action<T, string> setStatus,
        Action resetBindings,
        string logName)
    {
        Directory.CreateDirectory(targetFolder);
        _downloadCts?.Dispose();
        _downloadCts = new CancellationTokenSource();
        var token = _downloadCts.Token;

        _progress.Value = 0;
        _progress.Maximum = selected.Count;
        SetDownloadUiState(true);

        var ok = 0;
        var ko = 0;
        var logLines = new List<string>
        {
            $"Centauri Carbon Downloader - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Imprimante : {_host}",
            $"Dossier : {targetFolder}",
            new string('-', 70)
        };

        using var http = new HttpClient { Timeout = LongTransferTimeout };

        for (var i = 0; i < selected.Count; i++)
        {
            var item = selected[i];
            var displayName = itemName(item);
            if (token.IsCancellationRequested)
            {
                setStatus(item, "Annulé");
                logLines.Add($"ANNULÉ : {displayName}");
                break;
            }

            setStatus(item, $"Téléchargement {i + 1}/{selected.Count}");
            resetBindings();
            SetStatus($"Téléchargement {i + 1}/{selected.Count} : {displayName}");

            try
            {
                var url = urlBuilder(item);
                if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("URL de téléchargement absente.");

                var localPath = pathBuilder(item);
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                await using (var source = await response.Content.ReadAsStreamAsync(token))
                await using (var destination = File.Create(localPath))
                {
                    await source.CopyToAsync(destination, token);
                }

                var info = new FileInfo(localPath);
                if (!info.Exists || info.Length == 0)
                    throw new IOException("Le fichier reçu est vide.");

                setStatus(item, "OK");
                ok++;
                logLines.Add($"OK : {displayName} -> {localPath} ({info.Length} octets)");
            }
            catch (OperationCanceledException)
            {
                setStatus(item, "Annulé");
                logLines.Add($"ANNULÉ : {displayName}");
                break;
            }
            catch (Exception ex)
            {
                setStatus(item, "Échec");
                ko++;
                logLines.Add($"ÉCHEC : {displayName} | {ex.Message}");
            }
            finally
            {
                _progress.Value = Math.Min(i + 1, _progress.Maximum);
                resetBindings();
            }
        }

        logLines.Add(new string('-', 70));
        logLines.Add($"Résultat : {ok} réussi(s), {ko} échec(s)");
        var logPath = Path.Combine(targetFolder, logName);
        await File.WriteAllLinesAsync(logPath, logLines, Encoding.UTF8);

        SetDownloadUiState(false);
        SetStatus($"Terminé : {ok} réussi(s), {ko} échec(s). Dossier : {targetFolder}");

        var icon = ko == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
        var result = MessageBox.Show(
            $"Téléchargement terminé.\n\nRéussis : {ok}\nÉchecs : {ko}\n\nOuvrir le dossier ?",
            "Téléchargement",
            MessageBoxButtons.YesNo,
            icon);
        if (result == DialogResult.Yes) OpenDownloadFolder(targetFolder);
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        var buffer = new byte[8192];

        while (!token.IsCancellationRequested && _webSocket is { State: WebSocketState.Open } ws)
        {
            try
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(buffer, token);
                    if (result.MessageType == WebSocketMessageType.Close) return;
                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                var json = Encoding.UTF8.GetString(ms.ToArray());
                BeginInvoke(new Action(() => HandleWsMessage(json)));
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() => SetStatus($"Connexion perdue : {ex.Message}")));
                return;
            }
        }
    }

    private void HandleWsMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("Data", out var rootData)) return;
            if (!rootData.TryGetProperty("Cmd", out var cmdElement)) return;
            var cmd = cmdElement.GetInt32();

            if (TryCompleteResponseWaiter(rootData)) return;

            switch (cmd)
            {
                case 258:
                    HandleFileList(rootData);
                    break;
                case 259:
                    HandleDeleteAck(rootData);
                    break;
                case 320:
                    _ = HandleHistoryIdsAsync(rootData);
                    break;
                case 321:
                    HandleHistoryDetails(rootData);
                    break;
                case 323:
                    HandleTimelapseExportAck(rootData);
                    break;
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Réponse imprimante illisible : {ex.Message}");
        }
    }

    private bool TryCompleteResponseWaiter(JsonElement rootData)
    {
        var requestId = GetStringProperty(rootData, "RequestID") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(requestId)) return false;

        TaskCompletionSource<JsonElement>? waiter = null;
        lock (_responseWaitersLock)
        {
            if (_responseWaiters.TryGetValue(requestId, out waiter))
                _responseWaiters.Remove(requestId);
        }

        if (waiter is null) return false;
        waiter.TrySetResult(rootData.Clone());
        return true;
    }

    private void HandleFileList(JsonElement rootData)
    {
        _localFiles.Clear();

        if (rootData.TryGetProperty("Data", out var payload) &&
            payload.TryGetProperty("FileList", out var list) &&
            list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                var name = GetStringProperty(item, "name")
                    ?? GetStringProperty(item, "Name")
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name)) continue;

                var size = GetLongProperty(item, "FileSize")
                    ?? GetLongProperty(item, "size")
                    ?? GetLongProperty(item, "Size")
                    ?? 0;

                _localFiles.Add(new PrinterFile
                {
                    Selected = false,
                    Name = CleanRemoteName(name),
                    Size = size,
                    Status = string.Empty
                });
            }
        }

        _localFiles.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        _localBinding.DataSource = _localFiles;
        _localBinding.ResetBindings(false);
        ApplyLocalFilter();
        SetStatus($"{_localFiles.Count} fichier(s) local(aux) trouvé(s). Dossier G-code : {_gcodeFolder}");
    }

    private async Task HandleHistoryIdsAsync(JsonElement rootData)
    {
        var ids = new List<string>();

        if (rootData.TryGetProperty("Data", out var payload) &&
            payload.TryGetProperty("HistoryData", out var historyData) &&
            historyData.ValueKind == JsonValueKind.Array)
        {
            foreach (var idEl in historyData.EnumerateArray())
            {
                if (idEl.ValueKind == JsonValueKind.String)
                {
                    var id = idEl.GetString();
                    if (!string.IsNullOrWhiteSpace(id)) ids.Add(id);
                }
            }
        }

        if (ids.Count == 0)
        {
            _timelapses.Clear();
            _timelapseBinding.ResetBindings(false);
            SetStatus("Historique vide ou inaccessible.");
            return;
        }

        SetStatus($"{ids.Count} impression(s) dans l'historique. Recherche des vidéos...");

        try
        {
            var arr = new JsonArray();
            foreach (var id in ids) arr.Add(id);
            await SendCommandAsync(321, new JsonObject { ["Id"] = arr });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Impossible de demander le détail de l'historique.\n\nDétail : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void HandleHistoryDetails(JsonElement rootData)
    {
        _timelapses.Clear();
        var total = 0;
        var unavailable = 0;
        var generating = 0;
        var failed = 0;

        if (rootData.TryGetProperty("Data", out var payload) &&
            payload.TryGetProperty("HistoryDetailList", out var list) &&
            list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                total++;
                var videoStatus = GetIntProperty(item, "TimeLapseVideoStatus") ?? 0;
                var videoUrl = GetStringProperty(item, "TimeLapseVideoUrl") ?? string.Empty;

                if (videoStatus == 3) generating++;
                if (videoStatus == 4) failed++;

                if (videoStatus != 1 || string.IsNullOrWhiteSpace(videoUrl))
                {
                    unavailable++;
                    continue;
                }

                var taskId = GetStringProperty(item, "TaskId") ?? string.Empty;
                var taskName = GetStringProperty(item, "TaskName") ?? taskId;
                if (string.IsNullOrWhiteSpace(taskName)) taskName = "timelapse";

                var begin = GetLongProperty(item, "BeginTime") ?? 0;
                var end = GetLongProperty(item, "EndTime") ?? 0;

                _timelapses.Add(new TimelapseItem
                {
                    Selected = false,
                    TaskId = taskId,
                    TaskName = taskName,
                    DateText = FormatUnixDate(begin),
                    DurationText = FormatDuration(begin, end),
                    VideoUrl = videoUrl,
                    RemoteMp4Path = RemotePathFromHttpUrl(videoUrl),
                    NeedsExport = false,
                    Source = "Historique",
                    Status = "MP4 disponible"
                });
            }
        }

        _timelapses.Sort((a, b) => string.Compare(b.DateText, a.DateText, StringComparison.CurrentCultureIgnoreCase));
        _timelapseBinding.DataSource = _timelapses;
        _timelapseBinding.ResetBindings(false);
        ApplyTimelapseFilter();

        var extra = new List<string>();
        if (generating > 0) extra.Add($"{generating} en génération");
        if (failed > 0) extra.Add($"{failed} en échec");
        var suffix = extra.Count > 0 ? " | " + string.Join(" | ", extra) : string.Empty;
        SetStatus($"{_timelapses.Count} timelapse(s) vidéo disponible(s) sur {total} historique(s). {unavailable} sans vidéo téléchargeable.{suffix}");
    }

    private async void HandleDeleteAck(JsonElement rootData)
    {
        var ack = -1;
        if (rootData.TryGetProperty("Data", out var payload) && payload.TryGetProperty("Ack", out var ackEl))
        {
            ack = ackEl.GetInt32();
        }

        if (ack == 0)
        {
            SetStatus("Suppression terminée. Actualisation...");
            await FetchLocalFilesAsync();
        }
        else
        {
            MessageBox.Show($"La suppression a échoué. Ack : {ack}", "Suppression", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task<JsonElement> SendCommandForResponseAsync(int cmd, JsonObject data, TimeSpan timeout, CancellationToken token)
    {
        if (_webSocket is not { State: WebSocketState.Open })
            throw new InvalidOperationException("WebSocket non connecté.");

        var requestId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_responseWaitersLock)
        {
            _responseWaiters[requestId] = tcs;
        }

        try
        {
            var payload = new JsonObject
            {
                ["Id"] = "",
                ["Data"] = new JsonObject
                {
                    ["Cmd"] = cmd,
                    ["Data"] = data,
                    ["RequestID"] = requestId,
                    ["MainboardID"] = "",
                    ["TimeStamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    ["From"] = 1
                }
            };

            var json = payload.ToJsonString();
            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, token);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout, token));
            if (completed != tcs.Task)
                throw new TimeoutException($"Pas de réponse à la commande {cmd} après {timeout.TotalSeconds:0}s.");

            return await tcs.Task;
        }
        finally
        {
            lock (_responseWaitersLock)
            {
                _responseWaiters.Remove(requestId);
            }
        }
    }

    private async Task SendCommandAsync(int cmd, JsonObject data)
    {
        if (_webSocket is not { State: WebSocketState.Open })
            throw new InvalidOperationException("WebSocket non connecté.");

        var payload = new JsonObject
        {
            ["Id"] = "",
            ["Data"] = new JsonObject
            {
                ["Cmd"] = cmd,
                ["Data"] = data,
                ["RequestID"] = Guid.NewGuid().ToString("N"),
                ["MainboardID"] = "",
                ["TimeStamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["From"] = 1
            }
        };

        var json = payload.ToJsonString();
        var bytes = Encoding.UTF8.GetBytes(json);
        await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task SendRawTextAsync(string text, CancellationToken token)
    {
        if (_webSocket is not { State: WebSocketState.Open }) return;
        var bytes = Encoding.UTF8.GetBytes(text);
        await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, token);
    }

    private bool EnsureConnected()
    {
        if (_webSocket is { State: WebSocketState.Open }) return true;
        MessageBox.Show(Tr("msgConnectFirst"), Tr("statusDisconnected"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    private IEnumerable<PrinterFile> GetSelectedLocalFiles()
    {
        _localGrid.EndEdit();
        return _localFiles.Where(f => f.Selected);
    }

    private IEnumerable<TimelapseItem> GetSelectedTimelapses()
    {
        _timelapseGrid.EndEdit();
        return _timelapses.Where(t => t.Selected);
    }

    private void ToggleAllVisible(DataGridView grid, Button button, BindingSource binding, Type itemType)
    {
        var visibleItems = grid.Rows.Cast<DataGridViewRow>()
            .Where(r => r.Visible && r.DataBoundItem is not null)
            .Select(r => r.DataBoundItem!)
            .ToList();

        if (visibleItems.Count == 0) return;

        bool shouldSelect;
        if (itemType == typeof(PrinterFile))
            shouldSelect = visibleItems.Cast<PrinterFile>().Any(f => !f.Selected);
        else
            shouldSelect = visibleItems.Cast<TimelapseItem>().Any(t => !t.Selected);

        foreach (var item in visibleItems)
        {
            if (item is PrinterFile file) file.Selected = shouldSelect;
            if (item is TimelapseItem video) video.Selected = shouldSelect;
        }

        button.Text = shouldSelect ? Tr("unselectAll") : Tr("selectAll");
        binding.ResetBindings(false);
    }

    private void ApplyLocalFilter()
    {
        var query = _filterLocalBox.Text.Trim();
        SafeApplyRowFilter(_localGrid, row =>
        {
            if (row.DataBoundItem is not PrinterFile file) return true;
            return string.IsNullOrWhiteSpace(query)
                || file.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase);
        });
    }

    private void ApplyTimelapseFilter()
    {
        var query = _filterTimelapseBox.Text.Trim();
        SafeApplyRowFilter(_timelapseGrid, row =>
        {
            if (row.DataBoundItem is not TimelapseItem video) return true;
            return string.IsNullOrWhiteSpace(query)
                || video.TaskName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || video.DateText.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || video.RemoteMp4Path.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || video.RemoteFolderPath.Contains(query, StringComparison.CurrentCultureIgnoreCase);
        });
    }

    private static void SafeApplyRowFilter(DataGridView grid, Func<DataGridViewRow, bool> isVisible)
    {
        try
        {
            grid.CurrentCell = null;
            grid.ClearSelection();
        }
        catch
        {
            // Filtering must remain silent.
        }

        foreach (DataGridViewRow row in grid.Rows)
        {
            try
            {
                row.Visible = isVisible(row);
            }
            catch
            {
                // Some WinForms binding states refuse to hide the current row. Ignore and continue.
            }
        }
    }

    private string FindFfmpegExecutable()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "ffmpeg.exe"),
            Path.Combine(baseDir, "tools", "ffmpeg.exe"),
            Path.Combine(Environment.CurrentDirectory, "ffmpeg.exe"),
            Path.Combine(Environment.CurrentDirectory, "tools", "ffmpeg.exe")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate)) return candidate;
        }

        var embedded = TryExtractEmbeddedFfmpeg();
        if (!string.IsNullOrWhiteSpace(embedded)) return embedded;

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var folder in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(folder.Trim(), "ffmpeg.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch
            {
                // Ignore invalid PATH entries.
            }
        }

        return string.Empty;
    }

    private string TryExtractEmbeddedFfmpeg()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => string.Equals(n, "ffmpeg.exe", StringComparison.OrdinalIgnoreCase)
                                     || n.EndsWith(".ffmpeg.exe", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(resourceName)) return string.Empty;

            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null || resourceStream.Length <= 0) return string.Empty;

            var targetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CentauriCarbonDownloader",
                "tools");
            Directory.CreateDirectory(targetDir);

            var targetPath = Path.Combine(targetDir, "ffmpeg.exe");
            var mustExtract = true;

            if (File.Exists(targetPath))
            {
                var info = new FileInfo(targetPath);
                mustExtract = info.Length != resourceStream.Length;
            }

            if (mustExtract)
            {
                resourceStream.Position = 0;
                using var output = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                resourceStream.CopyTo(output);
            }

            return File.Exists(targetPath) ? targetPath : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private int GetConfiguredFps()
    {
        if (!int.TryParse(_fpsBox.Text.Trim(), out var fps)) fps = 30;
        fps = Math.Clamp(fps, 1, 60);
        _fpsBox.Text = fps.ToString();
        return fps;
    }

    private int GetConfiguredParallelism()
    {
        if (!int.TryParse(_parallelBox.Text.Trim(), out var parallelism)) parallelism = 6;
        parallelism = Math.Clamp(parallelism, 1, 16);
        _parallelBox.Text = parallelism.ToString();
        return parallelism;
    }

    private static int NaturalCompare(string? x, string? y)
    {
        x ??= string.Empty;
        y ??= string.Empty;
        var ix = 0;
        var iy = 0;

        while (ix < x.Length && iy < y.Length)
        {
            if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
            {
                long vx = 0;
                while (ix < x.Length && char.IsDigit(x[ix]))
                {
                    vx = Math.Min(long.MaxValue / 10, vx) * 10 + (x[ix] - '0');
                    ix++;
                }

                long vy = 0;
                while (iy < y.Length && char.IsDigit(y[iy]))
                {
                    vy = Math.Min(long.MaxValue / 10, vy) * 10 + (y[iy] - '0');
                    iy++;
                }

                var numeric = vx.CompareTo(vy);
                if (numeric != 0) return numeric;
                continue;
            }

            var cx = char.ToUpperInvariant(x[ix]);
            var cy = char.ToUpperInvariant(y[iy]);
            var cmp = cx.CompareTo(cy);
            if (cmp != 0) return cmp;
            ix++;
            iy++;
        }

        return x.Length.CompareTo(y.Length);
    }

    private static bool IsImageExtension(string ext)
    {
        return ext.ToLowerInvariant() is ".jpg" or ".jpeg" or ".jpe" or ".png" or ".webp" or ".bmp" or ".gif" or ".mjpeg";
    }

    private static bool LooksLikeTimelapseFrameName(string remotePath)
    {
        // Certains firmwares Centauri exposent les frames du timelapse comme des fichiers
        // sans extension, par exemple : tlp_layer_1, tlp_layer_2, etc.
        // Le listing HTTP Mongoose les affiche avec une taille en octets, pas comme [DIR].
        // La v0.4.4 les prenait à tort pour des dossiers, ce qui provoquait un scan interminable
        // du type : Analyse 98% — 705 dossier(s), 0 image(s).
        var path = (remotePath ?? string.Empty).Replace('\\', '/');
        if (!path.Contains("/aic_tlp/", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.EndsWith('/')) return false;

        var name = Path.GetFileName(path.Split('?', '#')[0]);
        if (string.IsNullOrWhiteSpace(name)) return false;
        try { name = Uri.UnescapeDataString(name); } catch { }

        var ext = Path.GetExtension(name).ToLowerInvariant();
        if (IsImageExtension(ext)) return true;

        var stem = string.IsNullOrWhiteSpace(ext) ? name : name[..^ext.Length];

        // Cas observé sur Centauri Carbon : tlp_layer_1, tlp_layer_5, ... sans extension.
        if (Regex.IsMatch(stem, @"^tlp[_-]?layer[_-]?\d+$", RegexOptions.IgnoreCase)) return true;

        // Autres exports possibles : frame_0001, img-0001, capture0001, etc.
        return Regex.IsMatch(stem, @"^(img|image|frame|tlp|capture|snapshot|photo)[_-]?\d+$", RegexOptions.IgnoreCase)
            || Regex.IsMatch(stem, @"^\d{3,}$", RegexOptions.IgnoreCase);
    }

    private static string NormalizeSearchNeedle(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        try { value = Uri.UnescapeDataString(value); } catch { }
        value = Path.GetFileName(value.Replace('\\', '/').TrimEnd('/'));
        if (value.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) value = value[..^4];
        return value.Trim().ToLowerInvariant();
    }

    private static string CleanTimelapseDisplayName(string labelOrName, string fallbackPath)
    {
        var displayName = (labelOrName ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(displayName) || displayName is "." or "..")
            displayName = Path.GetFileName((fallbackPath ?? string.Empty).TrimEnd('/'));
        try { displayName = Uri.UnescapeDataString(displayName); } catch { }
        if (displayName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) displayName = displayName[..^4];
        return string.IsNullOrWhiteSpace(displayName) ? "timelapse" : displayName;
    }

    private string BuildPrinterHttpUrl(string remotePath)
    {
        var clean = (remotePath ?? string.Empty).Trim();
        if (clean.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            clean.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return clean;
        if (!clean.StartsWith("/")) clean = "/" + clean;
        return $"http://{_host}{EncodeRemotePath(clean)}";
    }

    private static string EncodeRemotePath(string path)
    {
        var normalized = path.Replace('\\', '/');
        var parts = normalized.Split('/');
        for (var i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i])) continue;
            try
            {
                parts[i] = Uri.EscapeDataString(Uri.UnescapeDataString(parts[i]));
            }
            catch
            {
                parts[i] = Uri.EscapeDataString(parts[i]);
            }
        }
        return string.Join("/", parts);
    }

    private static string ResolveRemotePath(string listPath, string href)
    {
        if (string.IsNullOrWhiteSpace(href)) return string.Empty;
        href = WebUtility.HtmlDecode(href.Trim()).Replace('\\', '/');

        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
            return absolute.AbsolutePath;

        var noQuery = href.Split('?', '#')[0];
        if (noQuery.StartsWith("/")) return noQuery;

        var basePath = string.IsNullOrWhiteSpace(listPath) ? "/" : listPath.Replace('\\', '/');
        if (!basePath.StartsWith("/")) basePath = "/" + basePath;
        if (!basePath.EndsWith("/")) basePath += "/";
        return basePath + noQuery;
    }

    private static string RemotePathFromHttpUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) return Uri.UnescapeDataString(uri.AbsolutePath);
        return url.StartsWith("/") ? url : "/" + url;
    }

    private static string NormalizeRemotePathKey(string value)
    {
        var path = RemotePathFromHttpUrl(value).Trim().Replace('\\', '/');
        try { path = Uri.UnescapeDataString(path); } catch { }
        return path.TrimEnd('/');
    }

    private static string StripHtml(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return Regex.Replace(value, "<[^>]+>", string.Empty).Trim();
    }

    private static long? ExtractUnixTimestampFromRow(string? rowHtml)
    {
        if (string.IsNullOrWhiteSpace(rowHtml)) return null;
        var match = Regex.Match(rowHtml, "\\bname\\s*=\\s*[\"']?(?<ts>-?\\d+)[\"']?", RegexOptions.IgnoreCase);
        if (match.Success && long.TryParse(match.Groups["ts"].Value, out var ts)) return ts;
        return null;
    }

    private string BuildLocalDownloadUrl(string remoteName)
    {
        var clean = CleanRemoteName(remoteName);
        var encoded = string.Join("/", clean
            .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));
        return $"http://{_host}/local/{encoded}";
    }

    private string BuildLocalGcodePath(string remoteName)
    {
        var clean = CleanRemoteName(remoteName);
        var parts = clean.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p != "." && p != "..")
            .Select(SanitizeFileName)
            .ToArray();

        if (parts.Length == 0)
            parts = new[] { "fichier_sans_nom.gcode" };

        var path = _gcodeFolder;
        foreach (var part in parts) path = Path.Combine(path, part);
        return path;
    }

    private string BuildTimelapsePath(TimelapseItem item)
    {
        var baseName = SanitizeFileName(item.TaskName);
        if (baseName.Length > 90) baseName = baseName[..90].Trim();
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "timelapse";

        var datePart = item.DateText;
        if (!DateTime.TryParse(item.DateText, out var parsedDate)) parsedDate = DateTime.Now;
        datePart = parsedDate.ToString("yyyyMMdd_HHmmss");

        var idPart = string.IsNullOrWhiteSpace(item.TaskId) ? string.Empty : "_" + item.TaskId[..Math.Min(8, item.TaskId.Length)];
        var ext = GetExtensionFromUrl(string.IsNullOrWhiteSpace(item.VideoUrl) ? item.RemoteMp4Path : item.VideoUrl, ".mp4");
        return Path.Combine(_timelapseFolder, $"{datePart}_{baseName}{idPart}{ext}");
    }

    private static string CleanRemoteName(string name)
    {
        var n = name.Trim().Replace('\\', '/');
        if (n.StartsWith("/local/", StringComparison.OrdinalIgnoreCase)) n = n[7..];
        if (n.StartsWith("local/", StringComparison.OrdinalIgnoreCase)) n = n[6..];
        return n.TrimStart('/');
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var cleaned = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "sans_nom" : cleaned;
    }

    private static string NormalizeHost(string input)
    {
        var host = input.Trim();
        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) host = host[7..];
        if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) host = host[8..];
        host = host.Trim('/').Trim();
        return host;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] units = { "B", "KB", "MB", "GB" };
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.#} {units[unit]}";
    }

    private static string FormatUnixDate(long seconds)
    {
        if (seconds <= 0) return string.Empty;
        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string FormatDuration(long begin, long end)
    {
        if (begin <= 0 || end <= begin) return string.Empty;
        var span = TimeSpan.FromSeconds(end - begin);
        return span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes}m" : $"{span.Minutes}m";
    }

    private static string GetExtensionFromUrl(string url, string fallback)
    {
        try
        {
            var uri = new Uri(url);
            var ext = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 8) return ext;
        }
        catch
        {
            // fallback below
        }
        return fallback;
    }

    private static string? GetStringProperty(JsonElement item, string name)
    {
        return item.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static long? GetLongProperty(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var value)) return value;
        if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out value)) return value;
        return null;
    }

    private static int? GetIntProperty(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value)) return value;
        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out value)) return value;
        return null;
    }

    private void RefreshButtonEnabledColors()
    {
        var palette = GetThemePalette();
        foreach (var button in GetAllButtons())
            ApplyButtonTheme(button, palette);

        foreach (var textBox in new[] { _ipBox, _filterLocalBox, _filterTimelapseBox, _fpsBox, _parallelBox })
            ApplyTextBoxTheme(textBox, palette);

        _keepFramesCheckBox.ForeColor = palette.Text;
        _keepFramesCheckBox.BackColor = palette.Back;
    }

    private Button[] GetAllButtons() => new[]
    {
        _connectButton, _openFolderButton, _chooseFolderButton, _cancelButton,
        _refreshLocalButton, _selectAllLocalButton, _downloadLocalButton, _deleteLocalButton,
        _refreshTimelapseButton, _selectAllTimelapseButton, _downloadTimelapseButton, _exportPrinterTimelapseButton
    };

    private readonly record struct ThemePalette(Color Back, Color Panel, Color Surface, Color Input, Color Button, Color ButtonDisabled, Color ButtonHover, Color ButtonDown, Color Text, Color Muted, Color Border, Color Accent, Color Header, Color GridAlt);

    private ThemePalette GetThemePalette()
    {
        if (_darkMode)
        {
            return new ThemePalette(
                Back: Color.FromArgb(11, 15, 20),
                Panel: Color.FromArgb(15, 23, 32),
                Surface: Color.FromArgb(20, 29, 40),
                Input: Color.FromArgb(24, 36, 51),
                Button: Color.FromArgb(30, 42, 58),
                ButtonDisabled: Color.FromArgb(22, 31, 43),
                ButtonHover: Color.FromArgb(40, 55, 75),
                ButtonDown: Color.FromArgb(18, 28, 39),
                Text: Color.FromArgb(235, 241, 247),
                Muted: Color.FromArgb(145, 158, 174),
                Border: Color.FromArgb(63, 78, 96),
                Accent: Color.FromArgb(96, 165, 250),
                Header: Color.FromArgb(24, 36, 51),
                GridAlt: Color.FromArgb(17, 25, 35));
        }

        return new ThemePalette(
            Back: SystemColors.Control,
            Panel: SystemColors.Control,
            Surface: SystemColors.Window,
            Input: SystemColors.Window,
            Button: SystemColors.Control,
            ButtonDisabled: SystemColors.Control,
            ButtonHover: Color.FromArgb(230, 238, 248),
            ButtonDown: Color.FromArgb(210, 225, 245),
            Text: SystemColors.ControlText,
            Muted: SystemColors.GrayText,
            Border: SystemColors.ControlDark,
            Accent: SystemColors.Highlight,
            Header: SystemColors.Control,
            GridAlt: Color.FromArgb(248, 248, 248));
    }

    private void ApplyButtonTheme(Button btn, ThemePalette palette)
    {
        btn.UseVisualStyleBackColor = false;
        btn.FlatStyle = FlatStyle.Flat;
        btn.BackColor = btn.Enabled ? palette.Button : palette.ButtonDisabled;
        btn.ForeColor = btn.Enabled ? palette.Text : palette.Muted;
        btn.FlatAppearance.BorderColor = palette.Border;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = palette.ButtonHover;
        btn.FlatAppearance.MouseDownBackColor = palette.ButtonDown;
        if (btn is ThemedButton themed)
        {
            themed.ThemedBorderColor = palette.Border;
            themed.ThemedDisabledForeColor = palette.Muted;
            themed.ThemedHoverBackColor = palette.ButtonHover;
            themed.ThemedDownBackColor = palette.ButtonDown;
        }
        btn.Invalidate();
    }

    private static void ApplyTextBoxTheme(TextBox tb, ThemePalette palette)
    {
        tb.BackColor = tb.Enabled ? palette.Input : palette.ButtonDisabled;
        tb.ForeColor = tb.Enabled ? palette.Text : palette.Muted;
        tb.BorderStyle = BorderStyle.FixedSingle;
    }

    private void SetConnectedState()
    {
        _connectButton.Text = Tr("disconnect");
        _refreshLocalButton.Enabled = true;
        _downloadLocalButton.Enabled = true;
        _deleteLocalButton.Enabled = true;
        _selectAllLocalButton.Enabled = true;
        _filterLocalBox.Enabled = true;
        _refreshTimelapseButton.Enabled = true;
        _downloadTimelapseButton.Enabled = true;
        _exportPrinterTimelapseButton.Enabled = true;
        _selectAllTimelapseButton.Enabled = true;
        _filterTimelapseBox.Enabled = true;
        _fpsBox.Enabled = true;
        _parallelBox.Enabled = true;
        _keepFramesCheckBox.Enabled = true;
        _openFolderButton.Enabled = true;
        _cancelButton.Enabled = false;
        RefreshButtonEnabledColors();
        SetStatus(string.Format(Tr("statusConnected"), _host));
    }

    private void SetDisconnectedState()
    {
        _connectButton.Text = Tr("connect");
        _refreshLocalButton.Enabled = false;
        _downloadLocalButton.Enabled = false;
        _deleteLocalButton.Enabled = false;
        _selectAllLocalButton.Enabled = false;
        _filterLocalBox.Enabled = false;
        _refreshTimelapseButton.Enabled = false;
        _downloadTimelapseButton.Enabled = false;
        _exportPrinterTimelapseButton.Enabled = false;
        _selectAllTimelapseButton.Enabled = false;
        _filterTimelapseBox.Enabled = false;
        _fpsBox.Enabled = false;
        _parallelBox.Enabled = false;
        _keepFramesCheckBox.Enabled = false;
        _openFolderButton.Enabled = true;
        _cancelButton.Enabled = false;
        RefreshButtonEnabledColors();
        SetStatus(Tr("statusDisconnected"));
    }

    private void SetBusy(bool busy, string? status = null)
    {
        _busy = busy;
        _connectButton.Enabled = !busy;
        var connected = _webSocket is { State: WebSocketState.Open };
        _refreshLocalButton.Enabled = !busy && connected;
        _refreshTimelapseButton.Enabled = !busy && connected;
        RefreshButtonEnabledColors();
        if (status is not null) SetStatus(status);
    }

    private void SetDownloadUiState(bool downloading)
    {
        _cancelButton.Enabled = downloading;
        _downloadLocalButton.Enabled = !downloading;
        _downloadTimelapseButton.Enabled = !downloading;
        _exportPrinterTimelapseButton.Enabled = !downloading;
        _deleteLocalButton.Enabled = !downloading;
        _refreshLocalButton.Enabled = !downloading && _webSocket is { State: WebSocketState.Open };
        _refreshTimelapseButton.Enabled = !downloading && _webSocket is { State: WebSocketState.Open };
        _connectButton.Enabled = !downloading;
        RefreshButtonEnabledColors();
    }

    private void SetStatus(string status)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(() => SetStatus(status))); } catch { }
            return;
        }
        _statusLabel.Text = status;
    }

    private void SetProgressPercent(int percent)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(() => SetProgressPercent(percent))); } catch { }
            return;
        }

        percent = Math.Max(0, Math.Min(100, percent));
        _progress.Style = ProgressBarStyle.Blocks;
        _progress.Maximum = 100;
        _progress.Value = percent;
    }

    private void ReportTimelapsePhase(TimelapseItem item, string phase, int percent, string detail)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(() => ReportTimelapsePhase(item, phase, percent, detail))); } catch { }
            return;
        }

        percent = Math.Max(0, Math.Min(100, percent));
        _progress.Style = ProgressBarStyle.Blocks;
        _progress.Maximum = 100;
        _progress.Value = percent;
        item.Status = string.IsNullOrWhiteSpace(detail)
            ? $"{phase} {percent}%"
            : $"{phase} {percent}% — {detail}";
        _timelapseBinding.ResetBindings(false);
        _statusLabel.Text = $"{phase} {percent}% — {item.TaskName}" + (string.IsNullOrWhiteSpace(detail) ? string.Empty : $" — {detail}");
    }



    private void SetDownloadBaseFolder(string folder, bool updateUi = true)
    {
        if (string.IsNullOrWhiteSpace(folder))
            folder = Path.Combine(GetDownloadsFolder(), "Centauri_Downloads");

        _baseDownloadFolder = folder.Trim();
        _gcodeFolder = Path.Combine(_baseDownloadFolder, "GCode");
        _timelapseFolder = Path.Combine(_baseDownloadFolder, "Timelapses");

        if (updateUi)
            UpdateDownloadFolderLabel();
    }

    private void UpdateDownloadFolderLabel()
    {
        if (_downloadFolderLabel is null || _downloadFolderLabel.IsDisposed) return;
        _downloadFolderLabel.Text = $"{Tr("exportFolder")} {_baseDownloadFolder}";
        _downloadFolderLabel.AutoEllipsis = true;
    }

    private void ChooseDownloadFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = Tr("chooseFolderTitle"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            SelectedPath = Directory.Exists(_baseDownloadFolder) ? _baseDownloadFolder : GetDownloadsFolder()
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        SetDownloadBaseFolder(dialog.SelectedPath);
        SaveUserSettings();
        SetStatus(string.Format(Tr("folderChanged"), _baseDownloadFolder));
    }

    private void Tabs_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _tabs.TabPages.Count) return;

        var palette = GetThemePalette();
        var page = _tabs.TabPages[e.Index];
        var selected = e.Index == _tabs.SelectedIndex;
        var back = selected ? palette.Surface : palette.Panel;

        using var backBrush = new SolidBrush(back);
        using var borderPen = new Pen(palette.Border);
        e.Graphics.FillRectangle(backBrush, e.Bounds);
        e.Graphics.DrawRectangle(borderPen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
        TextRenderer.DrawText(e.Graphics, page.Text, Font, e.Bounds, selected ? palette.Text : palette.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }


    // Créé par ashemka — paramètres UI simples, persistés dans AppData.
    private sealed class UserSettings
    {
        public string Language { get; set; } = "fr";
        public bool DarkMode { get; set; }
        public string ExportFolder { get; set; } = string.Empty;
    }

    private static readonly Dictionary<string, Dictionary<string, string>> UiText = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fr"] = new() { ["windowTitle"] = "Centauri Carbon Downloader {0} — Créé par ashemka", ["language"] = "Langue :", ["darkMode"] = "Mode sombre", ["printerIp"] = "IP imprimante :", ["connect"] = "Connexion", ["disconnect"] = "Déconnecter", ["openFolder"] = "Ouvrir dossier", ["chooseFolder"] = "Choisir dossier", ["exportFolder"] = "Dossier export :" , ["chooseFolderTitle"] = "Choisir le dossier d’export", ["folderChanged"] = "Dossier d’export défini : {0}", ["cancel"] = "Annuler", ["tabGcode"] = "Fichiers G-code", ["tabTimelapse"] = "Timelapses vidéo", ["refresh"] = "Actualiser", ["scanTimelapses"] = "Scanner timelapses", ["selectAll"] = "Tout cocher", ["unselectAll"] = "Tout décocher", ["filterFiles"] = "Filtrer les fichiers...", ["filterTimelapses"] = "Filtrer les timelapses...", ["downloadSelection"] = "Télécharger sélection", ["deleteSelection"] = "Supprimer sélection", ["fps"] = "FPS", ["parallel"] = "Flux", ["keepFrames"] = "Garder images", ["createOnPc"] = "Créer vidéos sur PC", ["printerExport"] = "Export imprimante", ["colFile"] = "Fichier", ["colSize"] = "Taille", ["colStatus"] = "Statut", ["colDate"] = "Date", ["colPrint"] = "Impression", ["colDuration"] = "Durée", ["colImages"] = "Images", ["ready"] = "Prêt.", ["statusConnected"] = "Connecté à {0}.", ["statusDisconnected"] = "Non connecté.", ["statusConnecting"] = "Connexion à l'imprimante… handshake max {0:0}s", ["msgEnterIp"] = "Entre une IP valide, par exemple 10.7.1.137", ["titleMissingIp"] = "IP manquante", ["msgConnectImpossible"] = "Connexion impossible.", ["detail"] = "Détail :", ["titleError"] = "Erreur", ["msgConnectFirst"] = "Connecte d'abord l'imprimante." },
        ["en"] = new() { ["windowTitle"] = "Centauri Carbon Downloader {0} — Created by ashemka", ["language"] = "Language:", ["darkMode"] = "Dark mode", ["printerIp"] = "Printer IP:", ["connect"] = "Connect", ["disconnect"] = "Disconnect", ["openFolder"] = "Open folder", ["chooseFolder"] = "Choose folder", ["exportFolder"] = "Export folder:", ["chooseFolderTitle"] = "Choose export folder", ["folderChanged"] = "Export folder set: {0}", ["cancel"] = "Cancel", ["tabGcode"] = "G-code files", ["tabTimelapse"] = "Video timelapses", ["refresh"] = "Refresh", ["scanTimelapses"] = "Scan timelapses", ["selectAll"] = "Select all", ["unselectAll"] = "Unselect all", ["filterFiles"] = "Filter files...", ["filterTimelapses"] = "Filter timelapses...", ["downloadSelection"] = "Download selection", ["deleteSelection"] = "Delete selection", ["fps"] = "FPS", ["parallel"] = "Threads", ["keepFrames"] = "Keep frames", ["createOnPc"] = "Create videos on PC", ["printerExport"] = "Printer export", ["colFile"] = "File", ["colSize"] = "Size", ["colStatus"] = "Status", ["colDate"] = "Date", ["colPrint"] = "Print", ["colDuration"] = "Duration", ["colImages"] = "Images", ["ready"] = "Ready.", ["statusConnected"] = "Connected to {0}.", ["statusDisconnected"] = "Disconnected.", ["statusConnecting"] = "Connecting to printer… handshake max {0:0}s", ["msgEnterIp"] = "Enter a valid IP, for example 10.7.1.137", ["titleMissingIp"] = "Missing IP", ["msgConnectImpossible"] = "Connection failed.", ["detail"] = "Detail:", ["titleError"] = "Error", ["msgConnectFirst"] = "Connect the printer first." },
        ["it"] = new() { ["windowTitle"] = "Centauri Carbon Downloader {0} — Creato da ashemka", ["language"] = "Lingua:", ["darkMode"] = "Modalità scura", ["printerIp"] = "IP stampante:", ["connect"] = "Connetti", ["disconnect"] = "Disconnetti", ["openFolder"] = "Apri cartella", ["chooseFolder"] = "Scegli cartella", ["exportFolder"] = "Cartella export:", ["chooseFolderTitle"] = "Scegli la cartella di export", ["folderChanged"] = "Cartella di export impostata: {0}", ["cancel"] = "Annulla", ["tabGcode"] = "File G-code", ["tabTimelapse"] = "Video timelapse", ["refresh"] = "Aggiorna", ["scanTimelapses"] = "Scansiona timelapse", ["selectAll"] = "Seleziona tutto", ["unselectAll"] = "Deseleziona tutto", ["filterFiles"] = "Filtra file...", ["filterTimelapses"] = "Filtra timelapse...", ["downloadSelection"] = "Scarica selezione", ["deleteSelection"] = "Elimina selezione", ["fps"] = "FPS", ["parallel"] = "Flussi", ["keepFrames"] = "Conserva immagini", ["createOnPc"] = "Crea video sul PC", ["printerExport"] = "Export stampante", ["colFile"] = "File", ["colSize"] = "Dimensione", ["colStatus"] = "Stato", ["colDate"] = "Data", ["colPrint"] = "Stampa", ["colDuration"] = "Durata", ["colImages"] = "Immagini", ["ready"] = "Pronto.", ["statusConnected"] = "Connesso a {0}.", ["statusDisconnected"] = "Non connesso.", ["statusConnecting"] = "Connessione alla stampante… handshake max {0:0}s", ["msgEnterIp"] = "Inserisci un IP valido, ad esempio 10.7.1.137", ["titleMissingIp"] = "IP mancante", ["msgConnectImpossible"] = "Connessione impossibile.", ["detail"] = "Dettaglio:", ["titleError"] = "Errore", ["msgConnectFirst"] = "Connetti prima la stampante." },
        ["es"] = new() { ["windowTitle"] = "Centauri Carbon Downloader {0} — Creado por ashemka", ["language"] = "Idioma:", ["darkMode"] = "Modo oscuro", ["printerIp"] = "IP impresora:", ["connect"] = "Conectar", ["disconnect"] = "Desconectar", ["openFolder"] = "Abrir carpeta", ["chooseFolder"] = "Elegir carpeta", ["exportFolder"] = "Carpeta export:", ["chooseFolderTitle"] = "Elegir carpeta de exportación", ["folderChanged"] = "Carpeta de exportación definida: {0}", ["cancel"] = "Cancelar", ["tabGcode"] = "Archivos G-code", ["tabTimelapse"] = "Vídeos timelapse", ["refresh"] = "Actualizar", ["scanTimelapses"] = "Escanear timelapses", ["selectAll"] = "Seleccionar todo", ["unselectAll"] = "Deseleccionar todo", ["filterFiles"] = "Filtrar archivos...", ["filterTimelapses"] = "Filtrar timelapses...", ["downloadSelection"] = "Descargar selección", ["deleteSelection"] = "Eliminar selección", ["fps"] = "FPS", ["parallel"] = "Flujos", ["keepFrames"] = "Guardar imágenes", ["createOnPc"] = "Crear vídeos en PC", ["printerExport"] = "Exportar impresora", ["colFile"] = "Archivo", ["colSize"] = "Tamaño", ["colStatus"] = "Estado", ["colDate"] = "Fecha", ["colPrint"] = "Impresión", ["colDuration"] = "Duración", ["colImages"] = "Imágenes", ["ready"] = "Listo.", ["statusConnected"] = "Conectado a {0}.", ["statusDisconnected"] = "No conectado.", ["statusConnecting"] = "Conectando a la impresora… handshake máx {0:0}s", ["msgEnterIp"] = "Introduce una IP válida, por ejemplo 10.7.1.137", ["titleMissingIp"] = "Falta IP", ["msgConnectImpossible"] = "Conexión imposible.", ["detail"] = "Detalle:", ["titleError"] = "Error", ["msgConnectFirst"] = "Conecta primero la impresora." },
        ["de"] = new() { ["windowTitle"] = "Centauri Carbon Downloader {0} — Erstellt von ashemka", ["language"] = "Sprache:", ["darkMode"] = "Dunkelmodus", ["printerIp"] = "Drucker-IP:", ["connect"] = "Verbinden", ["disconnect"] = "Trennen", ["openFolder"] = "Ordner öffnen", ["chooseFolder"] = "Ordner wählen", ["exportFolder"] = "Exportordner:", ["chooseFolderTitle"] = "Exportordner wählen", ["folderChanged"] = "Exportordner gesetzt: {0}", ["cancel"] = "Abbrechen", ["tabGcode"] = "G-code-Dateien", ["tabTimelapse"] = "Timelapse-Videos", ["refresh"] = "Aktualisieren", ["scanTimelapses"] = "Timelapses scannen", ["selectAll"] = "Alle auswählen", ["unselectAll"] = "Alle abwählen", ["filterFiles"] = "Dateien filtern...", ["filterTimelapses"] = "Timelapses filtern...", ["downloadSelection"] = "Auswahl laden", ["deleteSelection"] = "Auswahl löschen", ["fps"] = "FPS", ["parallel"] = "Threads", ["keepFrames"] = "Bilder behalten", ["createOnPc"] = "Videos am PC erstellen", ["printerExport"] = "Drucker-Export", ["colFile"] = "Datei", ["colSize"] = "Größe", ["colStatus"] = "Status", ["colDate"] = "Datum", ["colPrint"] = "Druck", ["colDuration"] = "Dauer", ["colImages"] = "Bilder", ["ready"] = "Bereit.", ["statusConnected"] = "Verbunden mit {0}.", ["statusDisconnected"] = "Nicht verbunden.", ["statusConnecting"] = "Verbindung zum Drucker… Handshake max {0:0}s", ["msgEnterIp"] = "Gib eine gültige IP ein, z. B. 10.7.1.137", ["titleMissingIp"] = "IP fehlt", ["msgConnectImpossible"] = "Verbindung nicht möglich.", ["detail"] = "Detail:", ["titleError"] = "Fehler", ["msgConnectFirst"] = "Verbinde zuerst den Drucker." },
        ["ja"] = new() { ["windowTitle"] = "Centauri Carbon Downloader {0} — ashemka 作成", ["language"] = "言語:", ["darkMode"] = "ダークモード", ["printerIp"] = "プリンターIP:", ["connect"] = "接続", ["disconnect"] = "切断", ["openFolder"] = "フォルダーを開く", ["chooseFolder"] = "フォルダー選択", ["exportFolder"] = "出力フォルダー:", ["chooseFolderTitle"] = "出力フォルダーを選択", ["folderChanged"] = "出力フォルダーを設定しました: {0}", ["cancel"] = "キャンセル", ["tabGcode"] = "G-code ファイル", ["tabTimelapse"] = "タイムラプス動画", ["refresh"] = "更新", ["scanTimelapses"] = "タイムラプス検索", ["selectAll"] = "すべて選択", ["unselectAll"] = "選択解除", ["filterFiles"] = "ファイルを絞り込み...", ["filterTimelapses"] = "タイムラプスを絞り込み...", ["downloadSelection"] = "選択をDL", ["deleteSelection"] = "選択を削除", ["fps"] = "FPS", ["parallel"] = "並列", ["keepFrames"] = "画像を保持", ["createOnPc"] = "PCで動画作成", ["printerExport"] = "プリンター出力", ["colFile"] = "ファイル", ["colSize"] = "サイズ", ["colStatus"] = "状態", ["colDate"] = "日付", ["colPrint"] = "印刷", ["colDuration"] = "時間", ["colImages"] = "画像", ["ready"] = "準備完了。", ["statusConnected"] = "{0} に接続しました。", ["statusDisconnected"] = "未接続。", ["statusConnecting"] = "プリンター接続中… handshake 最大 {0:0} 秒", ["msgEnterIp"] = "有効なIPを入力してください。例: 10.7.1.137", ["titleMissingIp"] = "IP未入力", ["msgConnectImpossible"] = "接続できません。", ["detail"] = "詳細:", ["titleError"] = "エラー", ["msgConnectFirst"] = "先にプリンターへ接続してください。" },
        ["zh"] = new() { ["windowTitle"] = "Centauri Carbon Downloader {0} — ashemka 创建", ["language"] = "语言:", ["darkMode"] = "深色模式", ["printerIp"] = "打印机 IP:", ["connect"] = "连接", ["disconnect"] = "断开", ["openFolder"] = "打开文件夹", ["chooseFolder"] = "选择文件夹", ["exportFolder"] = "导出文件夹:", ["chooseFolderTitle"] = "选择导出文件夹", ["folderChanged"] = "导出文件夹已设置: {0}", ["cancel"] = "取消", ["tabGcode"] = "G-code 文件", ["tabTimelapse"] = "延时视频", ["refresh"] = "刷新", ["scanTimelapses"] = "扫描延时", ["selectAll"] = "全选", ["unselectAll"] = "取消全选", ["filterFiles"] = "筛选文件...", ["filterTimelapses"] = "筛选延时...", ["downloadSelection"] = "下载所选", ["deleteSelection"] = "删除所选", ["fps"] = "FPS", ["parallel"] = "并发", ["keepFrames"] = "保留图片", ["createOnPc"] = "在电脑生成视频", ["printerExport"] = "打印机导出", ["colFile"] = "文件", ["colSize"] = "大小", ["colStatus"] = "状态", ["colDate"] = "日期", ["colPrint"] = "打印", ["colDuration"] = "时长", ["colImages"] = "图片", ["ready"] = "就绪。", ["statusConnected"] = "已连接到 {0}。", ["statusDisconnected"] = "未连接。", ["statusConnecting"] = "正在连接打印机… handshake 最多 {0:0} 秒", ["msgEnterIp"] = "请输入有效 IP，例如 10.7.1.137", ["titleMissingIp"] = "缺少 IP", ["msgConnectImpossible"] = "无法连接。", ["detail"] = "详情:", ["titleError"] = "错误", ["msgConnectFirst"] = "请先连接打印机。" },
        ["ko"] = new() { ["windowTitle"] = "Centauri Carbon Downloader {0} — ashemka 제작", ["language"] = "언어:", ["darkMode"] = "다크 모드", ["printerIp"] = "프린터 IP:", ["connect"] = "연결", ["disconnect"] = "연결 해제", ["openFolder"] = "폴더 열기", ["chooseFolder"] = "폴더 선택", ["exportFolder"] = "내보내기 폴더:", ["chooseFolderTitle"] = "내보내기 폴더 선택", ["folderChanged"] = "내보내기 폴더 설정됨: {0}", ["cancel"] = "취소", ["tabGcode"] = "G-code 파일", ["tabTimelapse"] = "타임랩스 영상", ["refresh"] = "새로고침", ["scanTimelapses"] = "타임랩스 검색", ["selectAll"] = "모두 선택", ["unselectAll"] = "모두 해제", ["filterFiles"] = "파일 필터...", ["filterTimelapses"] = "타임랩스 필터...", ["downloadSelection"] = "선택 다운로드", ["deleteSelection"] = "선택 삭제", ["fps"] = "FPS", ["parallel"] = "동시", ["keepFrames"] = "이미지 보관", ["createOnPc"] = "PC에서 영상 생성", ["printerExport"] = "프린터 내보내기", ["colFile"] = "파일", ["colSize"] = "크기", ["colStatus"] = "상태", ["colDate"] = "날짜", ["colPrint"] = "출력", ["colDuration"] = "시간", ["colImages"] = "이미지", ["ready"] = "준비됨.", ["statusConnected"] = "{0}에 연결됨.", ["statusDisconnected"] = "연결 안 됨.", ["statusConnecting"] = "프린터 연결 중… handshake 최대 {0:0}초", ["msgEnterIp"] = "유효한 IP를 입력하세요. 예: 10.7.1.137", ["titleMissingIp"] = "IP 없음", ["msgConnectImpossible"] = "연결할 수 없습니다.", ["detail"] = "세부정보:", ["titleError"] = "오류", ["msgConnectFirst"] = "먼저 프린터에 연결하세요." }
    };

    private string Tr(string key)
    {
        if (UiText.TryGetValue(_currentLang, out var lang) && lang.TryGetValue(key, out var value)) return value;
        if (UiText.TryGetValue("en", out var en) && en.TryGetValue(key, out var fallback)) return fallback;
        return key;
    }

    private static string LangCodeFromIndex(int index) => index switch
    {
        1 => "en",
        2 => "it",
        3 => "es",
        4 => "de",
        5 => "ja",
        6 => "zh",
        7 => "ko",
        _ => "fr"
    };

    private static int LangIndexFromCode(string code) => code.ToLowerInvariant() switch
    {
        "en" => 1,
        "it" => 2,
        "es" => 3,
        "de" => 4,
        "ja" => 5,
        "zh" or "zh-cn" or "zh-tw" => 6,
        "ko" => 7,
        _ => 0
    };

    private void LoadUserSettings()
    {
        try
        {
            if (!File.Exists(_settingsFile)) return;
            var settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(_settingsFile));
            if (settings is null) return;
            var loadedLanguage = string.IsNullOrWhiteSpace(settings.Language) ? "fr" : settings.Language;
            _languageBox.SelectedIndex = LangIndexFromCode(loadedLanguage);
            _currentLang = LangCodeFromIndex(_languageBox.SelectedIndex);
            _darkMode = settings.DarkMode;
            _darkModeCheckBox.Checked = _darkMode;
            if (!string.IsNullOrWhiteSpace(settings.ExportFolder))
                SetDownloadBaseFolder(settings.ExportFolder);
        }
        catch
        {
            _currentLang = "fr";
            _darkMode = false;
        }
    }

    private void SaveUserSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile)!);
            File.WriteAllText(_settingsFile, JsonSerializer.Serialize(new UserSettings { Language = _currentLang, DarkMode = _darkMode, ExportFolder = _baseDownloadFolder }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Settings persistence must not block printer operations.
        }
    }

    private void ApplyLanguage()
    {
        Text = string.Format(Tr("windowTitle"), AppVersion);
        _titleLabel.Text = Text;
        _languageLabel.Text = Tr("language");
        _darkModeCheckBox.Text = Tr("darkMode");
        _printerIpLabel.Text = Tr("printerIp");
        _chooseFolderButton.Text = Tr("chooseFolder");
        _openFolderButton.Text = Tr("openFolder");
        _cancelButton.Text = Tr("cancel");
        UpdateDownloadFolderLabel();
        _tabs.TabPages[0].Text = Tr("tabGcode");
        _tabs.TabPages[1].Text = Tr("tabTimelapse");
        _refreshLocalButton.Text = Tr("refresh");
        _selectAllLocalButton.Text = Tr("selectAll");
        _filterLocalBox.PlaceholderText = Tr("filterFiles");
        _downloadLocalButton.Text = Tr("downloadSelection");
        _deleteLocalButton.Text = Tr("deleteSelection");
        _refreshTimelapseButton.Text = Tr("scanTimelapses");
        _selectAllTimelapseButton.Text = Tr("selectAll");
        _filterTimelapseBox.PlaceholderText = Tr("filterTimelapses");
        _fpsLabel.Text = Tr("fps");
        _parallelLabel.Text = Tr("parallel");
        _keepFramesCheckBox.Text = Tr("keepFrames");
        _downloadTimelapseButton.Text = Tr("createOnPc");
        _exportPrinterTimelapseButton.Text = Tr("printerExport");
        _footerLabel.Text = $"{CreatorCredit} — {AppVersion}";

        if (_localGrid.Columns.Count >= 4)
        {
            _localGrid.Columns[1].HeaderText = Tr("colFile");
            _localGrid.Columns[2].HeaderText = Tr("colSize");
            _localGrid.Columns[3].HeaderText = Tr("colStatus");
        }
        if (_timelapseGrid.Columns.Count >= 6)
        {
            _timelapseGrid.Columns[1].HeaderText = Tr("colDate");
            _timelapseGrid.Columns[2].HeaderText = Tr("colPrint");
            _timelapseGrid.Columns[3].HeaderText = Tr("colDuration");
            _timelapseGrid.Columns[4].HeaderText = Tr("colImages");
            _timelapseGrid.Columns[5].HeaderText = Tr("colStatus");
        }

        if (_webSocket is { State: WebSocketState.Open }) _connectButton.Text = Tr("disconnect");
        else _connectButton.Text = Tr("connect");
        if (string.IsNullOrWhiteSpace(_statusLabel.Text) || _statusLabel.Text == "Prêt." || _statusLabel.Text == "Ready.") SetStatus(Tr("ready"));
    }

    private void ApplyTheme()
    {
        var palette = GetThemePalette();

        BackColor = palette.Back;
        ForeColor = palette.Text;
        ApplyThemeRecursive(this, palette);

        foreach (TabPage page in _tabs.TabPages)
        {
            page.UseVisualStyleBackColor = false;
            page.BackColor = palette.Surface;
            page.ForeColor = palette.Text;
            page.BorderStyle = BorderStyle.None;
        }

        if (_tabs is ThemedTabControl themedTabs)
        {
            themedTabs.DarkMode = _darkMode;
            themedTabs.ThemeBackColor = palette.Back;
            themedTabs.ThemeSurfaceColor = palette.Surface;
            themedTabs.ThemeTabBackColor = palette.Panel;
            themedTabs.ThemeSelectedTabColor = palette.Surface;
            themedTabs.ThemeTextColor = palette.Text;
            themedTabs.ThemeMutedTextColor = palette.Muted;
            themedTabs.ThemeBorderColor = palette.Border;
        }

        foreach (var grid in new[] { _localGrid, _timelapseGrid })
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = palette.Surface;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.GridColor = palette.Border;
            grid.DefaultCellStyle.BackColor = palette.Surface;
            grid.DefaultCellStyle.ForeColor = palette.Text;
            grid.DefaultCellStyle.SelectionBackColor = palette.Accent;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = Font;
            grid.AlternatingRowsDefaultCellStyle.BackColor = palette.GridAlt;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = palette.Text;
            grid.ColumnHeadersDefaultCellStyle.BackColor = palette.Header;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = palette.Text;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = palette.Header;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = palette.Text;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Regular);
            grid.RowHeadersDefaultCellStyle.BackColor = palette.Header;
            grid.RowHeadersDefaultCellStyle.ForeColor = palette.Text;
            grid.RowHeadersDefaultCellStyle.SelectionBackColor = palette.Header;
            grid.RowHeadersDefaultCellStyle.SelectionForeColor = palette.Text;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.DefaultCellStyle.BackColor = palette.Surface;
                column.DefaultCellStyle.ForeColor = palette.Text;
                column.DefaultCellStyle.SelectionBackColor = palette.Accent;
                column.DefaultCellStyle.SelectionForeColor = Color.White;
            }
        }

        if (_progress is ThemedProgressBar themedProgress)
        {
            themedProgress.ThemeBackColor = palette.Panel;
            themedProgress.ThemeBorderColor = palette.Border;
            themedProgress.ThemeBarColor = palette.Accent;
        }
        _progress.BackColor = palette.Panel;
        _progress.ForeColor = palette.Accent;

        _titleLabel.ForeColor = _darkMode ? Color.FromArgb(191, 219, 254) : Color.FromArgb(30, 64, 175);
        _footerLabel.ForeColor = palette.Muted;
        _downloadFolderLabel.ForeColor = palette.Muted;
        RefreshButtonEnabledColors();
        _tabs.Invalidate();
        _progress.Invalidate();
    }

    private void ApplyThemeRecursive(Control control, ThemePalette palette)
    {
        switch (control)
        {
            case TextBox tb:
                ApplyTextBoxTheme(tb, palette);
                break;
            case ComboBox cb:
                cb.BackColor = palette.Input;
                cb.ForeColor = palette.Text;
                cb.FlatStyle = FlatStyle.Flat;
                break;
            case Button btn:
                ApplyButtonTheme(btn, palette);
                break;
            case CheckBox chk:
                chk.BackColor = palette.Back;
                chk.ForeColor = chk.Enabled ? palette.Text : palette.Muted;
                break;
            case DataGridView:
                // Detailed grid styling is applied in ApplyTheme().
                break;
            case TabControl tab:
                tab.BackColor = palette.Back;
                tab.ForeColor = palette.Text;
                break;
            case TabPage page:
                page.UseVisualStyleBackColor = false;
                page.BackColor = palette.Surface;
                page.ForeColor = palette.Text;
                break;
            case Label label:
                label.BackColor = Color.Transparent;
                label.ForeColor = label == _footerLabel || label == _downloadFolderLabel
                    ? palette.Muted
                    : palette.Text;
                break;
            case ProgressBar:
                control.BackColor = palette.Panel;
                control.ForeColor = palette.Accent;
                break;
            case TableLayoutPanel:
            case Panel:
                control.BackColor = palette.Back;
                control.ForeColor = palette.Text;
                break;
            default:
                control.BackColor = palette.Panel;
                control.ForeColor = palette.Text;
                break;
        }

        foreach (Control child in control.Controls)
            ApplyThemeRecursive(child, palette);
    }

    private static void OpenDownloadFolder(string folder)
    {
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }

    private static string GetDownloadsFolder()
    {
        try
        {
            var knownFolder = new Guid("374DE290-123F-4565-9164-39C4925E467B");
            var result = SHGetKnownFolderPath(knownFolder, 0, IntPtr.Zero, out var pathPtr);
            if (result == 0 && pathPtr != IntPtr.Zero)
            {
                var path = Marshal.PtrToStringUni(pathPtr);
                Marshal.FreeCoTaskMem(pathPtr);
                if (!string.IsNullOrWhiteSpace(path)) return path;
            }
        }
        catch
        {
            // Fallback below.
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
        uint dwFlags,
        IntPtr hToken,
        out IntPtr ppszPath);
}
