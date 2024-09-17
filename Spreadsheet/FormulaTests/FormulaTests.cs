using SpreadsheetUtilities;
using System.Text.RegularExpressions;

namespace FormulaTests;
[TestClass]
public class FormulaTests {

    /**************** Basic Tests *************/

    [TestMethod]
    public void TestSimpleFormulaEquals() {
        Formula f1 = new Formula("3 + 4");
        Formula f2 = new Formula("3 + 4");

        Assert.IsTrue(f1.Equals(f2));
        Assert.IsTrue(f1 == f2);
        Assert.IsFalse(f1 != f2);

        f1 = new Formula("3 + 4");
        f2 = new Formula("4 + 3");

        Assert.IsFalse(f1.Equals(f2));
        Assert.IsFalse(f1 == f2);
        Assert.IsTrue(f1 != f2);
    }

    [TestMethod]
    public void TestUnequalLengthFormulasAreNotEqual()
    {
        Formula f1 = new("1 + 2 + 3");
        Formula f2 = new("1 + 2 + 3 + 4");
        Formula f3 = new("(1 + 2) + 3 + 4");


        Assert.IsFalse(f1.Equals(f2));
        Assert.IsFalse(f1.Equals(f3));
    }

    [TestMethod]
    public void TestFormulasWithDifferentOperatorsAreNotEqual()
    {
        Formula f1 = new("1+3-5");
        Formula f2 = new("1*3/5");

        Assert.AreNotEqual(f1, f2);
    }

    [TestMethod]
    public void TestUnequalVariablesAreNotEqual()
    {
        Formula f1 = new("a1 + b2");
        Formula f2 = new("A1 + B2");

        Assert.IsFalse(f1.Equals(f2));
        Assert.IsFalse(f2.Equals(f1));
    }

    [TestMethod]
    public void TestUnequalDoublesAreNotEqual()
    {
        Formula f1 = new("1.0 + 2.0");
        Formula f2 = new("1.0 + 2.5");

        Assert.AreNotEqual(f1, f2);
        Assert.AreNotEqual(f2, f1);
    }

    [TestMethod]
    public void TestDoubleNotEqualToVariable()
    {
        Formula f1 = new("1.0 + 2.0");
        Formula f2 = new("1.0 + B2");

        Assert.AreNotEqual(f1, f2);
        Assert.AreNotEqual(f2, f1);
    }

    [TestMethod]
    public void TestUnequalNormalizationsAreNotEqual()
    {
        Formula f1 = new("1.0 + a1");
        Formula f2 = new("1.0 + A1");

        Assert.AreNotEqual(f1, f2);
        Assert.AreNotEqual(f2, f1);
    }

    [TestMethod]
    public void TestUnequalVariablesNormalizedToSameAreEqual()
    {
        Func<string, string> norm = (string input) => input.ToUpperInvariant();
        Func<string, bool> valid = (string input) => true;

        Formula f1 = new("a1 + b2", norm, valid);
        Formula f2 = new("A1 + B2", norm, valid);

        Assert.AreEqual(f1, f2);
        Assert.IsTrue(f2.Equals(f1));
    }

    [TestMethod]
    public void TestObjectAsFormulaIsEqual()
    {
        Formula f1 = new("(27 -4)/(10 + 3)");
        Object objFormula = new Formula("(27-4)/(10+3)");
        Object objNotFormula = new string("not a formula");

        Assert.IsTrue(f1.Equals(objFormula));
        Assert.IsTrue(f1 == (Formula) objFormula);
        Assert.IsFalse(f1 != (Formula)objFormula);

        Assert.IsFalse(f1.Equals(objNotFormula));
    }

    [TestMethod]
    public void TestFormulaDoesNotEqualNull()
    {
        Formula f1 = new("1 + 1");
        Object? nullObj = null;

        Assert.IsFalse(f1.Equals(nullObj));
    }

