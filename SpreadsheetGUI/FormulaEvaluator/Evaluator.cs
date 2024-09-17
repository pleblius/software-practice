using System.Text.RegularExpressions;

namespace FormulaEvaluator
{
    
    /// <summary>
    /// A static class containing methods to solve a provided infix expression with possible variable values.
    /// </summary>
    public static class Evaluator
    {
        public delegate int Lookup(String v);
        
        /// <summary>
        /// Evaluates the final of the provided string as an infix expression, using the provided delegate to calculate variable values.
        /// </summary>
        /// <param name="exp">The string expression to be evaluated.</param>
        /// <param name="variableEvaluator">A variable calculation delegate.</param>
        /// <returns>The expression's value</returns>
        /// <exception cref="ArgumentException">If the expression is null or is not properly structured.</exception>
        public static int Evaluate(String exp, Lookup variableEvaluator)
        {
            if (exp == null)
            {
                throw new ArgumentException("Null value detected.");
            }

            List<string> tokens = new List<string>();

            // Parse expression into tokens and trim whitespace
            string[] strings = Regex.Split(exp, "(\\()|(\\))|(-)|(\\+)|(\\*)|(/)");
            for (int i = 0; i < strings.Length; i++)
            {
                string v = strings[i].Trim();
                if (!v.Equals(""))
                {
                    tokens.Add(v);
                }
            }

            return Evaluate(tokens, variableEvaluator);
        }
        
        /// <summary>
        /// Helper method to evaluate the final value of the provided infix expression.
        /// </summary>
        /// <param name="tokens">A list of parsed string tokens to be processed.</param>
        /// <param name="variableEvaluator">The variable lookup delegate.</param>
        /// <returns>The expression's final value.</returns>
        /// <exception cref="ArgumentException">If any token is not an int, a variable of the form [letter][int],
        /// or an arithmetic operator.</exception>
        private static int Evaluate(List<string> tokens, Lookup variableEvaluator)
        {
            Stack<int> valueStack = new Stack<int>();
            Stack<char> operatorStack = new Stack<char>();

            foreach (string token in tokens)
            {
                int value;
                char oper;

                if (IsValue(token, variableEvaluator, out value))
                {
                    ProcessValue(value, valueStack, operatorStack);
                }
                else if (IsOperator(token, out oper))
                {
                    ProcessOperator(oper, valueStack, operatorStack);
                }
                else
                {
                    throw new ArgumentException("Unexpected token.");
                }
            }

            return ProcessFinish(valueStack, operatorStack);
        }

        /************           Helper Methods          ************/

        /// <summary>
        /// Checks if the provided token is an int or variable int token.
        /// </summary>
        /// <param name="token">The token to be checked.</param>
        /// <param name="variableEvaluator">The variable lookup delegate.</param>
        /// <param name="value">The integer value of the token.</param>
        /// <returns>true if the token is a number, false otherwise.</returns>
        /// <exception cref="ArgumentException">If a variable token cannot be evaluated by the lookup delegate.</exception>
        private static bool IsValue(string token, Lookup variableEvaluator, out int value)
        {
            // Check if token begins with a letter - variable format
            if (Regex.IsMatch(token, "^[A-Z]+[0-9]+$", RegexOptions.IgnoreCase))
            {
                try
                {
                    value = variableEvaluator(token);
                }
                catch
                {
                    throw new ArgumentException("Unexpected variable behavior.");
                }

                return true;
            }

            return int.TryParse(token, out value);
        }

        /// <summary>
        /// Calculates the value to be pushed based on the current state of the operation stack.
        /// </summary>
        /// <param name="value">The value of the token being evaluated.</param>
        /// <param name="valueStack">The stack of values to be operated on.</param>
        /// <param name="operatorStack">The stack of operators.</param>
        /// <exception cref="ArgumentException"></exception>
        private static void ProcessValue(int value, Stack<int> valueStack, Stack<char> operatorStack)
        {
            char nextOp;

            // If top operator is * or /, perform operation
            if (operatorStack.TryPeek(out nextOp) && (nextOp == '*' || nextOp == '/'))
            {
                if (valueStack.Count == 0)
                {
                    throw new ArgumentException("Expected value missing.");
                }

                value = Calculate(value, valueStack.Pop(), operatorStack.Pop());
            }

            valueStack.Push(value);
        }

