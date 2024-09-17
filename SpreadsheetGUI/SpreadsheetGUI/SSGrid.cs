// Written by Alexa Fresz and Tyler Wilcox, October 2023
// Using code by Joe Zachary and Travis Martin for CS 3500, September 2011, 2023
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using PointF = Microsoft.Maui.Graphics.PointF;
using System.Text.RegularExpressions;
using SpreadsheetUtilities;

namespace SS;

class SSGrid : ScrollView, IDrawable, ISpreadsheetGrid
{
    /// <summary>
    /// Private data-storage class to keep track of changes to the spreadsheet.
    /// Used for implementation of Undo and Redo buttons by storing old and previous values.
    /// Used for implementation of the Cut/Copy/Paste buttons by storing the value being copied.
    /// </summary>
    private class Change
    {
        internal string Name { get; }
        internal string NewContents { get; set; }
        internal string OldContents { get; set; }

        /// <summary>
        /// Creates a new change object, tracking the changed cell's name, the cell's new contents, and the
        /// cell's contents that were replaced.
        /// </summary>
        /// <param name="name">Cell that was changed.</param>
        /// <param name="newContents">The new contents in the cell.</param>
        /// <param name="oldContents">The old contents of the cell.</param>
        public Change(string name, string newContents, string oldContents)
        {
            Name = name;
            NewContents = newContents;
            OldContents = oldContents;
        }
    }

    // Current spreadsheet version
    private const string VERSION = "ps6";

    // SelectionChanged checks when a new cell is clicked on in the GUI.
    public event SelectionChangedHandler SelectionChanged;
    // ContentsUpdated checks whenever any cell's contents/value are changed.
    public event ContentsUpdatedHandler ContentsUpdated;

    // These constants control the layout of the spreadsheet grid.
    // The height and width measurements are in pixels.
    private const int DATA_COL_WIDTH = 80;
    private const int DATA_ROW_HEIGHT = 20;
    private const int LABEL_COL_WIDTH = 30;
    private const int LABEL_ROW_HEIGHT = 30;
    private const int PADDING = 4;
    private const int COL_COUNT = 26;
    private const int ROW_COUNT = 99;
    private const int FONT_SIZE = 12;

    // Columns and rows are numbered beginning with 0.  This is the coordinate
    // of the selected cell.
    private int _selectedCol;
    private int _selectedRow;

    // Coordinate of cell in upper-left corner of display
    private int _firstColumn = 0;
    private int _firstRow = 0;

    // Scrollbar positions
    private double _scrollX = 0;
    private double _scrollY = 0;

    // The spreadsheet object modeling the data stored in the GUI.
    private Spreadsheet _spreadsheet;

    // Stacks for undoing and redoing operations.
    private Stack<Change> undoStack;
    private Stack<Change> redoStack;

    // Holds the most recently copied cell data for later pasting.
    private Change copy;

    // GraphicsView maintains the actual drawing of the grid and listens
    // for click events
    private GraphicsView graphicsView = new();

    // Spreadsheet function templates for cell noramlization and validation.
    private readonly Func<string, bool> _validator;
    private readonly Func<string, string> _normalizer;

    public SSGrid()
    {
        // Create a new spreadsheet template for the GUI
        _normalizer = s => s.ToUpper();
        _validator = Validate;
        _spreadsheet = new Spreadsheet(_validator, _normalizer, VERSION);

        // A new spreadsheet should have empty undo/redo stacks and no pastable data.
        undoStack = new Stack<Change>();
        redoStack = new Stack<Change>();
        copy = null;
        
        // Controls how the spreadsheet is drawn in the GUI.
        BackgroundColor = Colors.LightGray;
        graphicsView.Drawable = this;
        graphicsView.HeightRequest = LABEL_ROW_HEIGHT + (ROW_COUNT + 1) * DATA_ROW_HEIGHT;
        graphicsView.WidthRequest = LABEL_COL_WIDTH + (COL_COUNT + 1) * DATA_COL_WIDTH;
        graphicsView.BackgroundColor = Colors.LightGrey;
        graphicsView.EndInteraction += OnEndInteraction;
        this.Content = graphicsView;
        this.Scrolled += OnScrolled;
        this.Orientation = ScrollOrientation.Both;
    }

