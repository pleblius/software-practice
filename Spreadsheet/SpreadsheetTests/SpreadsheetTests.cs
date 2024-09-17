using NuGet.Frameworks;
using SpreadsheetUtilities;
using SS;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SpreadsheetTests;

[TestClass]
public class SpreadsheetTests
{
    // ARCHIVED TESTS FOR PS4

    /******************** Simple Construction Tests ***********************/

    [TestMethod]
    public void TestEmptyConstructorHasNoCells()
    {
        Spreadsheet ss = new();

        // Test that no cells have been added.
        Assert.AreEqual(0, ss.GetNamesOfAllNonemptyCells().Count());
        Assert.AreEqual("", ss.GetCellContents("A1"));
    }

    [TestMethod]
    public void TestSetContentsDoubleCreatesCell()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A1", "0.5");

        // Test that a single cell with a contents of 0.5 has been created.
        Assert.AreEqual(1, ss.GetNamesOfAllNonemptyCells().Count());
        Assert.AreEqual("A1", ss.GetNamesOfAllNonemptyCells().First());
        Assert.AreEqual(0.5, ss.GetCellContents("A1"));
    }

    [TestMethod]
    public void TestSetContentsStringCreatesCell()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A1", "testString");

        // Test that a single cell with the contents "testString" has been created.
        Assert.AreEqual(1, ss.GetNamesOfAllNonemptyCells().Count());
        Assert.AreEqual("A1", ss.GetNamesOfAllNonemptyCells().First());
        Assert.AreEqual("testString", ss.GetCellContents("A1"));
    }

    [TestMethod]
    public void TestSetContentsFormulaCreatesCell()
    {
        Spreadsheet ss = new();
        string f1 = new("=1 + 1 + 1");

        ss.SetContentsOfCell("A1", f1);

        // Test that a single cell with the Formula object "1+1+1" has been created.
        Assert.AreEqual(1, ss.GetNamesOfAllNonemptyCells().Count());
        Assert.AreEqual("A1", ss.GetNamesOfAllNonemptyCells().First());
        Assert.AreEqual(new Formula("1+1+1"), ss.GetCellContents("A1"));
    }

    [TestMethod]
    public void TestChangeContentsGetsCorrectContents()
    {
        Spreadsheet ss = new();

        // Test the GetContents method for cells with a double, string, and Formula contents.

        ss.SetContentsOfCell("A1", "5.5");
        Assert.AreEqual(5.5, ss.GetCellContents("A1"));

        ss.SetContentsOfCell("A1", "new string");
        Assert.AreEqual("new string", ss.GetCellContents("A1"));

        ss.SetContentsOfCell("A1", "=A2 + B2");
        Assert.AreEqual("A2+B2", ss.GetCellContents("A1").ToString());
    }

    [TestMethod]
    public void TestAddEmptyStringToCellDeletesCell()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A1", "5");
        ss.SetContentsOfCell("A1", "");

        // Test that the extant cell has been deleted and only returns an empty string.
        Assert.AreEqual(0, ss.GetNamesOfAllNonemptyCells().Count());
        Assert.AreEqual("", ss.GetCellContents("A1"));
    }

    /******************* Circular Exceptions *****************/

    [TestMethod]
    [ExpectedException(typeof(CircularException))]
    public void TestDirectCircularDependencyThrows()
    {
        Spreadsheet ss = new();

        // Self-referential circular dependency
        ss.SetContentsOfCell("A1", "=A1");
    }

    [TestMethod]
    [ExpectedException(typeof(CircularException))]
    public void TestTwoDeepCircularDependencyThrows()
    {
        Spreadsheet ss = new();

        // Two-layer circular dependency
        ss.SetContentsOfCell("A1", "=2*B2");
        ss.SetContentsOfCell("B2", "=A1 + 4");
    }

    [TestMethod]
    [ExpectedException(typeof(CircularException))]
    public void TestRoundaboutCircularDependencyThrows()
    {
        Spreadsheet ss = new();

        // Multiple layer circular dependency
        ss.SetContentsOfCell("A1", "=B1");
        ss.SetContentsOfCell("B1", "=C1 + D1");
        ss.SetContentsOfCell("C1", "=D1");
        ss.SetContentsOfCell("D1", "=F1");
        ss.SetContentsOfCell("F1", "=B1");
    }

    [TestMethod]
    public void TestCircularFormulaCausesNoChanges()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A1", "=B2");

        try
        {
            ss.SetContentsOfCell("B2", "=A1");
        }
        catch (CircularException) { }

        // Test that, after exception is thrown, B2 is empty and A1 is unchanged.
        Assert.AreEqual("B2", ss.GetCellContents("A1").ToString());
        Assert.AreEqual(1, ss.GetNamesOfAllNonemptyCells().Count());
        Assert.AreEqual("", ss.GetCellContents("B2"));
    }

    /****************** Invalid Name Exceptions ***************/

    [TestMethod]
    [ExpectedException(typeof(InvalidNameException))]
    public void TestGetBadNameThrows()
    {
        Spreadsheet ss = new();

        ss.GetCellContents("1A");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidNameException))]
    public void TestSetDoubleBadNameThrows()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("1A", "5.5");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidNameException))]
    public void TestSetStringBadNameThrows()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("1A", "A1");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidNameException))]
    public void TestSetFormulaBadNameThrows()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("1A", "=5.0 + A2");
    }

    /******************* Set Content List Tests **********************/

    [TestMethod]
    public void TestSetDoubleGetsCorrectList()
    {
        Spreadsheet ss = new();
        List<string> nameList = new List<string> { "D4" };

        ss.SetContentsOfCell("A1", "5.5");
        ss.SetContentsOfCell("B2", "1.2");
        ss.SetContentsOfCell("C3", "1.7e15");

        // No dependency, should only return self.
        List<string> list = new List<string>(ss.SetContentsOfCell("D4", "0"));

        Assert.AreEqual(nameList.Count, list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            Assert.AreEqual(nameList[i], list[i]);
        }
    }

    [TestMethod]
    public void TestSetStringGetsCorrectList()
    {
        Spreadsheet ss = new();
        List<string> nameList = new List<string> { "D4", "E5" };

        ss.SetContentsOfCell("A1", "abc");
        ss.SetContentsOfCell("B2", "def");
        ss.SetContentsOfCell("C3", "ghi");

        ss.SetContentsOfCell("E5", "=D4");

        // No dependency, should only return self.
        List<string> list = new List<string>(ss.SetContentsOfCell("D4", "jkl"));

        Assert.AreEqual(nameList.Count, list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            Assert.AreEqual(nameList[i], list[i]);
        }
    }

    [TestMethod]
    public void TestSetFormulaGetsCorrectList()
    {
        Spreadsheet ss = new();
        List<string> nameList = new List<string> { "A1", "C3", "D4" };

        ss.SetContentsOfCell("A1", "=B1 + C1");
        ss.SetContentsOfCell("B2", "=3*1e9/C2");
        ss.SetContentsOfCell("C3", "=A1 + B2 *(3+2)/A1");
        ss.SetContentsOfCell("D4", "=C3");

        // Changed dependency, should return dependents: C3 and D4
        List<string> list = new List<string>(ss.SetContentsOfCell("A1", "C2 + 2"));

        Assert.AreEqual(nameList.Count, list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            Assert.AreEqual(nameList[i], list[i]);
        }
    }

    [TestMethod]
    public void TestChangingFormulaUpdatesDependencies()
    {
        Spreadsheet ss = new();
        List<string> nameList = new List<string> { "C3", "D4" };

        ss.SetContentsOfCell("A1", "B1 + C1");
        ss.SetContentsOfCell("B2", "=3*1e9/C2");
        ss.SetContentsOfCell("C3", "=A1 + B2 *(3+2)/A1");
        ss.SetContentsOfCell("D4", "=C3");

        List<string> list = new List<string>(ss.SetContentsOfCell("C3", "text"));

        Assert.AreEqual(nameList.Count, list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            Assert.AreEqual(nameList[i], list[i]);
        }

        // After removing formula from D4, it should no longer rely on C3.
        ss.SetContentsOfCell("D4", "1");

        list = new List<string>(ss.SetContentsOfCell("C3", "5"));
        nameList = new List<string> { "C3" };

        Assert.AreEqual(nameList.Count, list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            Assert.AreEqual(nameList[i], list[i]);
        }
    }

    [TestMethod]
    public void StressTest()
    {
        Spreadsheet ss = new();
        List<string> variableList = new();

        for (int i = 0; i < 5000; i++)
        {
            variableList.Add("A" + i);
        }

        ss.SetContentsOfCell("A0", "5.0");

        // Add a bunch of variables that depend on each other.

        for (int i = 1; i < variableList.Count; i++)
        {
            ss.SetContentsOfCell(variableList[i], $"=A{i - 1}");
        }

        // Update parent of all dependents and check that all other cells are returned.
        List<string> depList = new(ss.SetContentsOfCell("A0", "1.0"));

        Assert.AreEqual(variableList.Count, ss.GetNamesOfAllNonemptyCells().Count());

        for (int i = 0; i < variableList.Count; i++)
        {
            Assert.AreEqual(variableList[i], depList[i]);
        }
    }

    /*********************** PS5 tests ***********************/

    [TestMethod]
    public void TestDefaultConstructorPopulatesFields()
    {
        Spreadsheet ss = new();

        Assert.AreEqual("default", ss.Version);

        // Check normalizer and validator

        _ = ss.SetContentsOfCell("a1", "1.0");
        _ = ss.SetContentsOfCell("A1", "2.0");

        Assert.AreEqual(1.0, ss.GetCellContents("a1"));
        Assert.AreEqual(2.0, ss.GetCellContents("A1"));
    }

    [TestMethod]
    public void TestThreeParameterConstructorPopulatesNormalizer()
    {
        Spreadsheet ss = new(s => true, s => s.ToUpper(), "1.0");

        _ = ss.SetContentsOfCell("a1", "=a2 + b2");

        Assert.AreEqual(new Formula("A2 + B2"), ss.GetCellContents("A1"));
        Assert.AreNotEqual(new Formula("a2 + b2"), ss.GetCellContents("a1"));

        Assert.IsTrue(ss.GetNamesOfAllNonemptyCells().Contains<string>("A1"));
        Assert.IsFalse(ss.GetNamesOfAllNonemptyCells().Contains<string>("a1"));
    }

    [TestMethod]
    public void TestThreeParamConstructorPopulatesValidator()
    {
        Spreadsheet ss = new(s => false, s => s, "1.0");

        Assert.ThrowsException<InvalidNameException>(() => _ = ss.SetContentsOfCell("a1", "1"));
    }

    [TestMethod]
    public void TestThreeParamConstructorCreatesEmptySpreadsheet()
    {
        Spreadsheet ss = new(s => true, s => s, "1.0");

        Assert.AreEqual(0, ss.GetNamesOfAllNonemptyCells().Count());
    }

    [TestMethod]
    public void TestFourParameterConstructorPopulatesNormalizer()
    {
        // Create spreadsheet, save it, then reload it with normalizer
        Spreadsheet ss = new();

        ss.Save("test.txt");

        Spreadsheet loaded = new("test.txt", s => true, s => s.ToUpper(), "default");

        loaded.SetContentsOfCell("a1", "1.0");

        // Check if cell name is capitalized
        Assert.IsTrue(loaded.GetNamesOfAllNonemptyCells().Contains<string>("A1"));

        Assert.IsFalse(loaded.GetNamesOfAllNonemptyCells().Contains<string>("a1"));
    }

    [TestMethod]
    public void TestFourParameterConstructorPopulatesValidator()
    {
        // Create spreadsheet, save it, then reload it with validator
        Spreadsheet ss = new();

        ss.Save("test.txt");

        Spreadsheet loaded = new("test.txt", s => s.StartsWith("A"), s => s, "default");

        Assert.ThrowsException<InvalidNameException>(() => loaded.SetContentsOfCell("B1", "1.0"));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidNameException))]
    public void TestFourParamConstructorPopulatesValidator()
    {
        throw new InvalidNameException();
    }

    // Invalid name tests

    [TestMethod]
    [ExpectedException(typeof(InvalidNameException))]
    public void TestGetCellContentsIllegalNameThrows()
    {
        Spreadsheet ss = new();

        ss.GetCellContents("1A");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidNameException))]
    public void TestGetCellValueIllegalNameThrows()
    {
        Spreadsheet ss = new();

        ss.GetCellValue("1A");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidNameException))]
    public void TestSetContentsOfCellIllegalNameThrows()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("1A", "1");
    }

    // List Order Tests

    [TestMethod]
    public void TestOutputListIsInDependencyOrder()
    {
        Spreadsheet ss = new();
        List<string> depList;

        // Single List
        depList = ss.SetContentsOfCell("A1", "=B1").ToList<string>();

        Assert.AreEqual(1, depList.Count);
        Assert.AreEqual("A1", depList[0]);

        // Should have B1, A1 as dependent
        depList = ss.SetContentsOfCell("B1", "=C1").ToList<string>();
        Assert.AreEqual(2, depList.Count);
        Assert.AreEqual("B1", depList[0]);
        Assert.AreEqual("A1", depList[1]);

        // Should have C1, B1, A1 as dependents in that order
        depList = ss.SetContentsOfCell("C1", "=D1 + E1").ToList<string>();
        Assert.AreEqual(3, depList.Count);
        Assert.AreEqual("C1", depList[0]);
        Assert.AreEqual("B1", depList[1]);
        Assert.AreEqual("A1", depList[2]);

        // Should have E1, C1, B1, A1
        depList = ss.SetContentsOfCell("E1", "=F1").ToList<string>();
        Assert.AreEqual(4, depList.Count);
        Assert.AreEqual("E1", depList[0]);
        Assert.AreEqual("C1", depList[1]);
        Assert.AreEqual("B1", depList[2]);
        Assert.AreEqual("A1", depList[3]);

        // Create branch
        _ = ss.SetContentsOfCell("D1", "=F1");

        // Should go F1, E1 OR D1, D1 OR E1, C1, B1, A1
        depList = ss.SetContentsOfCell("F1", "1.0").ToList<string>();
        Assert.AreEqual(6, depList.Count);
        Assert.AreEqual("F1", depList[0]);
        Assert.IsTrue(depList[1] is "D1" or "E1");
        Assert.IsTrue(depList[2] is "D1" or "E1");
        Assert.AreEqual("C1", depList[3]);
        Assert.AreEqual("B1", depList[4]);
        Assert.AreEqual("A1", depList[5]);
    }

    // Value Tests

    [TestMethod]
    public void TestGetEmptyCellValue()
    {
        Spreadsheet ss = new();

        Assert.AreEqual("", ss.GetCellValue("A1"));
    }

    [TestMethod]
    public void TestGetCellValueDouble()
    {
        Spreadsheet ss = new();

        _ = ss.SetContentsOfCell("A1", "1.0");

        Assert.AreEqual(1.0, ss.GetCellValue("A1"));
    }

    [TestMethod]
    public void TestGetCellValueString()
    {
        Spreadsheet ss = new();

        _ = ss.SetContentsOfCell("A1", "string");

        Assert.AreEqual("string", ss.GetCellValue("A1"));
    }

    [TestMethod]
    public void TestGetCellValueFormula()
    {
        Spreadsheet ss = new();

        _ = ss.SetContentsOfCell("B1", "1");
        _ = ss.SetContentsOfCell("A1", "=B1");

        // Value should be value of B1

        Assert.AreEqual(1.0, ss.GetCellValue("A1"));
    }

    [TestMethod]
    public void TestUpdateDependeeUpdatesDependents()
    {
        Spreadsheet ss = new();
        ss.SetContentsOfCell("A1", "=B1 + 2.0");
        ss.SetContentsOfCell("B1", "1.0");

        Assert.AreEqual(3.0, ss.GetCellValue("A1"));
    }

    [TestMethod]
    public void TestComplicatedUpdateDependeeUpdatesDependents()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A1", "=B1 + 2.0");
        ss.SetContentsOfCell("B1", "=C1");
        ss.SetContentsOfCell("C1", "=D1");
        ss.SetContentsOfCell("D1", "=4.5");

        Assert.AreEqual(6.5, ss.GetCellValue("A1"));
        Assert.AreEqual(4.5, ss.GetCellValue("B1"));
    }

    [TestMethod]
    public void TestCellValueGetsFormulaErrorWithMissingDependee()
    {
        Spreadsheet ss = new();

        // B1 has no value
        _ = ss.SetContentsOfCell("A1", "=B1");

        Assert.IsInstanceOfType(ss.GetCellValue("A1"), typeof(FormulaError));
    }

    [TestMethod]
    public void TestCellValueGetsFormulaErrorWithBadVariableValue()
    {
        Spreadsheet ss = new();

        // B1 value is not a number
        _ = ss.SetContentsOfCell("A1", "=B1");
        _ = ss.SetContentsOfCell("B1", "text");

        Assert.IsInstanceOfType(ss.GetCellValue("A1"), typeof(FormulaError));
    }

    [TestMethod]
    public void TestCellValueGetsFormulaErrorWithFormulaErrorValue()
    {
        Spreadsheet ss = new();

        // B1 is a FormulaError for dividing by 0
        _ = ss.SetContentsOfCell("A1", "=B1");
        _ = ss.SetContentsOfCell("B1", "=1/0");

        Assert.IsInstanceOfType(ss.GetCellValue("A1"), typeof(FormulaError));
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestBadFormulaFormatThrows()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A1", "1.0");

        ss.SetContentsOfCell("A1", "=2 + + 3");
    }

    // Changed Property Tests

    [TestMethod]
    public void TestChangedValueUpdatesOnChange()
    {
        Spreadsheet ss = new();

        Assert.IsFalse(ss.Changed);

        // Change spreadsheet

        _ = ss.SetContentsOfCell("A1", "1.0");

        Assert.IsTrue(ss.Changed);
    }

    [TestMethod]
    public void TestChangedValueResetsOnSave()
    {
        Spreadsheet ss = new();

        Assert.IsFalse(ss.Changed);

        // Change spreadsheet

        _ = ss.SetContentsOfCell("A1", "1.0");

        Assert.IsTrue(ss.Changed);

        // Save spreadsheet

        ss.Save("test.txt");

        Assert.IsFalse(ss.Changed);
    }

    // Serialization Tests

    [TestMethod]
    public void TestDeserializationWithNonDefaultVersionLoads()
    {
        Spreadsheet ss = new(s => true, s => s, "V2.1");

        _ = ss.SetContentsOfCell("A1", "4.5");

        ss.Save("test.txt");

        Spreadsheet loaded = new("test.txt", s => true, s => s, "V2.1");

        // Check version and value
        Assert.AreEqual(ss.Version, loaded.Version);

        Assert.AreEqual(4.5, (double) loaded.GetCellValue("A1"), .0001);
    }

    [TestMethod]
    public void TestDeserializeSingleCellJSONCreatesCorrectSpreadsheetObject()
    {
        // Create new spreadsheet with single cell
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A1", "1.2");

        // Save and reload

        ss.Save("test.txt");

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");

        // Check version and value of A1
        Assert.AreEqual(1.2, loaded.GetCellValue("A1"));
        Assert.AreEqual("default", loaded.Version);
    }

    [TestMethod]
    public void TestDeserializeMultipleCellJSONCreatesCorrectSpreadsheetObject()
    {
        // Create new spreadsheet with single cell
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A1", "1.2");
        ss.SetContentsOfCell("B1", "=A1");
        ss.SetContentsOfCell("C1", "=B1");
        ss.SetContentsOfCell("D1", "=C1");

        List<string> nameList = new() { "A1", "B1", "C1", "D1" };
        // Save and reload

        ss.Save("test.txt");

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");

        // Check version, size, and all cell values and names
        Assert.AreEqual("default", loaded.Version);

        Assert.AreEqual(4, loaded.GetNamesOfAllNonemptyCells().Count());

        foreach (string s in nameList)
        {
            Assert.IsTrue(loaded.GetNamesOfAllNonemptyCells().Contains<string>(s));
        }

        foreach (string s in loaded.GetNamesOfAllNonemptyCells().ToList<string>())
        {
            Assert.AreEqual(1.2, loaded.GetCellValue(s));
        }
    }

    [TestMethod]
    public void TestLoadFormulaeUpdatesAllValues()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A1", "1.2");
        ss.SetContentsOfCell("B1", "=A1");
        ss.SetContentsOfCell("C1", "=B1");
        ss.SetContentsOfCell("D1", "=C1");

        ss.Save("test.txt");

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");

        foreach (string s in ss.GetNamesOfAllNonemptyCells())
        {
            Assert.AreEqual(ss.GetCellValue(s), loaded.GetCellValue(s));
        }
    }

    // Save Tests

    [TestMethod]
    public void TestLoadEmptySpreadsheetCreatesEmptySpreadsheet()
    {
        Spreadsheet ss = new();

        // Save and load empty sheet
        ss.Save("emptyTest.txt");

        Spreadsheet loaded = new("emptyTest.txt", s => true, s => s, "default");

        Assert.AreEqual(0, loaded.GetNamesOfAllNonemptyCells().ToList<string>().Count);
    }

    // Load Exception Tests

    [TestMethod]
    [ExpectedException(typeof(SpreadsheetReadWriteException))]
    public void TestLoadNonExistentFileThrows()
    {
        // Delete file
        if (File.Exists("test.txt"))
        {
            File.Delete("test.txt");
        }

        Spreadsheet ss = new("test.txt", s => true, s => s, "default");
    }

    [TestMethod]
    [ExpectedException(typeof(SpreadsheetReadWriteException))]
    public void TestLoadFileWithImproperFormatThrows()
    {
        using (StreamWriter sw = File.CreateText("test.txt"))
        {
            sw.WriteLine("This is not a spreadsheet.");
        }

        Spreadsheet ss = new("test.txt", s => true, s => s, "default");
    }

    [TestMethod]
    [ExpectedException(typeof(SpreadsheetReadWriteException))]
    public void TestSaveToBadPathThrows()
    {
        Spreadsheet ss = new();

        ss.Save("/badpath/.txt");
    }

    [TestMethod]
    [ExpectedException(typeof(SpreadsheetReadWriteException))]
    public void TestLoadEmptyFileThrows()
    {
        // Make sure file is empty

        if (File.Exists("test.txt"))
        {
            File.Delete("test.txt");
        }
        using (_ = File.Create("test.txt"))
        {

        }

        Spreadsheet ss = new("test.txt", s => true, s => s, "default");
    }

    [TestMethod]
    [ExpectedException(typeof(SpreadsheetReadWriteException))]
    public void TestNullFileThrows()
    {
        // Make sure file is empty

        if (File.Exists("test.txt"))
        {
            File.Delete("test.txt");
        }
        using (StreamWriter sw = File.CreateText("test.txt"))
        {
            sw.Write("null");
        }

        Spreadsheet ss = new("test.txt", s => true, s => s, "default");
    }

    [TestMethod]
    [ExpectedException(typeof(SpreadsheetReadWriteException))]
    public void TestIncompatibleLoadedVersionThrows()
    {
        Spreadsheet ss = new(s => true, s => s, "1.0");
        ss.Save("test.txt");

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");
    }

    [TestMethod]
    [ExpectedException(typeof(SpreadsheetReadWriteException))]
    public void TestLoadBadFormulaFormatThrows()
    {
        Spreadsheet ss = new();
        _ = ss.SetContentsOfCell("A1", "=1 + 1");

        ss.Save("test.txt");
        string str = File.ReadAllText("test.txt");

        str = str.Replace("1+1", "1++1");

        using (StreamWriter sw = File.CreateText("test.txt"))
        {
            sw.Write(str);
        }

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");
    }

    [TestMethod]
    [ExpectedException(typeof(SpreadsheetReadWriteException))]
    public void TestLoadCircularFormulaThrows()
    {
        Spreadsheet ss = new();
        _ = ss.SetContentsOfCell("A1", "=B1");
        _ = ss.SetContentsOfCell("B1", "=C1");

        ss.Save("test.txt");
        string str = File.ReadAllText("test.txt");

        str = str.Replace("C1", "A1");

        using (StreamWriter sw = File.CreateText("test.txt"))
        {
            sw.Write(str);
        }

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");
    }

    [TestMethod]
    [ExpectedException(typeof(SpreadsheetReadWriteException))]
    public void TestLoadMissingCellNameThrows()
    {
        Spreadsheet ss = new();
        _ = ss.SetContentsOfCell("A1", "=1 + 1");

        ss.Save("test.txt");
        string str = File.ReadAllText("test.txt");

        str = str.Replace("A1", "");

        using (StreamWriter sw = File.CreateText("test.txt"))
        {
            sw.Write(str);
        }

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");
    }

    [TestMethod]
    [ExpectedException(typeof(SpreadsheetReadWriteException))]
    public void TestLoadBadCellNameThrows()
    {
        Spreadsheet ss = new();
        _ = ss.SetContentsOfCell("A1", "=1 + 1");

        ss.Save("test.txt");
        string str = File.ReadAllText("test.txt");

        str = str.Replace("A1", "1A");

        using (StreamWriter sw = File.CreateText("test.txt"))
        {
            sw.Write(str);
        }

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");
    }

    [TestMethod]
    [ExpectedException(typeof(SpreadsheetReadWriteException))]
    public void TestLoadNoVersionThrows()
    {
        Spreadsheet ss = new();
        _ = ss.SetContentsOfCell("A1", "=1 + 1");

        ss.Save("test.txt");
        string str = File.ReadAllText("test.txt");

        str = str.Replace("default", "");

        using (StreamWriter sw = File.CreateText("test.txt"))
        {
            sw.Write(str);
        }

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");
    }

    [TestMethod]
    public void ChangedShouldBeFalseAfterLoad()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A1", "asdf");

        ss.Save("test.txt");

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");

        Assert.AreEqual(false, loaded.Changed);
    }

    [TestMethod]
    public void StressTest1()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A0", "1.0");

        for (int i = 1; i < 5000; i++)
        {
            ss.SetContentsOfCell($"A{i}", $"=A{i - 1} + 1");
        }

        for (int i = 0; i < 5000; i++)
        {
            Assert.AreEqual(i + 1.0, ss.GetCellValue($"A{i}"));
        }
    }

    [TestMethod]
    public void StressTest2()
    {
        Spreadsheet ss = new();

        for (int i = 1; i < 5000 ; i++)
        {
            ss.SetContentsOfCell($"A{i}", $"=A{i - 1} + 1");
        }

        ss.SetContentsOfCell("A0", "1.0");

        ss.Save("test.txt");

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");

        for (int i = 0; i < 5000; i++)
        {
            Assert.AreEqual(i + 1.0, loaded.GetCellValue($"A{i}"));
        }
    }

    [TestMethod]
    public void TestSpreadsheetLoadingEmptyCellHasCellAsEmpty()
    {
        Spreadsheet ss = new();

        ss.SetContentsOfCell("A1", "0.0");
        ss.SetContentsOfCell("B1", "1.0");
        ss.SetContentsOfCell("C1", "2.0");
        ss.SetContentsOfCell("D1", "3.0");
        ss.SetContentsOfCell("E1", "4.0");
        ss.SetContentsOfCell("F1", "5.0");
        ss.SetContentsOfCell("G1", "6.0");

        ss.Save("test.txt");

        string text = File.ReadAllText("test.txt");

        using (StreamWriter sw = File.CreateText("test.txt"))
        {
            sw.WriteLine(text.Replace(double.Parse("6.0").ToString(), ""));
        }

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");

        Assert.IsFalse(loaded.GetNamesOfAllNonemptyCells().Contains("G1"));
    }

    [TestMethod]
    public void MultipleDependencyLayersStressTest()
    {
        Spreadsheet ss = new();

        for (int i = 1; i < 1000; i++)
        {
            ss.SetContentsOfCell($"A{i}", $"=A{i - 1} + 1");
        }

        StringBuilder sb = new("=A0");

        for (int i = 1; i < 1000; i++)
        {
            sb.Append($"+A{i}");
        }

        for (int i = 1; i < 100; i++)
        {
            ss.SetContentsOfCell($"B{i}", sb.ToString());
        }

        ss.SetContentsOfCell("A0", "1.0");

        ss.Save("test.txt");

        Spreadsheet loaded = new("test.txt", s => true, s => s, "default");

        for (int i = 0; i < 1000; i++)
        {
            Assert.AreEqual(i + 1.0, loaded.GetCellValue($"A{i}"));
        }
    }
}