        /// <summary>
        /// Checks if the provided token is an operator token.
        /// </summary>
        /// <param name="token">The token to be checked.</param>
        /// <param name="oper">The token's operator value as a character.</param>
        /// <returns>true if token is an operator, false otherwise.</returns>
        private static bool IsOperator(string token, out char oper)
        {
            oper = token[0];

            if (token.Length == 1 &&
                oper == '+' ||
                oper == '-' ||
                oper == '*' ||
                oper == '/' ||
                oper == '(' ||
                oper == ')')
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Processes a new operator token based on the current state of the value and operator stacks, possibly popping or pushing
        /// additional values to those stacks.
        /// </summary>
        /// <param name="oper">The operator token being processed.</param>
        /// <param name="valueStack">The stack of values to be operated on.</param>
        /// <param name="operatorStack">The already-existing stack of operators.</param>
        /// <exception cref="ArgumentException">If there are not enough values on the stack to perform the relevant operation.</exception>
        private static void ProcessOperator(char oper, Stack<int> valueStack, Stack<char> operatorStack)
        {
            if (oper == '+' || oper == '-')
            {
                AddSubtractOperation(valueStack, operatorStack);

                operatorStack.Push(oper);
            }
            else if (oper == '*' || oper == '/' || oper == '(')
            {
                operatorStack.Push(oper); // Only adds to stack for now
            }
            else if (oper == ')')
            {
                // Add or subtract if necessary
                AddSubtractOperation(valueStack, operatorStack);

                // After add/subtract next operator must be an open paren
                if (operatorStack.Count == 0 || operatorStack.Pop() != '(')
                {
                    throw new ArgumentException("Expected parenthesis missing.");
                }

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
        /// <exception cref="ArgumentException">If the value stack has fewer than two values to add or subtract.</exception>
        private static void AddSubtractOperation(Stack<int> valueStack, Stack<char> operatorStack)
        {
            char nextOp;

            // If top operator on stack is +/-, perform operation before pushing token to opstack
            if (operatorStack.TryPeek(out nextOp) && (nextOp == '+' || nextOp == '-'))
            {
                if (valueStack.Count < 2)
                {
                    throw new ArgumentException("Expected values missing.");
                }

                valueStack.Push(Calculate(valueStack.Pop(), valueStack.Pop(), operatorStack.Pop()));
            }
        }

        /// <summary>
        /// Performs a multiply or divide operation, storing the result in the value stack.
        /// </summary>
        /// <param name="oper">Operator character, either "*" or "/".</param>
        /// <param name="valueStack">Stack containing the remaining values.</param>
        /// <param name="operatorStack">Stack containing the reminaing operators.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">If the value stack has fewer than two values to multiply or divide.</exception>
        private static void MultiplyDivideOperation(Stack<int> valueStack, Stack<char> operatorStack)
        {
            char nextOp;

            if (operatorStack.TryPeek(out nextOp) && (nextOp == '*' || nextOp == '/'))
            {
                if (valueStack.Count < 2)
                {
                    throw new ArgumentException("Expected values missing.");
                }
                
                valueStack.Push(Calculate(valueStack.Pop(), valueStack.Pop(), operatorStack.Pop()));
            }
        }

        /// <summary>
        /// Checks for valid stack conditions after all tokens have been processed and calculates the final value.
        /// </summary>
        /// <param name="valueStack">The stack of values to be operated on.</param>
        /// <param name="operatorStack">The stack of operators.</param>
        /// <returns>The infix expression's final value</returns>
        /// <exception cref="ArgumentException">If token evaluation has finished and the expression cannot be evaluated.</exception>
        private static int ProcessFinish(Stack<int> valueStack, Stack<char> operatorStack)
        {
            if (operatorStack.Count == 0 && valueStack.Count == 1)
            {
                return valueStack.Pop();
            }
            else if (operatorStack.Count == 1)
            {
                char oper = operatorStack.Pop();

                if (valueStack.Count != 2 || oper != '+' && oper != '-')
                {
                    throw new ArgumentException();
                }

                return Calculate(valueStack.Pop(), valueStack.Pop(), oper);
            }

            throw new ArgumentException();
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
        /// <exception cref="ArgumentException">If calculation would result in a division by zero or if operand is invalid.</exception>
        private static int Calculate(int rightValue, int leftValue, char operand)
        {
            if (operand == '/' && rightValue == 0)
            {
                throw new ArgumentException("Divide by zero error.");
            }

            return operand switch
            {
                '*' => leftValue * rightValue,
                '/' => leftValue / rightValue,
                '+' => leftValue + rightValue,
                '-' => leftValue - rightValue,
                _ => throw new ArgumentException("Invalid operator.")
            };
        }
    }
}