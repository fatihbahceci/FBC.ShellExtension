using System.Diagnostics;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using FBC.ShellExtension.WikiImport;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Rename from IMDb (ADR-0046): looks the films up on OMDb and renames each video and the
/// subtitles named after it, with every new name shown first. A film is taken only when OMDb is
/// certain; otherwise it is chosen by the user, after the search, one after another. All or
/// nothing, like Advanced rename: while one name has a problem, none is renamed; Windows' own
/// engine renames, so Explorer's Undo takes it back.
/// </summary>
internal partial class ImdbRenameForm : Form
{
    // OMDb answers a few at a time quickly; more would only reach the daily limit sooner.
    private const int ParallelLookups = 4;

    private readonly IReadOnlyList<SelectedItem> _selection;
    private readonly string _root;
    private readonly DpapiProtector _protector = new();

    // The names in each folder, read once for the preview: a keystroke asks about every file.
    private readonly Dictionary<string, HashSet<string>> _names = new(StringComparer.OrdinalIgnoreCase);
    private List<FilmState> _films = [];
    private List<string> _unreadable = [];
    private RenamePreview _preview = new([], null);
    private CancellationTokenSource? _reading;
    private CancellationTokenSource? _searching;
    private (int Done, int Total)? _progress;
    private bool _ready;

    public ImdbRenameForm(IReadOnlyList<SelectedItem> items)
    {
        _selection = items;
        _root = items[0].Kind == ItemKind.Background ? items[0].Path : items[0].Directory;
        InitializeComponent();
        ApplyStrings();
        subfoldersCheck.Visible = items.Any(item => item.Kind != ItemKind.File);
        string? format = null;
        try
        {
            format = OmdbSettings.Load(OmdbSettings.DefaultPath).Format;
        }
        catch (IOException)
        {
            // The default then; the key window says what is wrong with the file.
        }

        formatBox.Text = string.IsNullOrWhiteSpace(format) ? MovieNames.DefaultFormat : format;
        _ready = true;
    }

    /// <summary>A video and its subtitles, and what OMDb and the user made of them.</summary>
    private sealed class FilmState(MovieGroup group)
    {
        public MovieGroup Group { get; } = group;

        /// <summary>The film the files are renamed after.</summary>
        public MovieInfo? Movie { get; set; }

        /// <summary>What OMDb offered when it was not certain; null before the lookup.</summary>
        public IReadOnlyList<MovieInfo>? Candidates { get; set; }

        /// <summary>The user left the names as they are.</summary>
        public bool Left { get; set; }

        public string Key => $"{Group.Files[0].Item.Directory}\n{Group.BaseName}";

        /// <summary>Two films whose names say the same (cd1, cd2) are looked up and chosen once.</summary>
        public string GuessKey => $"{Group.Guess.Title}\n{Group.Guess.Year}".ToLowerInvariant();
    }

    private void ApplyStrings()
    {
        selectionLabel.Text = ToolWindows.Selection(_selection);
        formatLabel.Text = Strings.ImdbFormatLabel();
        tokensLabel.Text = Strings.ImdbTokens();
        subfoldersCheck.Text = Strings.ImdbSubfolders();
        searchButton.Text = Strings.ImdbSearch();
        keyLink.Text = Strings.ImdbKeyLink();
        fileColumn.Text = Strings.ImdbFileColumn();
        filmColumn.Text = Strings.ImdbFilmColumn();
        newColumn.Text = Strings.RenameNewColumn();
        stateColumn.Text = Strings.ImdbStateColumn();
        chooseItem.Text = Strings.ImdbMenuChoose();
        leaveItem.Text = Strings.ImdbMenuLeave();
        openItem.Text = Strings.ImdbMenuOpen();
        renameButton.Text = Strings.RenameButton();
        closeButton.Text = Strings.ToolWindowClose();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await ReadFilesAsync();
    }

    private async void OnSubfoldersChanged(object? sender, EventArgs e)
    {
        if (_ready)
        {
            await ReadFilesAsync();
        }
    }

