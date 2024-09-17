using SpreadsheetUtilities;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace SS;

/// <summary>
/// This class represents a spreadsheet object that implements the AbstractSpreadsheet class.
/// Data in a spreadsheet is stored in cells which have a <b>name</b>, <b>contents</b>, and a
/// <b>value</b>:<br></br>
/// <listheader><term>Name</term>
/// <description>A string is a valid cell name if and only if: </description></listheader>
/// <list type="bullet">
/// <item>Its first character is an underscore or a letter.</item>
/// <item>Its remaining characters (if any) are underscores and/or letters and/or digits.</item></list>
/// <listheader><term>Contents</term></listheader><description>Can be of the type:</description>
/// <list type="bullet">
/// <item>String</item>
/// <item>Double</item>
/// <item>Formula</item></list>
/// <listheader><term>Value</term></listheader><description>Can be of the type:</description>
/// <list type="bullet">
/// <item>String</item>
/// <item>Double</item>
/// <item>FormulaError</item></list>
/// If a cell's contents are an empty string, then it is an empty cell. If the contents are a string or a double,
/// then the cell's value is that string or double. If the contents are a formula, then the value will either be
/// a string, a double, or a FormulaError object if the formula cannot be evaluated.
/// </summary>
public class Spreadsheet : AbstractSpreadsheet
{ 
    // Dictionary contains a mapping of all non-empty cells, using the cell's name as a key.
    [JsonInclude]
    public Dictionary<string, Cell> Cells { get; private set; }

    // Dependency graph tracks dependencies in the spreadsheet for recalculation
    protected readonly DependencyGraph Dependencies;

    // Delegates to store normalizer and validator passed to the constructor.
    protected readonly Func<string, string> Normalizer;
    protected readonly Func<string, bool> Validator;

    /// <summary>
    /// Creates an empty Spreadsheet object with default version, default normalizer, and default validator.
    /// The default normalizer leaves variable names unchanged.
    /// The default validator always returns true for legal variable names.
    /// </summary>
    public Spreadsheet() : this(s => true, s => s, "default")
    { }

    /// <summary>
    /// Creates an empty spreadsheet with the specified version, using
    /// the default normalizer and validator.
    /// The default normalizer leaves variable names unchanged.
    /// The default validator always returns true for legal variable names.
    /// </summary>
    /// <param name="Version">This spreadsheet's version.</param>
    [JsonConstructor]
    public Spreadsheet(string Version) : this(s => true, s => s, Version)
    { }

    /// <summary>
    /// Creates a new, empty spreadsheet using the provided validator and normalizer functions
    /// and with the provided version.
    /// </summary>
    /// <param name="validator">A function to define the validity of cell/variable names. Takes a string input
    /// and outputs a boolean.</param>
    /// <param name="normalizer">A function to normalize cell/variable names. (Such as converting them to uppercase.)
    /// Takes a string input and outputs a new string.</param>
    /// <param name="version">This spreadsheet's version.</param>
    public Spreadsheet(Func<string, bool> validator, Func<string, string> normalizer, string Version) : base(Version)
    {
        Normalizer = normalizer;
        Validator = validator;
        Changed = false;

        Cells = new Dictionary<string, Cell>();
        Dependencies = new DependencyGraph();
    }

    /// <summary>
    /// Loads a spreadsheet from the given file at <paramref name="path"/> and creates a new spreadsheet using
    /// the provided validator, normalizer, and version.
    /// This method will throw an exception if any of the following occur:
    /// <list type="bullet">
    /// <item>The file cannot be opened or read from.</item>
    /// <item>The file is not in JSON format.</item>
    /// <item>The saved spreadsheet has a different version than the one provided.</item>
    /// <item>The saved spreadsheet is missing necessary iformation.</item>
    /// <item>Any variable or cell has an invalid name.</item>
    /// <item>Any Formula has an invalid format.</item>
    /// <item>Any Formula would create a circular dependency.</item>
    /// <item></item>
    /// </list>
    /// </summary>
    /// <param name="path">The filepath to load the spreadsheet from.</param>
    /// <param name="validator">A function to define the validity of cell/variable names. Takes a string input
    /// and outputs a new string.</param>
    /// <param name="normalizer">A function to normalize cell/variable names. (Such as converting them to uppercase.)
    /// Takes a string input and outputs a bool.</param>
    /// <param name="Version">The version of the spreadsheet being opened.</param>
    /// <exception cref="SpreadsheetReadWriteException">If path cannot be loaded.</exception>
    public Spreadsheet(string path, Func<string, bool> validator, Func<string, string> normalizer, string Version)
        : this(validator, normalizer, Version)
    {
        Spreadsheet ss = LoadSpreadsheet(path);

        if (ss.Version != Version)
        {
            throw new SpreadsheetReadWriteException("Loaded spreadsheet version incompatible with constructed version.");
        }

        try
        {
            LoadCells(ss);
        }
        catch (Exception e)
        {
            throw new SpreadsheetReadWriteException(e.Message);
        }

        Changed = false;
    }