    /// <summary>
    /// Validates the name of the provided string according to the spreadsheet rules,
    /// [Capital Letter][# from 1-99].
    /// </summary>
    /// <param name="s">Name being validated.</param>
    /// <returns>True if name is valid, false otherwise.</returns>
    private bool Validate(string s) => Regex.IsMatch(s, "^[A-Z][1-9][0-9]?$");

    /// <summary>
    /// Resets the spreadsheet to its starting configuration, clearing all data,
    /// clearing the undo and redo stacks, and resetting the selected cell.
    /// </summary>
    public void Clear()
    {
        _spreadsheet = new Spreadsheet(_validator, _normalizer, VERSION);
        undoStack.Clear();
        redoStack.Clear();
        _ = SetSelection(0, 0);

        // Redraw
        Invalidate();
    }

    /// <summary>
    /// Checks if the spreadsheet has been changed since the last save.
    /// </summary>
    /// <returns>True if the spreadsheet has been changed, false otherwise.</returns>
    public bool IsChanged()
    {
        return _spreadsheet.Changed;
    }

    /// <summary>
    /// Checks if the provided address is valid for the given spreadsheet's size.
    /// Gets the associated cell's name if the address is valid.
    /// </summary>
    /// <param name="col">Provided column value.</param>
    /// <param name="row">Provided row value.</param>
    /// <param name="name">Cell's string name.</param>
    /// <returns>True if the address is within the grid, false otherwise.</returns>
    private bool IsValidAddress(int col, int row, out string name)
    {
        if (col < 0 || row < 0 || col > COL_COUNT || row > ROW_COUNT)
        {
            name = null;

            return false;
        }

        name = GetCellName(col, row);
        return true;
    }

    /// <summary>
    /// Gets the address of a given cell in row, column form.
    /// e.g. "B3" -> (1, 2)
    /// </summary>
    /// <param name="name">The cell's name.</param>
    /// <param name="col">The address's column value.</param>
    /// <param name="row">The address's row value.</param>
    private static void GetCellAddressFromName(string name, out int col, out int row)
    {
        string colName = name[..1];
        string rowName = name[1..];

        // Converts cell name to (col, row), e.g. (B3) -> (1, 2)
        col = (int) colName[0] - 65;
        row = int.Parse(rowName) - 1;
    }

    /// <summary>
    /// Sets the currently selected cell to the cell at the given address.
    /// If the provided address is invalid, this method has no effect.
    /// </summary>
    /// <param name="col">Address's column value.</param>
    /// <param name="row">Address's row value.</param>
    /// <returns>True if the selection was valid, false otherwise.</returns>
    public bool SetSelection(int col, int row)
    {
        if (!IsValidAddress(col, row, out _))
        {
            return false;
        }

        _selectedCol = col;
        _selectedRow = row;

        // Update and redraw
        SelectionChanged?.Invoke(this);
        Invalidate();

        return true;
    }

    /// <summary>
    /// Gets the numeric address of the currently selected cell.
    /// </summary>
    /// <param name="col">Address's column value.</param>
    /// <param name="row">Address's row value.</param>
    public void GetSelection(out int col, out int row)
    {
        col = _selectedCol;
        row = _selectedRow;
    }

    /// <summary>
    /// Sets the contents of the cell at the given address to the specified string <paramref name="c"/>.
    /// If the address or the contents are invalid, this method has no effect.
    /// 
    /// If updating the cell's value would affect the value of other cells, those cells' values
    /// are also updated.
    /// 
    /// Stores the change in the undo stack for potential future undoing, and clears the redo stack.
    /// </summary>
    /// <param name="col">Address's column value.</param>
    /// <param name="row">Address's row value.</param>
    /// <param name="c">Value to set the cell to.</param>
    /// <returns>True if the value could be set, false otherwise.</returns>
    /// <exception cref="InvalidNameException">If <paramref name="c"/> is a Formula that contains an invalid cell name reference.</exception>
    /// <exception cref="FormulaFormatException">If <paramref name="c"/> is a Formula that has an invalid syntax.</exception>
    /// <exception cref="CircularException">If <paramref name="c"/> is a Formula that results in a circular dependency.</exception>
    public bool SetValue(int col, int row, string c)
    {
        if (!IsValidAddress(col, row, out string name))
        {
            return false;
        }

        string oldContents = GetCellContents(col, row);

        _ = _spreadsheet.SetContentsOfCell(name, c);

        // 
        undoStack.Push(new Change(name, c, oldContents));
        redoStack.Clear();

        // Redraw GUI
        ContentsUpdated?.Invoke(this);
        Invalidate();

        return true;
    }