    /// <summary>The files of the selection, read off the UI thread; what was found for a film so far is kept.</summary>
    private async Task ReadFilesAsync()
    {
        _reading?.Cancel();
        using var reading = new CancellationTokenSource();
        _reading = reading;
        bool subfolders = subfoldersCheck.Checked;
        searchButton.Enabled = renameButton.Enabled = false;
        statusLabel.Text = Strings.ImdbReading();
        statusLabel.ForeColor = SystemColors.ControlText;
        IReadOnlyList<SelectedItem> files;
        List<string> unreadable = [];
        try
        {
            files = await Task.Run(() => FileTools.MovieSources(_selection, subfolders, out unreadable, reading.Token), reading.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            if (_reading == reading)
            {
                _reading = null;
            }
        }

        // A read the box replaced may still finish (the walk checks between folders only); the
        // newer one's files are the ones the box asks for.
        if (IsDisposed || reading.IsCancellationRequested)
        {
            return;
        }

        // Not ToDictionary: a key twice would throw out of the window's event.
        var before = new Dictionary<string, FilmState>(StringComparer.OrdinalIgnoreCase);
        foreach (FilmState film in _films)
        {
            before.TryAdd(film.Key, film);
        }

        _films = [.. MovieNames.Group(files).Select(group =>
        {
            var film = new FilmState(group);
            if (before.TryGetValue(film.Key, out FilmState? old))
            {
                film.Movie = old.Movie;
                film.Candidates = old.Candidates;
                film.Left = old.Left;
            }

            return film;
        })];
        _unreadable = unreadable;
        searchButton.Enabled = _films.Count > 0;
        RefreshPreview();
    }

    // The list is built again once the typing stops for a moment.
    private void OnFormatChanged(object? sender, EventArgs e)
    {
        if (_ready)
        {
            refreshTimer.Stop();
            refreshTimer.Start();
            renameButton.Enabled = false;
        }
    }

    private void OnRefreshTimer(object? sender, EventArgs e)
    {
        refreshTimer.Stop();
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        string format = formatBox.Text;
        string? formatProblem = MovieNames.FormatProblem(format);
        var names = new List<(SelectedItem Item, string NewName)>();
        var owners = new List<FilmState>();
        foreach (FilmState film in _films)
        {
            foreach (MovieFile file in film.Group.Files)
            {
                names.Add((file.Item, film.Movie is { } movie && !film.Left && formatProblem is null
                    ? MovieNames.NewName(format, movie, film.Group, file)
                    : file.Item.Name));
                owners.Add(film);
            }
        }

        _preview = FileTools.PreviewNames(names, Exists);

        // Built anew on every change; the selection and the scrolling stay where they were.
        HashSet<int> selected = [.. previewList.SelectedIndices.Cast<int>()];
        int top = previewList.TopItem?.Index ?? 0;
        previewList.BeginUpdate();
        previewList.Items.Clear();
        for (int index = 0; index < _preview.Rows.Count; index++)
        {
            RenameRow row = _preview.Rows[index];
            FilmState film = owners[index];
            string state = row.Problem ?? (film.Left ? Strings.ImdbStateLeft()
                : film.Movie is not null ? ""
                : film.Candidates is null ? Strings.ImdbStateWaiting()
                : film.Candidates.Count == 0 ? Strings.ImdbStateNotFound()
                : Strings.ImdbStateChoose());
            string shown = film.Movie is { } movie ? Strings.ImdbFilm(movie.Title, movie.Year, movie.ImdbId) : "";
            previewList.Items.Add(new ListViewItem([ShownPath(row.Item.Path), shown, row.Changes ? row.NewName : "", state])
            {
                Tag = film,
                ForeColor = row.Problem is not null ? Color.Firebrick : row.Changes ? SystemColors.WindowText : SystemColors.GrayText,
                Selected = selected.Contains(index),
            });
        }

        previewList.EndUpdate();
        if (top > 0 && top < previewList.Items.Count)
        {
            previewList.TopItem = previewList.Items[top];
        }

        ShowStatus(formatProblem);
    }

    private void ShowStatus(string? formatProblem = null)
    {
        int problems = _preview.Rows.Count(row => row.Problem is not null);
        string status = formatProblem
            ?? (_films.Count == 0 ? Strings.ImdbNoFiles()
            : _progress is { } progress ? Strings.ImdbSearching(progress.Done, progress.Total)
            : problems > 0 ? Strings.RenameProblems(problems)
            : _films.Any(film => film.Candidates is not null || film.Movie is not null)
                ? Strings.ImdbSummary(_films.Count(film => film.Movie is not null), _films.Count, _preview.Rows.Count(row => row.Changes))
            : Strings.ImdbFiles(_preview.Rows.Count, _films.Count));
        statusLabel.Text = _unreadable.Count > 0 ? $"{status} {Strings.OrganizeUnreadable(_unreadable.Count)}" : status;
        statusLabel.ForeColor = formatProblem is not null || problems > 0 || _films.Count == 0 ? Color.Firebrick : SystemColors.ControlText;
        renameButton.Enabled = _searching is null && _reading is null && formatProblem is null && _preview.CanRename;
    }

    /// <summary>The file as found under the folder the user looked at: "Extras\film.mkv".</summary>
    private string ShownPath(string path)
    {
        string folder = _root.EndsWith('\\') ? _root : _root + "\\";
        return path.StartsWith(folder, StringComparison.OrdinalIgnoreCase) ? path[folder.Length..] : path;
    }

    private bool Exists(string path)
    {
        string folder = Path.GetDirectoryName(path) ?? "";
        if (!_names.TryGetValue(folder, out HashSet<string>? names))
        {
            try
            {
                names = new HashSet<string>(Directory.EnumerateFileSystemEntries(folder, "*", new EnumerationOptions { AttributesToSkip = 0 })
                    .Select(entry => Path.GetFileName(entry)), StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Unreadable: nothing known to be in the way; Windows asks if something is.
                names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            _names[folder] = names;
        }

        return names.Contains(Path.GetFileName(path));
    }

    /// <summary>The key kept for OMDb; when there is none, the key window is offered first.</summary>
    private string? ApiKey()
    {
        if (StoredKey() is { } key)
        {
            return key;
        }

        if (MessageBox.Show(this, Strings.ImdbNoKey(), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
        {
            return null;
        }

        using (var form = new OmdbKeyForm())
        {
            form.ShowDialog(this);
        }

        return StoredKey();
    }

    private string? StoredKey()
    {
        try
        {
            return OmdbSettings.Load(OmdbSettings.DefaultPath).Key(_protector);
        }
        catch (IOException)
        {
            return null;
        }
    }

    private void OnKeyLink(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        using var form = new OmdbKeyForm();
        form.ShowDialog(this);
    }

    /// <summary>
    /// Looks up the films not looked up yet, a few at a time, then asks about those OMDb was not
    /// certain of. A second click stops. The first refusal from OMDb (the key, the limit, no
    /// answer) stops the search: the next lookups would fare no better.
    /// </summary>
    private async void OnSearch(object? sender, EventArgs e)
    {
        if (_searching is not null)
        {
            _searching.Cancel();
            return;
        }

        if (ApiKey() is not { } key || IsDisposed)
        {
            return;
        }

        List<IGrouping<string, FilmState>> pending = [.. _films.Where(film => film.Movie is null && !film.Left && film.Candidates is null)
            .GroupBy(film => film.GuessKey)];
        using var searching = new CancellationTokenSource();
        _searching = searching;
        searchButton.Text = Strings.ImdbStop();
        subfoldersCheck.Enabled = false;
        _progress = (0, pending.Count);
        ShowStatus();
        OmdbException? failure = null;
        try
        {
            using var gate = new SemaphoreSlim(ParallelLookups);
            await Task.WhenAll(pending.Select(async same =>
            {
                try
                {
                    await gate.WaitAsync(searching.Token);
                    MovieLookup lookup;
                    try
                    {
                        lookup = await OmdbClient.LookupAsync(same.First().Group.Guess, key, searching.Token);
                    }
                    finally
                    {
                        gate.Release();
                    }

                    // Back on the window's thread: the states and the list are its own.
                    foreach (FilmState film in same)
                    {
                        film.Movie = lookup.Match;
                        film.Candidates = lookup.Candidates;
                    }

                    _progress = (_progress.Value.Done + 1, _progress.Value.Total);
                    if (!IsDisposed)
                    {
                        RefreshPreview();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Stopped, or another lookup failed.
                }
                catch (OmdbException ex)
                {
                    failure ??= ex;
                    searching.Cancel();
                }
            }));
        }
        finally
        {
            // However the search ends, Stop and Close do not reach a disposed source, and the window is usable again.
            _searching = null;
            _progress = null;
            if (!IsDisposed)
            {
                searchButton.Text = Strings.ImdbSearch();
                subfoldersCheck.Enabled = true;
            }
        }

        bool stopped = searching.IsCancellationRequested;
        if (IsDisposed)
        {
            return;
        }

        RefreshPreview();
        if (failure is not null)
        {
            MessageBox.Show(this, $"{Strings.ImdbSearchStopped()}\n\n{failure.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else if (!stopped)
        {
            // Nothing new to look up: Search asks again about the films still to choose.
            AskInRow(key, pending.Count > 0 ? [.. pending.SelectMany(same => same)] : _films);
        }
    }

    /// <summary>The films OMDb offered several of, asked about one after another; a choice goes to the films whose names say the same.</summary>
    private void AskInRow(string key, IReadOnlyList<FilmState> looked)
    {
        foreach (FilmState film in looked)
        {
            if (film.Movie is not null || film.Left || film.Candidates is not { Count: > 0 })
            {
                continue;
            }

            MovieChoice choice = Choose(film, key, inRow: true);
            RefreshPreview();
            if (choice == MovieChoice.Stopped || IsDisposed)
            {
                return;
            }
        }
    }

    private MovieChoice Choose(FilmState film, string key, bool inRow)
    {
        // A film already found is offered first, so that choosing again starts from it.
        IReadOnlyList<MovieInfo> offered = film.Movie is { } found
            ? [found, .. (film.Candidates ?? []).Where(other => !string.Equals(other.ImdbId, found.ImdbId, StringComparison.OrdinalIgnoreCase))]
            : film.Candidates ?? [];
        using var form = new MovieChoiceForm(ShownPath(film.Group.Files[0].Item.Path), film.Group.Guess, offered, key, inRow);
        form.ShowDialog(this);
        if (form.Choice == MovieChoice.Selected && form.Movie is { } movie)
        {
            foreach (FilmState same in _films.Where(other => other == film || (other.GuessKey == film.GuessKey && other.Movie is null && !other.Left)))
            {
                same.Movie = movie;
                same.Left = false;
            }
        }

        return form.Choice;
    }

    private FilmState? SelectedFilm() => previewList.SelectedItems.Count > 0 ? previewList.SelectedItems[0].Tag as FilmState : null;

    private void OnListDoubleClick(object? sender, EventArgs e) => OnChoose(sender, e);

    private void OnChoose(object? sender, EventArgs e)
    {
        // Not while the folders are read again: the list's films would be replaced under the choice.
        if (_searching is not null || _reading is not null || SelectedFilm() is not { } film || ApiKey() is not { } key || IsDisposed)
        {
            return;
        }

        Choose(film, key, inRow: false);
        RefreshPreview();
    }

    /// <summary>The selected films keep their names; chosen again, they take the film's.</summary>
    private void OnLeave(object? sender, EventArgs e)
    {
        List<FilmState> films = [.. previewList.SelectedItems.Cast<ListViewItem>().Select(item => item.Tag).OfType<FilmState>().Distinct()];
        bool leave = films.Any(film => !film.Left);
        foreach (FilmState film in films)
        {
            film.Left = leave;
        }

        RefreshPreview();
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        if (SelectedFilm()?.Movie is { } movie && Omdb.ImdbIdIn(movie.ImdbId) is { } id)
        {
            Process.Start(new ProcessStartInfo($"https://www.imdb.com/title/{id}/") { UseShellExecute = true })?.Dispose();
        }
    }

    private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        FilmState? film = SelectedFilm();
        if (film is null)
        {
            e.Cancel = true;
            return;
        }

        chooseItem.Enabled = _searching is null && _reading is null;
        leaveItem.Checked = previewList.SelectedItems.Cast<ListViewItem>().Select(item => item.Tag).OfType<FilmState>().All(state => state.Left);
        openItem.Enabled = film.Movie is not null;
    }

    private void OnRename(object? sender, EventArgs e)
    {
        // Built again from the disk: something may have changed since the folders were read.
        refreshTimer.Stop();
        _names.Clear();
        RefreshPreview();
        if (!renameButton.Enabled)
        {
            return;
        }

        List<string> errors = ShellFileOperation.Rename(this, [.. FileTools.RenameSequence(_preview).Select(row => (row.Item.Path, row.NewName))]);
        try
        {
            string format = formatBox.Text;
            OmdbSettings.Update(OmdbSettings.DefaultPath, settings => settings.Format = format);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Only the template is not remembered; the names are what was asked for.
        }

        ToolWindows.ShowErrors(this, Text, Strings.RenameNotRenamed(), errors);
        Close();
    }

    // Not a dialog: the window is the program, and Close ends it.
    private void OnClose(object? sender, EventArgs e) => Close();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _reading?.Cancel();
        _searching?.Cancel();
        base.OnFormClosing(e);
    }
}