    /// <summary>
    /// Loads a new spreadsheet object from the given file <paramref name="path"/>.
    /// Method deserializes the Json representation of the spreadsheet into a spreadsheet object,
    /// or throws an exception if it was unable to access the file or deserialize the text.
    /// </summary>
    /// <param name="path">The file to load the spreadsheet from.</param>
    /// <returns>A deserialized spreadsheet.</returns>
    /// <exception cref="SpreadsheetReadWriteException">If the method cannot load the spreadsheet.</exception>
    protected static Spreadsheet LoadSpreadsheet(string path)
    {
        if (!File.Exists(path))
        {
            throw new SpreadsheetReadWriteException($"File at {path} does not exist.");
        }

        Spreadsheet? ss;
        try
        {
            // Try to read and parse text from filepath
            string str = File.ReadAllText(path);

            ss = JsonSerializer.Deserialize<Spreadsheet>(str);
        }
        catch (Exception e)
        {
            throw new SpreadsheetReadWriteException($"Unable to load file from {path}. " + e.Message);
        }

        return ss is not null ? ss : throw new SpreadsheetReadWriteException($"Spreadsheet at {path} is null.");
    }

    /// <summary>
    /// Loads the cells from the provided spreadsheet into this spreadsheet.
    /// Copies all cell information and then updates the cell values.
    /// Throws an exception if an invalid name, a circular dependency, or an illegal formula is encountered.
    /// </summary>
    /// <param name="ss">The spreadsheet being copied.</param>
    /// <exception cref="SpreadsheetReadWriteException">If an invalid name or illegal formula is copied.</exception>
    protected void LoadCells(Spreadsheet ss)
    {
        CopyCells(ss);

        // Once all cells are copied, update cell values according to their formulas to avoid redundancy.
        LoadCellValues();
    }

    /// <summary>
    /// Adds all cells from the given spreadsheet to this spreadsheet.
    /// </summary>
    /// <param name="ss">The spreadsheet being copied.</param>
    protected void CopyCells(Spreadsheet ss)
    {
        // Add all loaded cells to this spreadsheet.
        foreach (string s in ss.GetNamesOfAllNonemptyCells())
        {
            string normalName = ValidateName(s);

            // Add cell and copy contents
            if (ss.Cells.TryGetValue(s, out Cell? cell))
            {
                string stringForm = cell.StringForm;

                Cells.Add(normalName, new Cell(stringForm));

                _ = ParseCellFields(normalName, stringForm);
            }
        }
    }

    /// <summary>
    /// Loads the cell values and string forms, in order from least-to-most dependent using
    /// GetCellsToRecalculate(). Will calculate values in dependent "batches."
    /// Eliminates redundant calculations of formula values.
    /// </summary> 
    private void LoadCellValues()
    {
        HashSet<string> calculatedCells = new();

        foreach (string cellName in GetNamesOfAllNonemptyCells())
        {
            // If this cell's batch has already been calculated, skip this cell.
            if (!calculatedCells.Contains(cellName))
            {
                // Get top cell of this batch
                string topCell = GetTopDependeeName(cellName);

                // Iterate over batch dependents, updating each in dependency order
                IEnumerable<string> dependentList = GetCellsToRecalculate(topCell);

                UpdateCells(dependentList);

                // Add list to set
                calculatedCells.UnionWith(dependentList);
            }
        }
    }

