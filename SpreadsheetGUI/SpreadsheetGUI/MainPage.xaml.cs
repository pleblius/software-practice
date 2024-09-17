using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using SpreadsheetUtilities;
using SS;
using System.Text;

namespace SpreadsheetGUI;

/// <summary>
/// Example of using a SpreadsheetGUI object.
/// </summary>
public partial class MainPage : ContentPage
{
    string _savePath;

    /// <summary>
    /// Constructor for MAUI page.
    /// </summary>
	public MainPage()
    {
        InitializeComponent();

        _savePath = "";

        // SelectionChanged event should update displayed values to match new selection

        spreadsheetGrid.SelectionChanged += DisplaySelection;
        spreadsheetGrid.SelectionChanged += DisplayValue;
        spreadsheetGrid.SelectionChanged += DisplayContents;

        // ContentsUpdated event should update displayed values to match new cell contents and values
        spreadsheetGrid.ContentsUpdated += DisplayValue;
        spreadsheetGrid.ContentsUpdated += DisplayContents;

        // Default starting cell = "A1"
        _ = spreadsheetGrid.SetSelection(0, 0);
    }

    /// <summary>
    /// Displays/updates the selected cell name in the cell-name textbox.
    /// </summary>
    private void DisplaySelection(ISpreadsheetGrid grid)
    {
        spreadsheetGrid.GetSelection(out int col, out int row);

        CellName.Text = grid.GetCellName(col, row);
    }

    /// <summary>
    /// Displays/updates the cell's value in the cell-value textbox.
    /// </summary>
    private void DisplayValue(ISpreadsheetGrid grid)
    {
        spreadsheetGrid.GetSelection(out int col, out int row);

        _ = grid.GetValue(col, row, out string value);

        CellValue.Text = value;
    }
    
    /// <summary>
    /// Displays/updates the cell's stringform contents in the contents textbox.
    /// </summary>
    private void DisplayContents(ISpreadsheetGrid grid)
    {
        spreadsheetGrid.GetSelection(out int col, out int row);

        string contents = spreadsheetGrid.GetCellContents(col, row);

        CellContents.Text = contents;
    }

    /// <summary>
    /// Opens a new, empty spreadsheet.
    /// If this would result in erasing unsaved changes, a pop-up warning alerts the user and asks for confirmation.
    /// </summary>
    private async void NewClicked(Object sender, EventArgs e)
    {
        // If unsaved changes, prompt for user confirmation first
        if (spreadsheetGrid.IsChanged())
        {
            bool answer = await DisplayAlert("Losing data", "You have unsaved changes, would you like to continue?", "Yes", "No");
            if (answer)
            {
                spreadsheetGrid.Clear();

                // Clear save path
                _savePath = "";
            }
        }
        else
        {
            spreadsheetGrid.Clear();
            _savePath = "";
        }
    }

    /// <summary>
    /// Loads the selected file as a new spreadsheet object.
    /// If loading fails, a pop-up alert will notify the user.
    /// If this would result in erasing unsaved changes, a pop-up warning alerts the user and asks for confirmation.
    /// </summary>
    private async void OpenClicked(Object sender, EventArgs e)
    {
        // If the operations would erase unsaved data, prompt user
        if (spreadsheetGrid.IsChanged())
        {
            bool answer = await DisplayAlert("Losing data", "You have unsaved changes, would you like to continue?", "Yes", "No");

            // If user does not wish to continue, return
            if (!answer)
            {
                return;
            }
        }

        try
        {
            FileResult fileResult = await FilePicker.Default.PickAsync();

            if (fileResult != null)
            {
                spreadsheetGrid.Clear();
                spreadsheetGrid.Load(fileResult.FullPath);
                _savePath = fileResult.FullPath;
            }
        }
        catch (Exception)
        {
            await DisplayAlert("Load Error", "Failed to load file", "Okay");
        }
    }

    /// <summary>
    /// Saves the spreadsheet as a .sprd file to the previous save location, chosen either
    /// from where the spreadsheet was loaded if Open() has been called at any point, or from the 
    /// last invocation of SaveAs().
    /// If no previous save location exists, this function defaults to calling SaveAs().
    /// </summary>
    private async void SaveClicked(Object sender, EventArgs e)
    {
        if (_savePath == "")
        {
            SaveAsClicked(sender, e);
        }
        else
        {
            if (!spreadsheetGrid.Save(_savePath))
            {
                await DisplayAlert("Save Error", "Failed to save file.", "Okay");
            }
        }
    }