    /// <summary>
    /// Gets the string version of the value of the cell at the provided address.
    /// If the address is invalid, this method returns false and c is left null.
    /// </summary>
    /// <param name="col">Address's column value.</param>
    /// <param name="row">Address's row value.</param>
    /// <param name="c">Specified cell's value as a string.</param>
    /// <returns>True if a value could be obtained, false otherwise.</returns>
    public bool GetValue(int col, int row, out string c)
    {
        if (!IsValidAddress(col, row, out string name)) 
        {
            c = null;
            return false;
        }
        object value = _spreadsheet.GetCellValue(name);

        if(value is FormulaError)
        {
            c = ((FormulaError) _spreadsheet.GetCellValue(name)).Reason;
            return true;
        }

        c = value.ToString();
        return true;
    }

    /// <summary>
    /// Converts the associated grid address to cell's string name,
    /// e.g. (0,0) -> "A1".
    /// </summary>
    /// <param name="col">Column value.</param>
    /// <param name="row">Row value.</param>
    /// <returns>The string form of the cell's name.</returns>
    public string GetCellName(int col, int row)
    {
        // Converts address to cell name, e.g. (0,0) -> "A1"
        string colName = ((char) (col + 65)).ToString();
        string rowName = (row + 1).ToString();

        return colName + rowName;
    }

    /// <summary>
    /// Gets the string form of the contents of the cell at the provided coordinates.
    /// </summary>
    /// <param name="col">Column value.</param>
    /// <param name="row">Row value.</param>
    /// <returns>The string form of the cell's contents.</returns>
    public string GetCellContents(int col, int row)
    {
        string cellName = GetCellName(col, row);

        if (_spreadsheet.Cells.ContainsKey(cellName))
        {
            return _spreadsheet.Cells[cellName].StringForm;
        }

        return "";
    }

    /// <summary>
    /// Attempts to save the spreadsheet to the provided filepath.
    /// If the file cannot be saved, an informative alert is displayed to the user
    /// and the file remains unchanged.
    /// </summary>
    /// <returns>True if the file was successfully saved, false otherwise.</returns>
    /// <param name="filepath">File path to be saved to</param>
    public bool Save(string filepath)
    {
        _spreadsheet.Save(filepath);

        return true;
    }

    /// <summary>
    /// Attempts to load the file from the provided filepath.
    /// If the load fails, 
    /// </summary>
    /// <param name="filepath"></param>
    /// <returns>True if the file was successfully loaded, false otherwise.</returns>
    public bool Load(string filepath)
    {
        _spreadsheet = new Spreadsheet(filepath, _validator, _normalizer, VERSION);

        ContentsUpdated?.Invoke(this);
        Invalidate();

        return true;
    }