    /// <summary>
    /// Recursive method that gets the top-level dependee of the set of cells that contain <paramref name="name"/>.
    /// If <paramref name="name"/> is the top-level dependee, it returns itself.
    /// </summary>
    /// <param name="name">The name of the string whose top dependees are being searched for.</param>
    /// <returns>The string at the top of the dependency-hierarchy.</returns>
    protected string GetTopDependeeName(string name)
    {
        if (Dependencies.HasDependees(name))
        {
            string dependee = Dependencies.GetDependees(name).First();

            return GetTopDependeeName(dependee);
        }

        return name;
    }

    /// <summary>
    /// Checks if the provided name is a valid variable or cell name, using
    /// the provided normalizer and validator for the spreadsheet.
    /// A valid name must be both valid according to the validator and legal 
    /// according to the spreadsheet's definition of a name.
    /// </summary>
    /// <param name="name">The name being checked.</param>
    /// <returns><see langword="true"/> if the <paramref name="name"/> is both valid and legal, <see langword="false"/> otherwise.</returns>
    protected bool IsValidName(string name, out string normalName)
    {
        normalName = Normalizer(name);

        return Validator(normalName) && IsLegalName(normalName);
    }

    /// <summary>
    /// Checks if the name is a valid cell name for this spreadsheet.
    /// A valid cell name begins with a letter (case insensitive) or an underscore.
    /// It is then followed by any combination of 0 or more letters, underscores, or digits.
    /// </summary>
    /// <param name="name">The name being checked for validity.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> is a valid name, <see langword="false"/> otherwise.</returns>
    protected static bool IsLegalName(string name) => Regex.IsMatch(name, "^[a-zA-Z_][a-zA-Z_0-9]*$");

    /// <summary>
    /// Gets the normalized, validated name for the provided string.
    /// Throws an exception if the normalized form of the name cannot be validated.
    /// </summary>
    /// <param name="name">Name being validated.</param>
    /// <returns>The normalized name using this spreadsheet's normalizer function.</returns>
    /// <exception cref="InvalidNameException">If the cell name is illegal or invalid.</exception>
    protected string ValidateName(string name)
    {
        return IsValidName(name, out string normalName) ? normalName : throw new InvalidNameException();
    }

    /// <summary>
    /// Checks if the provided string can be parsed as a Formula object.
    /// A string is a Formula if the first character is '='.
    /// Throws an exception if the string is a formula but is improperly formatted.
    /// </summary>
    /// <param name="content">The string being parsed.</param>
    /// <exception cref="FormulaFormatException">If the string starts with '=' but is improperly formated.</exception>
    /// <returns><see langword="true"/> if <paramref name="content"/> can be parsed into a Formula, <see langword="false"/> otherwise.</returns>
    protected bool IsFormula(string content, [NotNullWhen(true)] out Formula? f)
    {
        // Check first character of content
        if (content.Length > 0 && content[0] == '=')
        {
            f = new Formula(content[1..], Normalizer, Validator);

            return true;
        }
        else
        {
            f = null;

            return false;
        }
    }

    /// <summary>
    /// Returns the contents of the cell with the given name.
    /// Contents can be either a string, a double, or a Formula object.
    /// If the cell is empty, this method returns an empty string.
    /// </summary>
    /// <param name="name">The name of the cell whose contents are returned.</param>
    /// <returns>The contents of <paramref name="name"/>, or "" if it is empty. </returns>
    /// <exception cref="InvalidNameException">If <paramref name="name"/> is an invalid cell name.</exception>
    public override object GetCellContents(string name)
    {
        string normalName = ValidateName(name);

        // Return an empty string if the cell is not in the cell map
        return Cells.TryGetValue(normalName, out Cell? cell) ? cell.Contents : "";
    }

    /// <summary>
    /// Returns the value of the cell with the given name.
    /// Value can be either a string, a double, or a FormulaError object.
    /// If the cell is empty, this method returns an empty string.
    /// </summary>
    /// <param name="name">The name of the cell whose value is returned.</param>
    /// <returns>The contents of <paramref name="name"/> as a string, a double, or a FormulaError. </returns>
    /// <exception cref="InvalidNameException">If <paramref name="name"/> is an invalid cell name.</exception>
    public override object GetCellValue(string name)
    {
        string normalName = ValidateName(name);

        // Return an empty string if the cell is not in the cell map
        return Cells.TryGetValue(normalName, out Cell? cell) ? cell.Value : "";
    }