    /// <summary>
    /// Saves the spreadsheet as a .sprd file to the selected save location, saving that file path
    /// as the default save location for future calls to Save().
    /// </summary>
    private async void SaveAsClicked(Object sender, EventArgs e)
    {
        await SaveFile(new CancellationToken());
    }

    /// <summary>
    /// Asynchronous save function. Uses the MAUI filesaver toolkit to generate a filepath,
    /// then calls the spreadsheet's native save function to that same filepath.
    /// 
    /// If any exception occurs, a warning pop-up is displayed.
    /// </summary>
    private async Task SaveFile(CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(Encoding.Default.GetBytes(""));
        var fileSaverResult = await FileSaver.Default.SaveAsync(".sprd", stream, cancellationToken);

        if (!fileSaverResult.IsSuccessful)
        {
            await DisplayAlert("Save Error", "Failed to save file.", "Okay");
            return;
        }

        _savePath = fileSaverResult.FilePath;

        if (!spreadsheetGrid.Save(_savePath))
        {
            await DisplayAlert("Save Error", "Failed to save file.", "Okay");
        }
    }

    /// <summary>
    /// Undoes the most recent operation.
    /// If no operations have been committed, this method does nothing.
    /// </summary>
    private void UndoClicked(Object sender, EventArgs e)
    {
        _ = spreadsheetGrid.Undo();
    }

    /// <summary>
    /// Redoes the most recently undone operation.
    /// If no operations have been undone, this method does nothing.
    /// </summary>
    private void RedoClicked(Object sender, EventArgs e)
    {
        _ = spreadsheetGrid.Redo();
    }

    /// <summary>
    /// Copies the contents of the currently selected cell to be pasted later.
    /// </summary>
    private void CopyClicked(Object sender, EventArgs e)
    {
        spreadsheetGrid.Copy();
    }

    /// <summary>
    /// Copies the contents of the currently selected cell to be pasted later, then deletes
    /// the copied cell.
    /// </summary>
    private void CutClicked(Object sender, EventArgs e)
    {
        spreadsheetGrid.Cut();
    }

    /// <summary>
    /// Pastes the copied contents into the selected cell, overriding any extant data.
    /// If no data has been copied or cut, this method does nothing.
    /// If pasting the data would result in an invalid cell contents (due to a FormulaFormatException, for example),
    /// this method alerts the user with a pop-up warning and does nothing.
    /// </summary>
    private async void PasteClicked(Object sender, EventArgs e)
    {
        try
        {
            spreadsheetGrid.Paste();
        }
        catch (Exception ex)
        {
            // Same error block as update contents (without the call to set value)
            string msg = "";

            if (ex is CircularException)
            {
                msg = "Formula resulted in circular dependency. Spreadsheet left unchanged.\n";
            }
            else if (ex is InvalidNameException)
            {
                msg = "Formula resulted in invalid name. Spreadsheet left unchanged.\n";
            }
            else if (ex is FormulaFormatException)
            {
                msg = "Formula has an invalid format. Spreadsheet left unchanged.\n";
                msg += ex.Message;
            }

            await DisplayAlert("Error", msg, "Okay");
        }
    }

    /// <summary>
    /// Updates the cell contents and spreadsheet when the "CellContents" entry box
    /// is changed.
    /// If changes would result in an invalid formula, an error popup is displayed and the change is undone.
    /// </summary>
    private async void UpdateContents(Object sender, EventArgs e)
    {
        spreadsheetGrid.GetSelection(out int col, out int row);

        // Try to update contents
        try
        {
            _ = spreadsheetGrid.SetValue(col, row, CellContents.Text);
        }
        catch (Exception ex)
        {
            string msg = "";

            if (ex is CircularException)
            {
                msg = "Formula resulted in circular dependency. Spreadsheet left unchanged.\n";
            }
            else if (ex is InvalidNameException)
            {
                msg = "Formula resulted in invalid name. Spreadsheet left unchanged.\n";
            }
            else if (ex is FormulaFormatException)
            {
                // FormulaFormatException displays which error was invoked.
                msg = "Formula has an invalid format. Spreadsheet left unchanged.\n";
                msg += ex.Message;
            }

            await DisplayAlert("Error", msg, "Okay");
        }
    }

    /***************************** HELP METHODS *********************************/

