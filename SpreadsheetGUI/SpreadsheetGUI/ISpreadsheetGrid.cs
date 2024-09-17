namespace SS;

/// <summary>
/// The type of delegate used to register for SelectionChanged events
/// </summary>
/// <param name="sender"></param>
public delegate void SelectionChangedHandler(ISpreadsheetGrid sender);

/// <summary>
/// The type of delegate used to register for ContentsUpdated events
/// </summary>
public delegate void ContentsUpdatedHandler(ISpreadsheetGrid sender);

/// <summary>
/// An interface for a 2D grid that knows how to store values, display itself,
/// and maintain selection.
/// When the selection changes, a SelectionChange event is fired.
/// Clients can register to be notified of such events.
/// </summary>
public interface ISpreadsheetGrid
{
    /// <summary>
    /// The event used to send notifications of a selection change
    /// </summary>
    public event SelectionChangedHandler SelectionChanged;

    /// <summary>
    /// Clears the display.
    /// </summary>
    public void Clear();

    /// <summary>
    /// Checks if the spreadsheet has been changed since the last save.
    /// </summary>
    /// <returns>True if the spreadsheet has been changed, false otherwise.</returns>
    public bool IsChanged();

    /// <summary>
    /// If the zero-based column and row are in range, sets the value of that
    /// cell and returns true.  Otherwise, returns false.
    /// </summary>
    public bool SetValue(int col, int row, string c);

    /// <summary>
    /// If the zero-based column and row are in range, assigns the value
    /// of that cell to the out parameter and returns true.  Otherwise,
    /// returns false.
    /// </summary>
    public bool GetValue(int col, int row, out string c);

    /// <summary>
    /// If the zero-based column and row are in range, uses them to set
    /// the current selection and returns true.  Otherwise, returns false.
    /// </summary>
    public bool SetSelection(int col, int row);

    /// <summary>
    /// Assigns the column and row of the current selection to the
    /// out parameters.
    /// </summary>
    public void GetSelection(out int col, out int row);

    /// <summary>
    /// Gets the name of the cell at the provided spreadsheet grid coordinates.
    /// </summary>
    /// <param name="col">Column value.</param>
    /// <param name="row">Row value.</param>
    /// <returns>The string form of the cell's name.</returns>
    public string GetCellName(int col, int row);

    /// <summary>
    /// Gets the contents of the cell at the provided spreadsheet grid coordinates.
    /// </summary>
    /// <param name="col">Column value.</param>
    /// <param name="row">Row value.</param>
    /// <returns>The stringform of the cell's contents.</returns>
    public string GetCellContents(int col, int row);

    /// <summary>
    /// Loads the spreadsheet at the provided filepath.
    /// </summary>
    /// <param name="path">Filepath to be loaded from.</param>
    public bool Load(string path);

    /// <summary>
    /// Saves the spreadsheet to the provided filepath.
    /// </summary>
    /// <param name="path">Filepath to be saved to.</param>
    public bool Save(string path);
}