    [TestMethod]
    public void TestNullFormula()
    {
        Formula? f1 = null;
        Formula f2 = new("1");

        Assert.AreNotEqual(f1, f2);
        Assert.AreNotEqual(f2, f1);
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestWhiteSpaceFormula()
    {
        _ = new Formula(" ");
    }

    [TestMethod]
    public void TestSimpleToString()
    {
        Formula f1 = new("1 + 2");

        Assert.AreEqual("1+2", f1.ToString());

        f1 = new("((1*3) + 4) /5");

        Assert.AreEqual("((1*3)+4)/5", f1.ToString());
    }

    [TestMethod]
    public void TestNormalizedToStringResultsInEqualFormulas()
    {
        Func<string, string> normalizer1 = (string input) => input.ToUpperInvariant();
        Func<string, bool> validator = (string input) => Regex.IsMatch(input, "[a-zA-Z][0-9]");

        Formula f1 = new("a1 + B2 + c3 + D4", normalizer1, validator);

        Formula f2 = new(f1.ToString());

        Assert.AreEqual(f1, f2);
        Assert.AreEqual(f2, f1);
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestValidatorThrowsFormulaFormatException()
    {
        Func<string, string> normalizer1 = (string input) => input.ToUpperInvariant();
        Func<string, bool> validator = (string input) => Regex.IsMatch(input, "[a-z][0-9]");

        Formula f1 = new("a1 + b2", normalizer1, validator);
    }

    [TestMethod]
    public void TestHashCodeEquality()
    {
        Formula f1 = new("1 *4 + 5");
        Formula f2 = new("1*4+5 ");
        Formula f3 = new("2+3*6");

        Assert.AreEqual(f1.GetHashCode(), f2.GetHashCode());
        Assert.AreNotEqual(f1.GetHashCode(), f3.GetHashCode());
    }

    [TestMethod]
    public void TestHashCodeEqualityWithNormalizedVariables()
    {
        Func<string, string> normalizer1 = (string input) => input.ToUpperInvariant();
        Func<string, bool> validator = (string input) => Regex.IsMatch(input, "[a-zA-Z][0-9]");

        Formula f1 = new("a5 + c6 / 2 - 5*B3", normalizer1, validator);
        Formula f2 = new("A5 +C6/2 -5*b3", normalizer1, validator);

        Assert.AreEqual(f1.GetHashCode(), f2.GetHashCode());
    }

    /******************************* BAD SYNTAX TESTS ***************************************************************/

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestEmptyExpressionThrows()
    {
        _ = new Formula("");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestWhiteSpaceExpressionThrows()
    {
        _ = new Formula("   ");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void BadOperatorThrows()
    {
        _ = new Formula("1^2");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestBadVariableFormatThrows()
    {
        _ = new Formula("1a + b2");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestConsecutiveOperatorsThrows()
    {
        _ = new Formula("20 + + 15");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestConsecutiveValuesThrows()
    {
        _ = new Formula("(30 + 27) 14 /5");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestConsecutiveVariablesThrows()
    {
        _ = new Formula("a1*b2 c4");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestConsecutiveDoubleVariableThrows()
    {
        _ = new Formula("20 a4");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestConsecutiveVariableDoubleThrows()
    {
        _ = new Formula("a4 20");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestNakedLeadingOperatorThrows()
    {
        _ = new Formula("*40");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestNakedTrailingOperatorThrows()
    {
        _ = new Formula("40+");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestTooManyRightParensThrows()
    {
        _ = new Formula("(((28* 19) /3) +2 ) -20)");
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestTooManyLeftParensThrows()
    {
        _ = new Formula("(((((20 -3) + 8) / (4 + 2)) *10)");
    }

    /************************** EVALUATION TESTS **********************************************/

    [TestMethod]
    public void TestAdditiveEvaluation()
    {
        Func<string, double> lookup = (string input) =>
        {
            String[] s = Regex.Split(input, "[a-z]+", RegexOptions.IgnoreCase);

            return int.Parse(s[1]);
        };

        Assert.AreEqual(10.0, new Formula("1 + 2 + 3 + 4").Evaluate(lookup));
    }

    [TestMethod]
    public void TestEvaluationWithEveryOperator()
    {
        Func<string, double> lookup = (string input) =>
        {
            String[] s = Regex.Split(input, "[a-z]+", RegexOptions.IgnoreCase);

            return int.Parse(s[1]);
        };

        Assert.AreEqual(4.0 / 6.0, new Formula("(1 +3) /(2*(5-2))").Evaluate(lookup));
    }

    [TestMethod]
    public void TestEvaluationWithSimpleParentheses()
    {
        Func<string, double> lookup = (string input) =>
        {
            String[] s = Regex.Split(input, "[a-z]+", RegexOptions.IgnoreCase);

            return int.Parse(s[1]);
        };

        Assert.AreEqual(1.5, new Formula("(1 + 6)/2 - (2 + 4)/3").Evaluate(lookup));
    }

    [TestMethod]
    public void TestScientificMethodEvaluation()
    {
        Func<string, double> lookup = (string input) =>
        {
            String[] s = Regex.Split(input, "[a-z]+", RegexOptions.IgnoreCase);

            return int.Parse(s[1]);
        };

        Assert.AreEqual(1020.25, new Formula("1e3 + 20 + 1/4").Evaluate(lookup));
    }

    [TestMethod]
    public void TestComplexEvaluation()
    {
        Func<string, double> lookup = (string input) =>
        {
            String[] s = Regex.Split(input, "[a-z]+", RegexOptions.IgnoreCase);

            return int.Parse(s[1]);
        };

        Assert.AreEqual(16.0, new Formula("(((3*5*4)/15)*((6/3)/2)*2)*2").Evaluate(lookup));
    }

    [TestMethod]
    public void TestZeroDividedByZero()
    {
        Func<string, double> lookup = (string input) =>
        {
            String[] s = Regex.Split(input, "[a-z]+", RegexOptions.IgnoreCase);

            if (s.Length != 2)
            {
                throw new ArgumentException();
            }

            return int.Parse(s[1]);
        };

        Formula f1 = new("0/0");

        string msg = new($"Formula resulted in 0 divided by 0. " +
                        "Please check expression and variable values.");

        FormulaError fe = (FormulaError) f1.Evaluate(lookup);

        Assert.IsNotNull(fe.Reason);
        Assert.AreEqual(msg, fe.Reason);
    }

    [TestMethod]
    public void TestComplexZeroDividedByZero()
    {
        Func<string, double> lookup = (string input) =>
        {
            String[] s = Regex.Split(input, "[a-z]+", RegexOptions.IgnoreCase);

            if (s.Length != 2)
            {
                throw new ArgumentException();
            }

            return int.Parse(s[1]);
        };

        Formula f1 = new("(5 - (3 +2))/((2*3 - 7) + 1)");

        string msg = new($"Formula resulted in 0 divided by 0. " +
                        "Please check expression and variable values.");

        FormulaError fe = (FormulaError) f1.Evaluate(lookup);

        Assert.IsNotNull(fe.Reason);
        Assert.AreEqual(msg, fe.Reason);
    }

    [TestMethod]
    public void TestSimplePositiveInfiniteResult()
    {
        Func<string, double> lookup = (string input) =>
        {
            String[] s = Regex.Split(input, "[a-z]+", RegexOptions.IgnoreCase);

            if (s.Length != 2)
            {
                throw new ArgumentException();
            }

            return int.Parse(s[1]);
        };

        Formula f1 = new("(5+3)/0");

        string msg = new($"Formula resulted in positive division by 0. " +
                        "Please check expression and variable values.");

        FormulaError fe = (FormulaError) f1.Evaluate(lookup);

        Assert.IsNotNull(fe.Reason);
        Assert.AreEqual(msg, fe.Reason);
    }

    [TestMethod]
    public void TestSimpleNegativeInfiniteResult()
    {
        Func<string, double> lookup = (string input) =>
        {
            String[] s = Regex.Split(input, "[a-z]+", RegexOptions.IgnoreCase);

            if (s.Length != 2)
            {
                throw new ArgumentException();
            }

            return int.Parse(s[1]);
        };

        Formula f1 = new("(3 -5)/0");

        string msg = new($"Formula resulted in negative division by 0. " +
                        "Please check expression and variable values.");

        FormulaError fe = (FormulaError) f1.Evaluate(lookup);

        Assert.IsNotNull(fe.Reason);
        Assert.AreEqual(msg, fe.Reason);
    }

    /************************ VARIABLE TESTS *******************************/

    [TestMethod]
    public void TestGetVariables()
    {
        Formula f1 = new("a1 + b2 + c3 + d4");

        var list = new List<string>() { "a1", "b2", "c3", "d4" };

        List<string> varList = new(f1.GetVariables());

        Assert.AreEqual(list.Count, varList!.Count);

        for (int i = 0; i < list.Count; i++)
        {
            Assert.AreEqual(list[i], varList[i]);
        }
    }

    [TestMethod]
    [ExpectedException(typeof(FormulaFormatException))]
    public void TestValidatorFails()
    {
        Func<string, string> normalizer = (string input) => input;
        Func<string, bool> validator = (string input) => input == "a5";

        _ = new Formula("1 + A5", normalizer, validator);
    }

    [TestMethod]
    public void TestComplexEvaluationWithVariables()
    {
        Func<string, string> normalizer = (string input) => input;
        Func<string, bool> validator = (string input) => true;
        Formula f1 = new("(A4*3)/2.5 + (B5 + C7)/D6", normalizer, validator);

        Func<string, double> lookup = (string input) =>
        {
            String[] s = Regex.Split(input, "[a-z]+", RegexOptions.IgnoreCase);

            return int.Parse(s[1]);
        };

        Assert.AreEqual(6.8, f1.Evaluate(lookup));
    }

    [TestMethod]
    public void TestFailedLookupReturnsFormulaError()
    {
        Func<string, double> lookup = (string input) =>
        {
            String[] s = Regex.Split(input, "[a-z]+", RegexOptions.IgnoreCase);

            if (s.Length != 2)
            {
                throw new ArgumentException();
            }

            return int.Parse(s[1]);
        };

        Formula f1 = new("1 + _1");

        string msg = new($"Failed get to get lookup value for variable _1. " +
                        $"Double-check variable name and assignment.");

        FormulaError fe = (FormulaError)f1.Evaluate(lookup);

        Assert.IsNotNull(fe.Reason);
        Assert.AreEqual(msg, fe.Reason);
    }
}