    /// <summary>
    /// Help method that shows the user a popup, explaining how cell-selection and the display works.
    /// </summary>
    private async void SelectionHelpClicked(Object sender, EventArgs e)
    {
        await DisplayAlert("Selecting", 
            "Clicking on any cell allows you to select it. When selected, a cell shows it's name and value below the menu bar. " +
            "A1 is the default selection.", 
            "OK");
    }

    /// <summary>
    /// Help method that shows the user how to update a cell's contents.
    /// </summary>
    private async void SettingContentsHelpClicked(Object sender, EventArgs e)
    {
        await DisplayAlert("Setting Contents",
            "The contents box allows you to set the contents of the cell. You can input any number or text you want by typing into " +
            "the contents box, then pressing enter. Formulas are input by putting an equals sign at the beginning \n(e.g. =a1+3). " +
            "Cell names can be upper or lowercase.",
            "OK");
    }

    /// <summary>
    /// Help method that shows the user how to open a new spreadsheet file.
    /// </summary>
    private async void NewFileHelpClicked(Object sender, EventArgs e)
    {
        await DisplayAlert("New Files",
            "File -> New lets you create a new spreadsheet. " +
            "If you have any unsaved data, a pop-up will request your confirmation before proceeding.",
            "OK");
    }

    /// <summary>
    /// Help method that shows the user how to load a previously saved spreadsheet file.
    /// </summary>
    private async void OpenFileHelpClicked(Object sender, EventArgs e)
    {
        await DisplayAlert("Opening Files",
            "File -> Open lets you open a previously saved spreadsheet from a file. " +
            "If the file is improperly formatted, it will fail to load. " +
            "If you have any unsaved data, a pop-up will request your confirmation before proceeding.",
            "OK");
    }

    /// <summary>
    /// Help method that shows the user how to save a file using the "Save" or "SaveAs" button.
    /// </summary>
    private async void SavingFileHelpClicked(Object sender, EventArgs e)
    {
        await DisplayAlert("Saving Files",
            "File -> Save OR File -> Save As allows you to save your spreadsheet to a .sprd file. " +
            "The SaveAs button will allow you to select a name and destination to save your file. " +
            "\nEXTRA FEATURE.The Save button will attempt to save your file to its stored filepath (if it has been saved " +
            "or loaded from a specific path), or will allow you to select a name and destination to save.",
            "OK");
    }

    /// <summary>
    /// Help method that shows the user how to use the spreadsheet's Undo/Redo functionality.
    /// </summary>
    private async void UndoRedoHelpClicked(Object sender, EventArgs e)
    {
        await DisplayAlert("Undo and Redo",
            "EXTRA FEATURE. You can undo and redo your changes. Any change made to a spreadsheet can be undone. " +
            "After an operation has been undone, you can then redo the operation if you change your mind. " +
            "You can undo as many changes as you've made. Any changes made after undoing an action will eliminate the " +
            "ability to redo that action.",
            "OK");
    }

    /// <summary>
    /// Help method that shows the user how to use the spreadsheet's Cut/Copy/Paste functionality.
    /// </summary>
    private async void CutCopyPasteHelpClicked(Object sender, EventArgs e)
    {
        await DisplayAlert("Cut, Copy, Paste",
            "EXTRA FEATURE. You can use the Edit menu option to cut, copy, and paste. Edit -> Copy allows you to copy the contents of a cell. " +
            "You can then use Edit -> Paste to paste the contents into another cell. If this would result in invalid cell contents, " +
            "no change will be made. Edit -> Cut copies and removes the contents of a given cell. ",
            "OK");
    }

    /// <summary>
    /// Help method that explains how the the spreadsheet handles loss of data or overwriting stored data.
    /// </summary>
    private async void OverwritingFileHelpClicked(Object sender, EventArgs e)
    {
        await DisplayAlert("Overwriting Files",
            "If any action you take when opening or creating a new spreadsheet would overwrite saved work, " +
            "or would result in a loss of unsaved data, you will be warned and prompted to confirm your decision. " +
            "If you try and close the application without saving, only God can help you. Sorry.",
            "OK");
    }

    /// <summary>
    /// Help method that explains how Windows dark mode is incompatible with this application.
    /// </summary>
    private async void DarkModeHelpClicked(Object sender, EventArgs e)
    {
        await DisplayAlert("Dark Mode",
            "Dark mode on Windows makes the text in the contents box nearly unreadable. Please use light mode if you're having this issue. Sorry.",
            "OK");
    }
}