    /// <summary>
    /// Attempts to undo the most recent previous operation. If there are no operations to undo, this will return false.
    /// </summary>
    /// <returns>True if an operation was undone, false otherwise.</returns>
    public bool Undo()
    {
        if (undoStack.TryPop(out Change change))
        {
            _ = _spreadsheet.SetContentsOfCell(change.Name, change.OldContents);
            ContentsUpdated?.Invoke(this);

            // Adds change to redo stack for future redoing
            redoStack.Push(change);

            Invalidate();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to redo the most recently undone previous operation. If there are no operations to redo, this will return false.
    /// </summary>
    /// <returns>True if an operation was redone, false otherwise.</returns>
    public bool Redo()
    {
        if (redoStack.TryPop(out Change change))
        {
            _ = _spreadsheet.SetContentsOfCell(change.Name, change.NewContents);
            ContentsUpdated?.Invoke(this);

            // Adds change to undo stack for potential undoing
            undoStack.Push(change);

            Invalidate();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Copies the data from the current cell to be pasted into a future cell.
    /// </summary>
    public void Copy()
    {
        GetSelection(out int col, out int row);

        string cellName = GetCellName(col, row);
        string copiedContents = GetCellContents(col, row);

        // Copies the cell's contents into the new and old field to not be overwritten
        copy = new Change(cellName, copiedContents, copiedContents);
        
    }

    /// <summary>
    /// Copies the data from the current cell to be pasted into a future cell.
    /// When the data is pasted, the copied cell is cleared of its data.
    /// </summary>
    public void Cut()
    {
        // Cut copies the data from the current cell
        Copy();
        //Get the col and row to set the desired cell to empty.
        GetSelection(out int col, out int row);

        // Sets the new value with an empty string, to overwrite previous cell.
        SetValue(col, row, "");
        Invalidate();   
    }

    /// <summary>
    /// Pastes the copied data into the selected cell.
    /// If no data has been copied, this method returns false.
    /// </summary>
    /// <returns>True if data is copied into the new cell, false otherwise.</returns>
    public void Paste()
    {
        if (copy is not null)
        {
            GetSelection(out int col, out int row);

            // Set selected cell to copied contents
            _ = SetValue(col, row, copy.OldContents);

            // Check if old cell needs to be overwritten by checking if they are different
            if (!copy.OldContents.Equals(copy.NewContents))
            {
                _ = _spreadsheet.SetContentsOfCell(copy.Name, copy.NewContents);
            }

            ContentsUpdated?.Invoke(this);
            redoStack.Clear();
            Invalidate();
        }
    }
    /***************************************** UI Methods ****************************************/

    /// <summary>
    /// Listener for scroll events. Redraws the panel, maintaining the
    /// row and column headers.
    /// </summary>
    private void OnScrolled(object sender, ScrolledEventArgs e)
    {
        _scrollX = e.ScrollX;
        _firstColumn = (int) e.ScrollX / DATA_COL_WIDTH;
        _scrollY = e.ScrollY;
        _firstRow = (int) e.ScrollY / DATA_ROW_HEIGHT;

        Invalidate();
    }

    /// <summary>
    /// Listener for click events on the grid.
    /// </summary>
    private void OnEndInteraction(object sender, TouchEventArgs args)
    {
        PointF touch = args.Touches[0];
        OnMouseClick(touch.X, touch.Y);
    }

    /// <summary>
    /// Determines which cell, if any, was clicked.  Generates a SelectionChanged
    /// event.  All of the indexes are zero based.
    /// </summary>
    private void OnMouseClick(float eventX, float eventY)
    {
        int x = (int) (eventX - _scrollX - LABEL_COL_WIDTH) / DATA_COL_WIDTH + _firstColumn;
        int y = (int) (eventY - _scrollY - LABEL_ROW_HEIGHT) / DATA_ROW_HEIGHT + _firstRow;

        if (eventX > LABEL_COL_WIDTH && eventY > LABEL_ROW_HEIGHT && (x < COL_COUNT) && (y < ROW_COUNT))
        {
            _selectedCol = x;
            _selectedRow = y;

            SelectionChanged?.Invoke(this);
        }

        Invalidate();
    }

    /// <summary>
    /// Redraws the application.
    /// </summary>
    private void Invalidate()
    {
        graphicsView.Invalidate();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Move the canvas to the place that needs to be drawn.
        canvas.SaveState();
        canvas.Translate((float) _scrollX, (float) _scrollY);

        // Color the background of the data area white
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(
            LABEL_COL_WIDTH,
            LABEL_ROW_HEIGHT,
            (COL_COUNT - _firstColumn) * DATA_COL_WIDTH,
            (ROW_COUNT - _firstRow) * DATA_ROW_HEIGHT);

        // Draw the column lines
        int bottom = LABEL_ROW_HEIGHT + (ROW_COUNT - _firstRow) * DATA_ROW_HEIGHT;
        canvas.DrawLine(0, 0, 0, bottom);
        for (int x = 0; x <= (COL_COUNT - _firstColumn); x++)
        {
            canvas.DrawLine(
                LABEL_COL_WIDTH + x * DATA_COL_WIDTH, 0,
                LABEL_COL_WIDTH + x * DATA_COL_WIDTH, bottom);
        }

        // Draw the column labels
        for (int x = 0; x < COL_COUNT - _firstColumn; x++)
        {
            DrawColumnLabel(canvas, x,
                (_selectedCol - _firstColumn == x) ? Font.Default : Font.DefaultBold);
        }

        // Draw the row lines
        int right = LABEL_COL_WIDTH + (COL_COUNT - _firstColumn) * DATA_COL_WIDTH;
        canvas.DrawLine(0, 0, right, 0);
        for (int y = 0; y <= ROW_COUNT - _firstRow; y++)
        {
            canvas.DrawLine(
                0, LABEL_ROW_HEIGHT + y * DATA_ROW_HEIGHT,
                right, LABEL_ROW_HEIGHT + y * DATA_ROW_HEIGHT);
        }

        // Draw the row labels
        for (int y = 0; y < (ROW_COUNT - _firstRow); y++)
        {
            DrawRowLabel(canvas, y,
                (_selectedRow - _firstRow == y) ? Font.Default : Font.DefaultBold);
        }

        // Highlight the selection, if it is visible
        if ((_selectedCol - _firstColumn >= 0) && (_selectedRow - _firstRow >= 0))
        {
            canvas.DrawRectangle(
                LABEL_COL_WIDTH + (_selectedCol - _firstColumn) * DATA_COL_WIDTH + 1,
                              LABEL_ROW_HEIGHT + (_selectedRow - _firstRow) * DATA_ROW_HEIGHT + 1,
                              DATA_COL_WIDTH - 2,
                              DATA_ROW_HEIGHT - 2);
        }

        // Draw the text
        foreach (string cellName in _spreadsheet.GetNamesOfAllNonemptyCells())
        {
            String text = _spreadsheet.GetCellValue(cellName).ToString();

            GetCellAddressFromName(cellName, out int col, out int row);

            col -= _firstColumn;
            row -= _firstRow;

            SizeF size = canvas.GetStringSize(text, Font.Default, FONT_SIZE + FONT_SIZE * 1.75f);
            canvas.Font = Font.Default;
            if (col >= 0 && row >= 0)
            {
                canvas.DrawString(text,
                    LABEL_COL_WIDTH + col * DATA_COL_WIDTH + PADDING,
                    LABEL_ROW_HEIGHT + row * DATA_ROW_HEIGHT + (DATA_ROW_HEIGHT - size.Height) / 2,
                    size.Width, size.Height, HorizontalAlignment.Left, VerticalAlignment.Center);
            }
        }
        canvas.RestoreState();
    }

    /// <summary>
    /// Draws a column label.  The columns are indexed beginning with zero.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="x"></param>
    /// <param name="f"></param>
    private void DrawColumnLabel(ICanvas canvas, int x, Font f)
    {
        String label = ((char) ('A' + x + _firstColumn)).ToString();
        SizeF size = canvas.GetStringSize(label, f, FONT_SIZE + FONT_SIZE * 1.75f);
        canvas.Font = f;
        canvas.FontSize = FONT_SIZE;
        canvas.DrawString(label,
              LABEL_COL_WIDTH + x * DATA_COL_WIDTH + (DATA_COL_WIDTH - size.Width) / 2,
              (LABEL_ROW_HEIGHT - size.Height) / 2, size.Width, size.Height,
              HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    /// <summary>
    /// Draws a row label.  The rows are indexed beginning with zero.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="y"></param>
    /// <param name="f"></param>
    private void DrawRowLabel(ICanvas canvas, int y, Font f)
    {
        String label = (y + 1 + _firstRow).ToString();
        SizeF size = canvas.GetStringSize(label, f, FONT_SIZE + FONT_SIZE * 1.75f);
        canvas.Font = f;
        canvas.FontSize = FONT_SIZE;
        canvas.DrawString(label,
            LABEL_COL_WIDTH - size.Width - PADDING,
            LABEL_ROW_HEIGHT + y * DATA_ROW_HEIGHT + (DATA_ROW_HEIGHT - size.Height) / 2,
            size.Width, size.Height,
              HorizontalAlignment.Right, VerticalAlignment.Center);
    }
}
