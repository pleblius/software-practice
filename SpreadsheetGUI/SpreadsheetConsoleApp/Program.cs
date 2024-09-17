using FormulaEvaluator;
using System.Text.RegularExpressions;

namespace SpreadsheetConsoleApp
{
    /// <summary>
    /// A console application to perform function testing for the formula evaluator project.
    /// </summary>
    internal class Program
    {
        
        /// <summary>
        /// Console application entry point.
        /// </summary>
        /// <param name="args">Unused</param>
        static void Main(string[] args)
        {
            args = new string[1];

            Dictionary<string, bool> testResults = new Dictionary<string, bool>();

            Evaluator.Lookup varDel = variableValue;

            string s;

            // Test basic operations
            s = new string("2 + 2");
            int a = 4;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("2 - 1");
            a = 1;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("1 - 2");
            a = -1;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("2 * 3");
            a = 6;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("12/4");
            a = 3;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("13/2");
            a = 6;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(2+2)");
            a = 4;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(2-1)");
            a = 1;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(4*1)");
            a = 4;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(1/4)");
            a = 0;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            // Test Operations with parentheses and external operation

            s = new string("(1 + 1) + 1");
            a = 3;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(6 - 1) + 4");
            a = 9;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(5/3) * 3");
            a = 3;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(1/4) * 8");
            a = 0;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(10/2)/2");
            a = 2;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(20/2)/2");
            a = 5;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(30/3)/1");
            a = 10;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(5 + 2)/3");
            a = 2;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(2*3)*4");
            a = 24;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(3 - 2)/10");
            a = 0;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            // Test complex operations with multiple sets of parentheses

            s = new string("(25/3)/(5/2)");
            a = 4;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(200/3)/(7/4)/(9/4)/(33/3)");
            a = 3;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);


            s = new string("((2 + 3) - 1)*2");
            a = 8;

            s = new string("(((1/4) + 3)/2)*5");
            a = 5;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(((5 * 5) * 2) / ((3 * 4)/6))*3");
            a = 75;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("((8 + 3) / (1 + 1) / (4 + 1)/(4-2))*3");
            a = 0;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("((8 + 3) / (1 + 1)) / ((4 + 1)/(4-2))*3");
            a = 6;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            // Test Variable Lookups

            s = new string("A6 + 2");
            a = 8;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("AA66 + BB2");
            a = 68;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("ABCDEFG6543210 + 2");
            a = 6543212;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(3 * b24)/17");
            a = 4;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("A3 - b2 + c5/d3");
            a = 2;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            s = new string("(((Z16/G4)/(J3 - H1) + I3)/N2) + 1");
            a = 3;
            testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);

            // Test Exceptions
            // Paren Exceptions

            s = new string("(");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("()(");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("(()))");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("(2 + 2) + 2)");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            // Divide by zero exceptions

            s = new string("1/0");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("(3/0)");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("(3 + 2)/(1/3)");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            // Misc. Exceptions

            s = new string("3 3 +");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("3 + + 3");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("3A3 + B9");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("A3A + B9B");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("-");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("1S + 2");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("1/3 - ?");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

            s = new string("4 == 4");
            a = 0;
            try
            {
                testResults.Add(s, Evaluator.Evaluate(s, varDel) == a);
                testResults.Add(s, false);
            }
            catch (ArgumentException)
            {
                testResults.Add(s, true);
            }
            catch (Exception)
            {
                testResults.Add(s, false);
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            s = null;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            a = 0;
            try
            {
#pragma warning disable CS8604 // Possible null reference argument.
                testResults.Add("null", Evaluator.Evaluate(s, varDel) == a);
#pragma warning restore CS8604 // Possible null reference argument.
                testResults.Add("null", false);
            }
            catch (ArgumentException)
            {
                testResults.Add("null", true);
            }
            catch (Exception)
            {
                testResults.Add("null", false);
            }

            Dictionary<string, bool>.KeyCollection keys = testResults.Keys;

            foreach (string str in keys)
            {
                Console.WriteLine(str);
                try
                {
                    Console.WriteLine(Evaluator.Evaluate(str, variableValue));
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Argument Exception");
                }
            }

            double num = 1.0 / 0.0;

            Console.WriteLine("1.0/0.0 = " + num);

            Console.ReadKey();
        }

        
        /// <summary>
        /// A function template to use with the lookup delegate for testing purposes.
        /// </summary>
        /// <param name="name">The variable's name as a string.</param>
        /// <returns>The stored variable value.</returns>
        public static int variableValue(string name)
        {
            String[] s = Regex.Split(name, "[a-z]+", RegexOptions.IgnoreCase);

            return int.Parse(s[1]);
        }
    }
}