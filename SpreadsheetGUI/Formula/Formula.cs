// Skeleton written by Profs Zachary, Kopta and Martin for CS 3500
// Read the entire skeleton carefully and completely before you
// do anything else!
// Last updated: August 2023 (small tweak to API)

using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetUtilities;

/// <summary>
/// Represents formulas written in standard infix notation using standard precedence
/// rules.  The allowed symbols are non-negative numbers written using double-precision
/// floating-point syntax (without unary preceeding '-' or '+');
/// variables that consist of a letter or underscore followed by
/// zero or more letters, underscores, or digits; parentheses; and the four operator
/// symbols +, -, *, and /.
///
/// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
/// a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable;
/// and "x 23" consists of a variable "x" and a number "23".
///
/// Associated with every formula are two delegates: a normalizer and a validator.  The
/// normalizer is used to convert variables into a canonical form. The validator is used to
/// add extra restrictions on the validity of a variable, beyond the base condition that
/// variables must always be legal: they must consist of a letter or underscore followed
/// by zero or more letters, underscores, or digits.
/// Their use is described in detail in the constructor and method comments.
/// </summary>
public class Formula
{
    // Stores the tokens as a list of strings to be evaluated later
    private readonly List<string> tokens;

    /// <summary>
    /// Creates a Formula from a string that consists of an infix expression written as
    /// described in the class comment.  If the expression is syntactically invalid,
    /// throws a FormulaFormatException with an explanatory Message.
    /// No additional input normalization or validation will be performed with this constructor.
    /// </summary>
    /// <param name="formula">The string to be parsed into an infix formula.</param>
    /// <exception cref="FormulaFormatException">If input string is invalid infix syntax.</exception>"
    public Formula(string formula) :
        this(formula, s => s, s => true) 
    { }

    /// <summary>
    /// Creates a Formula from a string that consists of an infix expression written as
    /// described in the class comment.  If the expression is syntactically incorrect,
    /// throws a FormulaFormatException with an explanatory Message.
    ///
    /// Throws a FormulaFormatException if the formula contains a variable v 
    /// such that normalize(v) is not a legal variable.
    ///
    /// Throws a FormulaFormatException if the formula contains a variable v 
    /// such that isValid(normalize(v)) is false,
    ///
    /// Suppose that N is a normalizer that converts all the letters in a string to upper case, and
    /// that V is a validator that returns true only if a string consists of one letter followed
    /// by one digit.  Then:
    /// <list type="bullet">
    /// <item>new Formula("x2+y3", N, V) will run properly.</item>
    /// <item>new Formula("x+y3", N, V) should throw a FormulaFormatException, since V(N("x")) is false.</item>
    /// <item>new Formula("2x+y3", N, V) should throw a FormulaFormatException, since "2x+y3" is invalid syntax.</item>
    /// </list>
    /// </summary>
    /// <param name="formula">The string to be parsed into an infix formula.</param>
    /// <param name="normalize">A normalizer delegate to normalize the variable tokens in the formula to an expected format.</param>
    /// <param name="isValid">A validator delegate to validate each normalized variable token.</param>
    /// <exception cref="FormulaFormatException">A FormulaFormatException if the infix expression contains invalid syntax.</exception>
    public Formula(string formula, Func<string, string> normalize, Func<string, bool> isValid)
    {
        tokens = new List<string>();

        // Parse each token
        foreach (string token in Formula.GetTokens(formula))
        {
            if (IsValidToken(token, normalize, isValid, out string str))
            {
                tokens.Add(str);
            }
            else
            {
                throw new FormulaFormatException($"'{token}' is invalid syntax.");
            }
        }

        // Check overall formula for proper infix syntax. VerifyFormula throws a FFE exception if syntax is invalid.
        try
        {
            VerifyFormula(tokens);
        }
        catch (FormulaFormatException e)
        {
            throw e;
        }
    }