    /// <summary>
    /// Enumerates the names of all non-empty cells in this spreadsheet. A cell is considered non-empty
    /// if its contents are anything other than the empty string "".
    /// </summary>
    /// <returns>An enumeration of all non-empty cells in this spreadsheet.</returns>
    public override IEnumerable<string> GetNamesOfAllNonemptyCells()
    {
        // All cells in cell map are defined as not empty.
        return Cells.Keys;
    }

    /// <summary>
    /// <para>Sets the contents of the given cell based on the contents of the string <paramref name="content"/>.
    /// If <paramref name="content"/> can be parsed as a double, the contents and value will be added as a double.
    /// If <paramref name="content"/> can be parsed as a Formula, the contents will be updated with a Formula and
    /// the value as a double or a FormulaError.
    /// Otherwise, the string will be set as the cell's contents and value.
    /// </para>
    /// <para>This method returns a list containing all cells that depend on <paramref name="name"/> for their value,
    /// including <paramref name="name"/> itself. These are the cells that must be recalculated to have an accurate
    /// value after changing the contents of <paramref name="name"/>. 
    /// The order of cells in the list will be any order such that every cell will be evaluated
    /// before any of its dependents.
    /// </para>
    /// Throws an exception if the name is invalid, if a formula is added with an improper format, or if a formula
    /// would create a circular dependency.
    /// If an exception is thrown, no change is made to the spreadsheet.
    /// </summary>
    /// <param name="name">The name of the cell being updated.</param>
    /// <param name="content">The string form of the content added to the cell.</param>
    /// <returns>A list of cell's depending on <paramref name="name"/> for their value, in evaluation order.</returns>
    /// <exception cref="InvalidNameException">If <paramref name="name"/>is invalid.</exception>
    /// <exception cref="FormulaFormatException">If <paramref name="content"/> is a Formula object with improper format.</exception>
    /// <exception cref="CircularException">If <paramref name="content"/> is a Formula that creates a circular dependency.</exception>
    public override IList<string> SetContentsOfCell(string name, string content)
    {
        string normalName = ValidateName(name);

        // Holds old StringForm value in case of reset.
        string oldString = GetOldString(normalName);

        IList<string> dependentList;

        // Check content type, also checking for Formula exceptions.
        try
        {
            dependentList = ParseCellFields(normalName, content);
        }
        catch
        {
            // Reset values if exception is caught
            _ = SetContentsOfCell(normalName, oldString);

            throw;
        }

        // Update spreadsheet cells with new information.
        UpdateCells(dependentList);
        Changed = true;

        return dependentList;
    }

    /// <summary>
    /// Parses the content string and passes it into the correct SetContents method, where
    /// its value, contents, and stringform are updated.
    /// </summary>
    /// <param name="name">Cell name to be updated.</param>
    /// <param name="content">String to be parsed.</param>
    /// <returns>A list of the cell's dependent on <paramref name="name"/>, in order of how they must be
    /// evaluated.</returns>
    protected IList<string> ParseCellFields(string name, string content)
    {
        // Update cell's contents based on object type passed in:
        return 
            IsFormula(content, out Formula? f)
            ? SetCellContents(name, f).ToList<string>()
            : double.TryParse(content, out double d)
                ? SetCellContents(name, d).ToList<string>()
                : SetCellContents(name, content).ToList<string>();
    }

    /// <summary>
    /// Sets the contents of the named cell to the given number, then
    /// returns a list containing the names of every cell whose value depends, directly or indirectly,
    /// on <paramref name="name"/>--including itself.
    /// </summary>
    /// <param name="name">The name of the cell whose contents are to be changed.</param>
    /// <param name="number">The double to become the cell's new contents.</param>
    /// <returns>A list of the cell's dependent on <paramref name="name"/>, in order of how they must be
    /// evaluated.</returns>
    protected override IList<string> SetCellContents(string name, double number)
    {
        // Update the contents and value to the number, and return the list of dependents.

        Cells[name].Contents = number;

        UpdateDependencies(name, number);

        return new List<string>(GetCellsToRecalculate(name));
    }

