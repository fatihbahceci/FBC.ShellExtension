using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>What the user did with a film to choose.</summary>
internal enum MovieChoice
{
    Selected,

    /// <summary>Skipped, or cancelled when not asked in a row.</summary>
    Skipped,

    /// <summary>The rest of the row is not asked about.</summary>
    Stopped,
}

/// <summary>
/// Choosing the film of a file (ADR-0046): the films OMDb offered, another search with the
/// user's own title and year, or the film's IMDb address pasted in. After a search the films
/// are asked about in a row, with Skip and Stop asking; later one at a time, with Cancel.
/// </summary>
internal partial class MovieChoiceForm : Form
{
    private readonly string _apiKey;
    private List<MovieInfo> _films = [];
    private CancellationTokenSource? _searching;

    public MovieChoiceForm(string fileName, MovieGuess guess, IReadOnlyList<MovieInfo> candidates, string apiKey, bool inRow)
    {
        _apiKey = apiKey;
        InitializeComponent();
        Text = Strings.ImdbChooseTitle();
        forLabel.Text = Strings.ImdbChooseFor(fileName);
        queryLabel.Text = Strings.ImdbQueryLabel();
        yearLabel.Text = Strings.ImdbYearLabel();
        searchButton.Text = Strings.ImdbSearch();
        titleColumn.Text = Strings.ImdbTitleColumn();
        yearColumn.Text = Strings.ImdbYearColumn();
        idColumn.Text = Strings.ImdbIdColumn();
        selectButton.Text = Strings.ImdbSelect();
        skipButton.Text = inRow ? Strings.ImdbSkip() : Strings.EditorCancel();
        stopButton.Text = Strings.ImdbStopAsking();
        stopButton.Visible = inRow;
        queryBox.Text = guess.Title;
        yearBox.Text = guess.Year ?? "";
        Fill(candidates);
    }

    public MovieInfo? Movie { get; private set; }

    public MovieChoice Choice { get; private set; } = MovieChoice.Skipped;

    private void Fill(IReadOnlyList<MovieInfo> films)
    {
        _films = [.. films];
        filmList.BeginUpdate();
        filmList.Items.Clear();
        filmList.Items.AddRange([.. _films.Select(film => new ListViewItem([film.Title, film.Year, film.ImdbId]))]);
        filmList.EndUpdate();
        if (filmList.Items.Count > 0)
        {
            filmList.Items[0].Selected = filmList.Items[0].Focused = true;
        }

        statusLabel.Text = _films.Count == 0 ? Strings.ImdbChooseNone() : "";
        selectButton.Enabled = _films.Count > 0;
    }

    /// <summary>An IMDb address or id is that film; anything else is looked up as a title, like the search of the main window.</summary>
    private async void OnSearch(object? sender, EventArgs e)
    {
        string query = queryBox.Text.Trim();
        if (query.Length == 0 || _searching is not null)
        {
            return;
        }

        string? year = yearBox.Text.Trim() is { Length: > 0 } text ? text : null;
        using var searching = new CancellationTokenSource();
        _searching = searching;
        searchButton.Enabled = selectButton.Enabled = false;
        UseWaitCursor = true;
        statusLabel.Text = Strings.ImdbAsking();
        IReadOnlyList<MovieInfo>? films = null;
        string? problem = null;
        try
        {
            if (Omdb.ImdbIdIn(query) is { } id)
            {
                films = (await OmdbClient.FetchAsync(Omdb.IdUri(_apiKey, id), searching.Token)).Films;
            }
            else
            {
                MovieLookup lookup = await OmdbClient.LookupAsync(new MovieGuess(query, year, null), _apiKey, searching.Token);
                films = lookup.Match is { } match ? [match] : lookup.Candidates;
            }
        }
        catch (OperationCanceledException) when (searching.IsCancellationRequested)
        {
            // Closed meanwhile.
            return;
        }
        catch (OmdbException ex)
        {
            problem = ex.Message;
        }
        finally
        {
            _searching = null;
        }

        if (IsDisposed)
        {
            return;
        }

        UseWaitCursor = false;
        searchButton.Enabled = true;
        if (films is not null)
        {
            Fill(films);
            filmList.Focus();
        }
        else
        {
            selectButton.Enabled = _films.Count > 0;
            statusLabel.Text = problem;
        }
    }

    // Enter in a search box searches; elsewhere it selects. The form takes Enter for its
    // default button before a text box's KeyDown would see it.
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData == Keys.Enter && (queryBox.Focused || yearBox.Focused))
        {
            OnSearch(this, EventArgs.Empty);
            return true;
        }

        return base.ProcessDialogKey(keyData);
    }

    private void OnSelect(object? sender, EventArgs e)
    {
        if (filmList.SelectedIndices.Count == 0 || _searching is not null)
        {
            return;
        }

        Movie = _films[filmList.SelectedIndices[0]];
        Choice = MovieChoice.Selected;
        DialogResult = DialogResult.OK;
    }

    private void OnSkip(object? sender, EventArgs e)
    {
        Choice = MovieChoice.Skipped;
        DialogResult = DialogResult.Cancel;
    }

    private void OnStop(object? sender, EventArgs e)
    {
        Choice = MovieChoice.Stopped;
        DialogResult = DialogResult.Cancel;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _searching?.Cancel();
        base.OnFormClosing(e);
    }
}
