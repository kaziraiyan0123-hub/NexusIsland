using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NexusIsland.Services;

public class CalculatorService
{
    public List<string> History { get; } = new();

    public string Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return "0";

        try
        {
            // Normalize tokens
            string sanitized = expression
                .Replace("×", "*")
                .Replace("÷", "/")
                .Replace("π", Math.PI.ToString(CultureInfo.InvariantCulture))
                .Replace("e", Math.E.ToString(CultureInfo.InvariantCulture));

            double result = ParseExpression(sanitized);

            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                return "Error";
            }

            // Clean number format
            string formatted = Math.Round(result, 8).ToString(CultureInfo.InvariantCulture);
            History.Insert(0, $"{expression} = {formatted}");
            if (History.Count > 15) History.RemoveAt(History.Count - 1);

            return formatted;
        }
        catch
        {
            return "Error";
        }
    }

    // Recursive Descent Parser for arithmetic & scientific expressions
    private double ParseExpression(string expression)
    {
        int index = 0;

        double ParseSubExpression()
        {
            double value = ParseTerm();
            while (index < expression.Length)
            {
                char op = expression[index];
                if (op != '+' && op != '-') break;
                index++;
                double nextTerm = ParseTerm();
                value = (op == '+') ? value + nextTerm : value - nextTerm;
            }
            return value;
        }

        double ParseTerm()
        {
            double value = ParseFactor();
            while (index < expression.Length)
            {
                char op = expression[index];
                if (op != '*' && op != '/' && op != '%' && op != '^') break;
                index++;
                double nextFactor = ParseFactor();
                if (op == '*') value *= nextFactor;
                else if (op == '/') value = nextFactor == 0 ? double.NaN : value / nextFactor;
                else if (op == '%') value %= nextFactor;
                else if (op == '^') value = Math.Pow(value, nextFactor);
            }
            return value;
        }

        double ParseFactor()
        {
            SkipWhitespace();
            if (index >= expression.Length) return 0;

            // Unary minus or plus
            if (expression[index] == '+')
            {
                index++;
                return ParseFactor();
            }
            if (expression[index] == '-')
            {
                index++;
                return -ParseFactor();
            }

            // Parenthesized expression
            if (expression[index] == '(')
            {
                index++;
                double val = ParseSubExpression();
                SkipWhitespace();
                if (index < expression.Length && expression[index] == ')') index++;
                return val;
            }

            // Scientific functions
            if (char.IsLetter(expression[index]))
            {
                int start = index;
                while (index < expression.Length && char.IsLetter(expression[index])) index++;
                string func = expression.Substring(start, index - start).ToLowerInvariant();

                SkipWhitespace();
                double arg = ParseFactor();

                return func switch
                {
                    "sin" => Math.Sin(arg * Math.PI / 180.0), // degrees for intuitive desktop HUD use
                    "cos" => Math.Cos(arg * Math.PI / 180.0),
                    "tan" => Math.Tan(arg * Math.PI / 180.0),
                    "sqrt" => arg < 0 ? double.NaN : Math.Sqrt(arg),
                    "log" => arg <= 0 ? double.NaN : Math.Log10(arg),
                    "ln" => arg <= 0 ? double.NaN : Math.Log(arg),
                    "abs" => Math.Abs(arg),
                    _ => arg
                };
            }

            // Number literal
            int numStart = index;
            while (index < expression.Length && (char.IsDigit(expression[index]) || expression[index] == '.'))
            {
                index++;
            }

            string numStr = expression.Substring(numStart, index - numStart);
            if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                return parsed;
            }

            return 0;
        }

        void SkipWhitespace()
        {
            while (index < expression.Length && char.IsWhiteSpace(expression[index])) index++;
        }

        return ParseSubExpression();
    }
}