    /// <summary>
    /// Sets the contents of the named cell to the given string, then
    /// returns a list containing the names of every cell whose value depends, directly or indirectly,
    /// on <paramref name="name"/>--including itself.
    /// </summary>
    /// <param name="name">The name of the cell whose contents are to be changed.</param>
    /// <param name="text">The string to become the cell's new contents.</param>
    /// <returns>A list of the cell's dependent on <paramref name="name"/>, in order of how they must be
    /// evaluated.</returns>
    protected override IList<string> SetCellContents(string name, string text)
    {
        if (text == "")
        {
            _ = Cells.Remove(name);
        }
        // Update the contents and value to the string, and return the list of dependents.
        else
        {
            Cells[name].Contents = text;
        }

        UpdateDependencies(name, text);

        return new List<string>(GetCellsToRecalculate(name));
    }

    /// <summary>
    /// Sets the contents of the named cell to the given Formula, then
    /// returns a list containing the names of every cell whose value depends, directly or indirectly,
    /// on <paramref name="name"/>--including itself.
    /// </summary>
    /// <param name="name">The name of the cell whose contents are to be changed.</param>
    /// <param name="formula">The Formula to become the cell's new contents.</param>
    /// <returns>A list of the cell's dependent on <paramref name="name"/>, in order of how they must be
    /// evaluated.</returns>
    /// <exception cref="CircularException">If <paramref name="formula"/> would cause a circular dependency.</exception>
    protected override IList<string> SetCellContents(string name, Formula formula)
    {
        // Update list of dependees, checking for a circular dependency
        UpdateDependencies(name, formula);

        // Update contents and value
        Cells[name].Contents = formula;

        return new List<string>(GetCellsToRecalculate(name));
    }

    /// <summary>
    /// Updates the values and stringforms of each cell in <paramref name="dependents"/>.
    /// </summary>
    /// <param name="dependents">The list of dependents to be updated.</param>
    protected void UpdateCells(IEnumerable<string> dependents)
    {
        foreach (string cellName in dependents)
        {
            if (Cells.TryGetValue(cellName, out Cell? cell))
            {
                cell.UpdateValue(Lookup);
                cell.UpdateStringForm();
            }
        }
    }

    /// <summary>
    /// Gets the old value stored in the named cell's StringForm field.
    /// If the cell does not exist, creates it and stores the empty string in its fields.
    /// </summary>
    /// <param name="name">The cell whose StringForm is saved.</param>
    /// <returns>The cell's old StringForm.</returns>
    protected string GetOldString(string name)
    {
        string oldString;

        // Create cell if it doesn't exist, otherwise store old contents
        if (!Cells.TryGetValue(name, out Cell? cell))
        {
            oldString = "";
            Cells.Add(name, new Cell(oldString));
        }
        else
        {
            oldString = cell.StringForm;
        }

        return oldString;
    }

    /// <summary>
    /// Updates the dependency graph based on the contents being added to the cell.
    /// If the new contents are not a formula, it will delete all dependees for the cell
    /// (if it had any). If they are a formula, it will replace them with the new set of dependees
    /// based on the variables in the formula.
    /// </summary>
    /// <param name="name">The cell whose dependees are updated.</param>
    /// <param name="contents">The contents being added to the cell.</param>
    protected void UpdateDependencies(string name, object contents)
    {
        // If not a formula, clears all dependees
        IEnumerable<string> newDependees = new List<string>();

        // If contents are a formula, add variables to new dependee list.
        if (contents is Formula f)
        {
            newDependees = f.GetVariables();
        }

        Dependencies.ReplaceDependees(name, newDependees);
    }

    /// <summary>
    /// Lookup function to pass as delegate parameter into Formula.Evaluate().
    /// Uses cached cell values to get the value of the variable assigned,
    /// or throws an ArgumentException if the variable does not have a properly formatted value.
    /// </summary>
    /// <param name="name">The variable whose value is looked up.</param>
    /// <returns>A double corresponding to the cell's value.</returns>
    /// <exception cref="ArgumentException">If the lookup fails to produce a numeric value.</exception>
    protected double Lookup(string name)
    {
        // Check if cell exists
        if (!Cells.TryGetValue(name, out Cell? cell))
        {
            throw new ArgumentException($"Cell {name} does not exist.");
        }
        // Check if cell value isn't a number
        else if (cell.Value is not double)
        {
            throw new ArgumentException($"Cell {name} does not have a numeric value.");
        }

        return (double) GetCellValue(name);
    }