    /// <summary>
    /// Checks if the provided token string is valid syntax for an infix expression.
    /// </summary>
    /// <param name="token">The token being checked.</param>
    /// <param name="str">The normalized string expression for the given token.</param>
    /// <returns>True if the token is valid syntax, false otherwise.</returns>
    /// <exception cref="FormulaFormatException">A FormulaFormatException if the token is a variable but is not valid according to the object's
    /// validator delegate.</exception>"
    private static bool IsValidToken(string token, Func<string, string> normalizer, Func<string, bool> validator, out string str)
    {
        if (IsVariable(token))
        {
            str = normalizer(token);

            // Check validity of normalized token
            if (!validator(str))
            {
                throw new FormulaFormatException
                    ($"Variable '{str}' is invalid syntax.");
            }

        }
        else if (IsDouble(token, out double value))
        {
            str = value.ToString();
        }
        else if (!IsOperator(token, out str))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the provided token can be parsed into a double floating point number.
    /// </summary>
    /// <param name="token">The token to be checked.</param>
    /// <param name="value">The parsed double.</param>
    /// <returns>true if the token can be parsed into a double, false otherwise.</returns>
    private static bool IsDouble(string token, out double value)
    {
        return double.TryParse(token, out value);
    }

    /// <summary>
    /// Checks if the provided token is a valid variable token.
    /// </summary>
    /// <param name="token">The token to be checked.</param>
    /// <param name="value">The normalized string representing the variable.</param>
    /// <returns>true if the token is a variable, false otherwise.</returns>
    private static bool IsVariable(string token)
    {
        // Proper variable format is letters or underscores followed by numbers.
        if (Regex.IsMatch(token, "^[a-zA-Z_][a-zA-Z_0-9]*$"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the provided token is an operator token.
    /// </summary>
    /// <param name="token">The token to be checked.</param>
    /// <param name="oper">The token's operator value as a string.</param>
    /// <returns>True if token is an operator, false otherwise.</returns>
    private static bool IsOperator(string token, out string oper)
    {
        oper = token[0].ToString();

        return token.Length == 1 &&
            (
            oper == "+" ||
            oper == "-" ||
            oper == "*" ||
            oper == "/" ||
            oper == "(" ||
            oper == ")"
            );
    }

    /// <summary>
    /// Verifies the syntactic integrity of the formula stored in this object.
    /// Checks for uneven parentheticals, consecutive operators or values, or naked leading/trailing operators.
    /// </summary>
    /// <exception cref="FormulaFormatException">If any infix syntax violations are discovered.</exception>
    private static void VerifyFormula(List<string> tokens)
    {
        // Verify there is at least one token
        if (tokens.Count < 1)
        {
            throw new FormulaFormatException("Cannot parse empty formula.");
        }

        // Check proper paren syntax
        VerifyParenRules(tokens);

        // Formula can't begin or end with an operator that isn't a parenthesis
        if (IsOperator(tokens[0], out string oper) && oper != "(")
        {
            throw new FormulaFormatException($"Formula has a naked leading operator, {oper}.");
        }

        if (IsOperator(tokens[^1], out oper) && oper != ")")
        {
            throw new FormulaFormatException($"Formula has a naked trailing operator, {oper}.");
        }

        // Check that there are no consecutive operators or values
        VerifyFollowingRules(tokens);
    }

    /// <summary>
    /// Gets all non-parenthetical strings from the passed in enumerable and sends them to
    /// another enumerable.
    /// </summary>
    /// <param name="tokens">The enumerable collection of strings to be parsed for parentheses.</param>
    /// <returns>A new enumerable collection with no parentheses.</returns>
    private static IEnumerable<string> GetNonParenTokens(IEnumerable<string> tokens)
    {
        foreach (string token in tokens)
        {
            if (token is not "(" and not ")")
            {
                yield return token;
            }
        }
    }
    
    /// <summary>
    /// Verifies that the formula has the proper parenthesis syntax, including an equal number of opening
    /// and closing parens.
    /// </summary>
    /// <param name="tokens">The list containing the token strings to be checked.</param>
    /// <exception cref="FormulaFormatException">If paren syntax is invalid.</exception>
    private static void VerifyParenRules(List<string> tokens)
    {
        // Count number of parens
        int leftParens = 0;
        int rightParens = 0;

        string previousToken = "";

        foreach (string token in tokens)
        {
            // Check if opening paren is followed by a closing paren
            if (previousToken == "(")
            {
                if (token == ")")
                {
                    throw new FormulaFormatException
                        ($"Empty parentheses at left-paren #{leftParens}.");
                }
            }
            if (token == "(")
            {
                leftParens++;
            }
            if (token == ")")
            {
                rightParens++;
            }

            if (rightParens > leftParens)
            {
                throw new FormulaFormatException
                    ($"Formula contains too many closing parentheses at closing parenthesis #{rightParens}.");
            }

            previousToken = token;
        }

        if (leftParens > rightParens)
        {
            throw new FormulaFormatException
                (String.Format($"Formula contains {leftParens-rightParens} unclosed parentheses."));
        }
    }

    /// <summary>
    /// Verifies that a proper token type follows each previous proper token type.
    /// Ignoring parens, each value (or variable) token should be followed by an operator token,
    /// and each operator token should be followed by a value (or variable) token.
    /// Parens don't affect following rules.
    /// </summary>
    /// <param name="tokens">The list of token strings to be verified.</param>
    /// <exception cref="FormulaFormatException">If there are two consecutive values (or variables) or operators.</exception>
    private static void VerifyFollowingRules(List<string> tokens)
    {
        string previousToken = new("");

        foreach (string token in GetNonParenTokens(tokens))
        {
            // Consecutive operators
            if (IsOperator(token, out string oper) && IsOperator(previousToken, out string oldOper))
            {
                throw new FormulaFormatException
                    ($"Consecutive operators, {previousToken} and {token}, detected.");
            }

            // Double or variable followed by a double or variable.
            if ((IsDouble(token, out double value) || IsVariable(token)) &&
                (IsDouble(previousToken, out value) || IsVariable(previousToken)))
            {
                throw new FormulaFormatException
                        ($"Consecutive values, {previousToken} and {token}, detected.");
            }

            previousToken = token;
        }
    }

    /// <summary>
    /// Evaluates this Formula, using the lookup delegate to determine the values of
    /// variables.  When a variable symbol v needs to be determined, it should be looked up
    /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to
    /// the constructor.)
    ///
    /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters
    /// in a string to upper case:
    ///
    /// new Formula("x+7", N, s => true).Evaluate(L) is 11
    /// new Formula("x+7").Evaluate(L) is 9
    ///
    /// Given a variable symbol as its parameter, lookup returns the variable's value
    /// (if it has one) or throws an ArgumentException (otherwise).
    ///
    /// If no undefined variables or divisions by zero are encountered when evaluating
    /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.
    /// The Reason property of the FormulaError should have a meaningful explanation.
    ///
    /// This method should never throw an exception.
    /// </summary>
    /// <param name="lookup">A delegate which returns the value represented by a variable input.</param>
    /// <returns>An with the value calculated for the given expression, stored as a double float. If the expression would result in a
    /// division by zero or contains a variable with no associated value, instead returns an informative FormulaError
    /// object.</returns>
    public object Evaluate(Func<string, double> lookup)
    {
        Stack<double> valueStack = new();
        Stack<string> operatorStack = new();

        double value;
        foreach (string token in tokens)
        {
            if (IsDouble(token, out value))
            {
                ProcessValue(value, valueStack, operatorStack);
            }
            else if (IsVariable(token))
            {
                try
                {
                    value = lookup(token);
                }
                catch (ArgumentException)
                {
                    return new FormulaError($"#REF");
                }

                ProcessValue(value, valueStack, operatorStack);
            }
            else if (IsOperator(token, out string oper))
            {
                ProcessOperator(oper, valueStack, operatorStack);
            }

            // Because syntax is checked in the constructor and VerifyFormula methods, preceding branches
            // should cover all possibilities.
        }

        // Process any values remaining on the stacks.
        value = ProcessFinish(valueStack, operatorStack);

        // Check for infinite value (denoting division by zero) and return a FormulaError if so.
        switch (value)
        {
            case double.NaN:
                return new FormulaError($"#DIV/0");
            case double.PositiveInfinity:
                return new FormulaError($"#DIV/0");
            case double.NegativeInfinity:
                return new FormulaError($"#DIV/0");
            default: break;
        }

        return value;
    }

    /// <summary>
    /// Calculates the value to be pushed based on the current state of the operation stack.
    /// </summary>
    /// <param name="value">The value of the token being evaluated.</param>
    /// <param name="valueStack">The stack of values to be operated on.</param>
    /// <param name="operatorStack">The stack of operators.</param>
    /// <returns>A double value with the result of the operation.</returns>
    private static void ProcessValue(double value, Stack<double> valueStack, Stack<string> operatorStack)
    {
        // If top operator is * or /, perform operation
        if (operatorStack.TryPeek(out string? nextOp) && (nextOp == "*" || nextOp == "/"))
        {
            value = Calculate(value, valueStack.Pop(), operatorStack.Pop());
        }
        
        valueStack.Push(value);
    }

    /// <summary>
    /// Processes a new operator token based on the current state of the value and operator stacks, possibly popping or pushing
    /// additional values to those stacks.
    /// </summary>
    /// <param name="oper">The operator token being processed.</param>
    /// <param name="valueStack">The stack of values to be operated on.</param>
    /// <param name="operatorStack">The already-existing stack of operators.</param>
    /// <returns>A double value with the result of the operation.</returns>
    private static void ProcessOperator(string oper, Stack<double> valueStack, Stack<string> operatorStack)
    {
        if (oper == "+" || oper == "-")
        {
            AddSubtractOperation(valueStack, operatorStack);

            operatorStack.Push(oper);
        }
        else if (oper == "*" || oper == "/" || oper == "(")
        {
            operatorStack.Push(oper); // Only adds to stack for now
        }
        else if (oper == ")")
        {
            // Add or subtract if necessary
            AddSubtractOperation(valueStack, operatorStack);

            // Pop left paren
            _ = operatorStack.Pop();

            // Multiply or divide if necessary
            MultiplyDivideOperation(valueStack, operatorStack);
        }
    }

    /// <summary>
    /// Performs add or subtract operation, storing the result in the value stack.
    /// </summary>
    /// <param name="oper">Operator character, either "+" or "-".</param>
    /// <param name="valueStack">Stack containing the current values.</param>
    /// <param name="operatorStack">Stack containing the current operators.</param>
    /// <returns>A double value with the result of the operation.</returns>
    private static void AddSubtractOperation(Stack<double> valueStack, Stack<string> operatorStack)
    {
        // If top operator on stack is +/-, perform operation before pushing token to opstack
        if (operatorStack.TryPeek(out string? nextOp) && (nextOp == "+" || nextOp == "-"))
        {
            double value = Calculate(valueStack.Pop(), valueStack.Pop(), operatorStack.Pop());

            valueStack.Push(value);
        }
    }

    /// <summary>
    /// Performs a multiply or divide operation, storing the result in the value stack.
    /// </summary>
    /// <param name="oper">Operator character, either "*" or "/".</param>
    /// <param name="valueStack">Stack containing the remaining values.</param>
    /// <param name="operatorStack">Stack containing the reminaing operators.</param>
    /// <returns>A double value with the result of the operation.</returns>
    private static void MultiplyDivideOperation(Stack<double> valueStack, Stack<string> operatorStack)
    {
        if (operatorStack.TryPeek(out string? nextOp) && (nextOp == "*" || nextOp == "/"))
        {
            double value = Calculate(valueStack.Pop(), valueStack.Pop(), operatorStack.Pop());

            valueStack.Push(value);
        }
    }

    /// <summary>
    /// Performs the relevant operation on the two data values, operating from RIGHT to LEFT. Because
    /// operations are performed from right-to-left order by the value stack, values must be read in from newest to
    /// oldest to guarantee proper behavior.
    /// E.G. 3/2 is popped off the stack in order 2 -> "/" -> 3, so 2 is the first and 3 is the second parameter.
    /// </summary>
    /// <param name="rightValue">The newer value.</param>
    /// <param name="leftValue">The older value.</param>
    /// <param name="operand">The operation to perform.</param>
    /// <returns>The calculation value.</returns>
    private static double Calculate(double rightValue, double leftValue, string operand)
    {
        // Can't implement without dead code, since the default value should never be reached
        // Except by making one operator type the "default".
        // Seems like bad practice, though
        return operand switch
        {
            "*" => rightValue * leftValue,
            "/" => leftValue / rightValue,
            "+" => leftValue + rightValue,
            _ => leftValue - rightValue,
        };
    }

    /// <summary>
    /// Checks for valid stack conditions after all tokens have been processed and calculates the final value.
    /// </summary>
    /// <param name="valueStack">The stack of values to be operated on.</param>
    /// <param name="operatorStack">The stack of operators.</param>
    /// <returns>The infix expression's final value</returns>
    private static double ProcessFinish(Stack<double> valueStack, Stack<string> operatorStack)
    {
        if (operatorStack.Count == 0 && valueStack.Count == 1)
        {
            return valueStack.Pop();
        }

        string oper = operatorStack.Pop();

        return Calculate(valueStack.Pop(), valueStack.Pop(), oper);
    }

    /// <summary>
    /// Enumerates the normalized versions of all variables that occur in this
    /// formula. Each variable is returned only once, even if it appears in the formula multiple times.
    /// </summary>
    /// <returns>An enumerable list with a single normalized copy of every variable
    /// that appears in this formula.</returns>
    public IEnumerable<string> GetVariables()
    {
        HashSet<string> variables = new();

        foreach (string token in tokens)
        {
            if (IsVariable(token))
            {
                _ = variables.Add(token);
            }
        }

        return variables;
    }

    /// <summary>
    /// Returns a string representing this formula, with no white space and all variables normalized, such that
    /// f1 == new Formula(f1.ToString());
    /// </summary>
    /// <returns>A string representation of this formula.</returns>
    public override string ToString()
    {
        StringBuilder sb = new(tokens.Count);

        foreach (string s in tokens)
        {
            _ = sb.Append(s);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Two Formulae are considered equal if they consist of the same tokens in the
    /// same order. 
    /// Numeric tokens are considered equal if they are equal after being "normalized" by
    /// using C#'s standard conversion from string to double and back to strig.
    /// Variable tokens are considered equal if their normalized forms are equal, as
    /// defined by the provided normalizer.
    ///
    /// For example, if N is a method that converts all the letters in a string to upper case:
    ///
    /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
    /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
    /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
    /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
    /// </summary>
    /// <returns>True if obj is a Formula and the two formulae are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null or not Formula)
        {
            return false;
        }

        // obj is Formula type and is not null.
        return this.Equals((Formula) obj);
    }

    /// <summary>
    /// Two Formulae are considered equal if they consist of the same tokens in the
    /// same order. 
    /// Numeric tokens are considered equal if they are equal after being "normalized" by
    /// using C#'s standard conversion from string to double and back to strig.
    /// Variable tokens are considered equal if their normalized forms are equal, as
    /// defined by the provided normalizer.
    ///
    /// For example, if N is a method that converts all the letters in a string to upper case:
    ///
    /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
    /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
    /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
    /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
    /// </summary>
    /// <returns>True if the two formulae are equal, false otherwise.</returns>
    private bool Equals(Formula f2)
    {
        // Compare strings
        return this.ToString() == f2.ToString();
    }

    /// <summary>
    /// Two Formulae are considered equal if they consist of the same tokens in the
    /// same order.  To determine token equality, all tokens are compared as strings
    /// except for numeric tokens and variable tokens.
    /// Numeric tokens are considered equal if they are equal after being "normalized" by
    /// using C#'s standard conversion from string to double (and optionally back to a string).
    /// Variable tokens are considered equal if their normalized forms are equal, as
    /// defined by the provided normalizer.
    ///
    /// For example, if N is a method that converts all the letters in a string to upper case:
    ///
    /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
    /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
    /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
    /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
    /// </summary>
    /// <returns>True if the two formulae are equal, false otherwise.</returns>
    public static bool operator ==(Formula f1, Formula f2)
    {
        return f1.Equals(f2);
    }

    /// <summary>
    /// Two Formulae are considered equal if they consist of the same tokens in the
    /// same order.  To determine token equality, all tokens are compared as strings
    /// except for numeric tokens and variable tokens.
    /// Numeric tokens are considered equal if they are equal after being "normalized" by
    /// using C#'s standard conversion from string to double (and optionally back to a string).
    /// Variable tokens are considered equal if their normalized forms are equal, as
    /// defined by the provided normalizer.
    ///
    /// For example, if N is a method that converts all the letters in a string to upper case:
    ///<list>
    /// <item>new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true</item>
    /// <item>new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false</item>
    /// <item>new Formula("x1+y2").Equals(new Formula("y2+x1")) is false</item>
    /// <item>new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true</item>
    /// </list>
    /// </summary>
    /// <returns>True if the two formulae are not equal, false otherwise.</returns>
    public static bool operator !=(Formula f1, Formula f2)
    {
        return !f1.Equals(f2);
    }

    /// <summary>
    /// Returns a hash code for this Formula. It is guaranteed that, if
    /// f1 == f2, then f1.GetHashCode() == f2.GetHashCode().
    /// </summary>
    /// <returns>An integer hash code.</returns>
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    /// <summary>
    /// Given a string representing an infix formula expression,
    /// enumerates the tokens that comprise it. Token consist of the following:
    /// <list type="bullet">
    /// <item>Left and right parentheses, "(" and ")"</item>
    /// <item>Plus and minus sign, "+" and "-"</item>
    /// <item>Multiplication and division sign, "*" and "/"</item>
    /// <item>Double literals, e.g. "23.5"</item>
    /// <item>Doubles as scientific notation, e.g. "1.5e4"</item>
    /// <item>Variables, e.g. "J24"</item>
    /// </list>
    /// Any additional characters and white space are ignored in the expression, and tokens with an
    /// invalid syntax are ignored.
    /// </summary>
    /// <returns>An enumerable collection of each infix token in the input string.</returns>
    private static IEnumerable<string> GetTokens(string formula)
    {
        // Patterns for individual tokens
        string lpPattern = @"\(";
        string rpPattern = @"\)";
        string opPattern = @"[\+\-*/]";
        string varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
        string doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
        string spacePattern = @"\s+";

        // Overall pattern
        string pattern = string.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                        lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

        // Enumerate matching tokens that don't consist solely of white space.
        foreach (string s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace)) {
            if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline)) {
                yield return s;
            }
        }
    }
}

/// <summary>
/// Used to report syntactic errors in the argument to the Formula constructor.
/// </summary>
public class FormulaFormatException : Exception
{
    /// <summary>
    /// Constructs a FormulaFormatException containing the explanatory message.
    /// </summary>
    public FormulaFormatException(string message) : base(message)
    {
    }
}

/// <summary>
/// Used as a possible return value of the Formula.Evaluate method.
/// </summary>
public struct FormulaError {
    /// <summary>
    /// Constructs a FormulaError containing the explanatory reason.
    /// </summary>
    /// <param name="reason">The reason this FormulaError was called.</param>
    public FormulaError(string reason) : this() 
    {
        Reason = reason;
    }

    /// <summary>
    ///  The reason why this FormulaError was created.
    /// </summary>
    public string Reason { get; private set; }

    /// <summary>
    /// Returns the string version of the reason for this Formula error.
    /// </summary>
    /// <returns>String version of this error.</returns>
    public override string ToString()
    {
        return Reason;
    }
}