    /// <summary>
    /// Gets an enumeration with a copy of each cell that is direcly dependent on <paramref name="name"/>
    /// for its value. A cell is directly dependent on <paramref name="name"/> if <paramref name="name"/> appears
    /// in that cell's Formula at least once.
    /// Duplicate values are ignored.
    /// </summary>
    /// <param name="name">The name of the cell whose dependents are enumerated.</param>
    /// <returns>An enumeration with a single copy of each direct dependent of <paramref name="name"/>.</returns>
    /// <exception cref="InvalidNameException">If <paramref name="name"/> is an invalid cell name.</exception>
    protected override IEnumerable<string> GetDirectDependents(string name)
    {
        return Dependencies.GetDependents(name);
    }

    /// <summary>
    /// <para>Saves the current spreadsheet to the file at the provided filepath.
    /// Stores contents of spreadsheet in serialized Json format.
    /// Will overwrite any files with the same name in the same folder.
    /// Updates the spreadsheet's changed value to <see langword="false"/>.
    /// </para>
    /// </summary>
    /// <param name="filename">The file and path to save to.</param>
    /// <exception cref="SpreadsheetReadWriteException">If the file cannot be written to.</exception>
    public override void Save(string filename)
    {
        // Create formatted JSON string representation
        JsonSerializerOptions jso = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string sheet = JsonSerializer.Serialize(this, jso);

        try
        {
            // Create or overwrite file path
            using (StreamWriter sw = File.CreateText(filename))
            {
                sw.Write(sheet);
            }
        }
        catch (Exception e)
        {
            throw new SpreadsheetReadWriteException(e.Message);
        }

        Changed = false;
    }

    /// <summary>
    /// A cell object that contains the data associated with the cells in this spreadsheet.
    /// Cells have a StringForm, a set of contents, and an associated value.
    /// </summary>
    public class Cell
    {
        // StringForm is the string representation of the contents of this object and is what is serialized.
        [JsonInclude]
        public string StringForm { get; internal set; }

        // Contents is the object form of those contents, and is either a string, double, or Formula.
        [JsonIgnore]
        internal object Contents { get; set; }

        // Value is the value associated with those contents after being parsed.
        // Is a string, double, or FormulaError.
        [JsonIgnore]
        internal object Value { get; set; }

        /// <summary>
        /// Creates a new cell for the spreadsheet.
        /// If a cell with this name already exists, it will attempt to override that cell's data.
        /// If a cell with this name does not exist, it will attempt to add it to <paramref name="ss"/>'s cell map.
        /// Otherwise it adds the given cell to the spreadsheet's cell map.
        /// Calls SetContentsOfCell to validate Contents and Value of Cell.
        /// </summary>
        /// <param name="ss">The spreadsheet this cell is being added to.</param>
        /// <param name="StringForm">The string representation of this cell's contents.</param>
        /// <param name="name">This cell's given name.</param>
        [JsonConstructor]
        public Cell(string StringForm)
        {
            this.StringForm = StringForm;
            this.Contents = StringForm;
            this.Value = StringForm;
        }

        /// <summary>
        /// Updates the cell's value based on its contents.
        /// </summary>
        /// <param name="lookup">A lookup delegate that takes a variable name and returns a numeric value.</param>
        internal void UpdateValue(Func<string, double> lookup)
        {
            if (Contents is double d)
            {
                Value = d;
            }
            else if (Contents is Formula f)
            {
                Value = f.Evaluate(lookup);
            }
            else
            {
                Value = Contents;
            }
        }

        /// <summary>
        /// Updates the StringForm of the cell based on its contents.
        /// If contents are a string, saves it literally.
        /// If contents are a double, saves the string form of that double.
        /// If contents are a Formula, saves '=' + Formula.ToString().
        /// </summary>
        internal void UpdateStringForm()
        {
            if (Contents is double d)
            {
                StringForm = d.ToString();
            }
            else if (Contents is string s)
            {
                StringForm = s;
            }
            else if (Contents is Formula f)
            {
                StringForm = "=" + f.ToString();
            }
        }
    }
}

