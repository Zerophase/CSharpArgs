using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCode_Args
{
    public class Args
    {
        private readonly string[] args;
        private readonly List<char> argsFound = new List<char>();
        private readonly Dictionary<char, ArgumentMarshaler> booleanArgs = new Dictionary<char, ArgumentMarshaler>();
        private readonly Dictionary<char, int> intArgs = new Dictionary<char, int>();
        private readonly string schema;
        private readonly Dictionary<char, string> stringArgs = new Dictionary<char, string>();
        private readonly List<char> unexpectedArguments = new List<char>();
        private int currentArgument;
        private char errorArgumentId = '\0';
        private ErrorCode errorCode = ErrorCode.OK;
        private string errorParameter = "TILT";
        private bool valid = true;
        ArgumentMarshaler Am = new ArgumentMarshaler();

        public Args(string schema, string[] args)
        {
            this.schema = schema;
            this.args = args;
            valid = Parse();
        }

        private bool Parse()
        {
            if (schema.Length == 0 && args.Length == 0)
                return true;
            ParseSchema();
            try
            {
                ParseArguments();
            }
            catch (ArgsException e)
            {
                throw;
            }
            return valid;
        }

        private bool ParseSchema()
        {
            foreach (string element in schema.Split(','))
            {
                if (element.Length > 0)
                {
                    string trimmedElement = element.Trim();
                    ParseSchemaElement(trimmedElement);
                }
            }
            return true;
        }

        private void ParseSchemaElement(string element)
        {
            char elementId = element[0];
            string elementTail = element.Substring(1);
            ValidateSchemaElementId(elementId);
            if (IsBooleanSchemaElement(elementTail))
                ParseBooleanSchemaElement(elementId);
            else if (IsStringSchemaElement(elementTail))
                ParseStringSchemaElement(elementId);
            else if (IsIntegerSchemaElement(elementTail))
                ParseIntegerSchemaElement(elementId);
            else
                throw new FormatException(string.Format("Argument {0} has invalid format : {1}", elementId, elementTail));
        }

        private void ValidateSchemaElementId(char elementId)
        {
            if (!char.IsLetter(elementId))
                throw new FormatException(string.Format("Bad character: {0} in Args format {1}", elementId, schema));
        }

        private void ParseBooleanSchemaElement(char elementId)
        {
            booleanArgs.Add(elementId, new BoolArgumentMarshaler());
        }

        private void ParseIntegerSchemaElement(char elementId)
        {
            intArgs.Add(elementId, 0);
        }

        private void ParseStringSchemaElement(char elementId)
        {
            stringArgs.Add(elementId, string.Empty);
        }

        private bool IsStringSchemaElement(string elementTail)
        {
            return elementTail.Equals("*");
        }

        private bool IsBooleanSchemaElement(string elementTail)
        {
            return elementTail.Length == 0;
        }

        private bool IsIntegerSchemaElement(string elementTail)
        {
            return elementTail.Equals("#");
        }

        private bool ParseArguments()
        {
            for (currentArgument = 0; currentArgument < args.Length; currentArgument++)
            {
                string arg = args[currentArgument];
                ParseArguments(arg);
            }
            return true;
        }

        private void ParseArguments(string arg)
        {
            if (arg.StartsWith("-"))
                ParseElement(arg);
        }

        private void ParseElement(string arg)
        {
            for (int i = 1; i < arg.Length; i++)
                ParseElement(arg[i]);
        }

        private void ParseElement(char argChar)
        {
            if (SetArgument(argChar))
                argsFound.Add(argChar);
            else
            {
                unexpectedArguments.Add(argChar);
                errorCode = ErrorCode.Unexpected_argument;
                valid = false;
            }
        }

        private bool SetArgument(char argChar)
        {
            if (IsBooleanArg(argChar))
                setBooleanArg(argChar, true);
            else if (IsStringArg(argChar))
                SetStringArg(argChar);
            else if (IsIntArg(argChar))
                SetIntArg(argChar);
            else return false;

            return true;
        }

        private bool IsIntArg(char argChar)
        {
            return intArgs.ContainsKey((argChar));
        }

        private void SetIntArg(char argChar)
        {
            currentArgument++;
            string parameter = null;
            try
            {
                if (intArgs.ContainsKey(argChar))
                    intArgs.Remove(argChar);
                parameter = args[currentArgument];
                intArgs.Add(argChar, int.Parse(parameter));
            }
            catch (Exception e)
            {
                valid = false;
                errorArgumentId = argChar;
                errorCode = ErrorCode.Missing_integer;
                throw new ArgsException();
            }
        }

        private void SetStringArg(char argChar)
        {
            currentArgument++;
            try
            {
                if (stringArgs.ContainsKey(argChar))
                    stringArgs.Remove(argChar);
                stringArgs.Add(argChar, args[currentArgument]);
            }
            catch (Exception e)
            {
                valid = false;
                errorArgumentId = argChar;
                errorCode = ErrorCode.Missing_string;
                throw new ArgsException();
            }
        }

        private bool IsStringArg(char argChar)
        {
            return stringArgs.ContainsKey(argChar);
        }

        private void setBooleanArg(char argChar, bool value)
        {
            if (booleanArgs.ContainsKey(argChar))
                booleanArgs.Remove(argChar);
            
            booleanArgs.Add(argChar, Am.setBool(value));
        }

        private bool IsBooleanArg(char argChar)
        {
            return booleanArgs.ContainsKey(argChar);
        }

        public int Cardinality()
        {
            return argsFound.Count;
        }

        public string Usage()
        {
            if (schema.Length > 0)
            {
                return "-[" + schema + "]";
            }
            else
                return "";
        }

        public string ErrorMessage()
        {
            switch (errorCode)
            {
                case ErrorCode.OK:
                    throw new Exception("TILT : Should not get here.");
                case ErrorCode.Unexpected_argument:
                    return UnexpectedArgumentMessage();
                case ErrorCode.Missing_string:
                    return string.Format("Could not find string parameter for {0}", errorArgumentId);
                case ErrorCode.Invalid_Interger:
                    return string.Format("Argument {0} expects an integer but was {1}", errorArgumentId, errorParameter);
                case ErrorCode.Missing_integer:
                    return string.Format("Could not find integer parameter for {0}", errorArgumentId);
            }
            return string.Empty;
        }

        private string UnexpectedArgumentMessage()
        {
            var message = new StringBuilder("Argument(s) -");
            foreach (char c in unexpectedArguments)
            {
                message.Append(c);
            }
            message.Append(" unexpected.");
            return message.ToString();
        }

        private bool FalseIfNull(bool b)
        {
            return b != null && b;
        }

        private int ZeroIfNull(int i)
        {
            return i == null ? 0 : i;
        }


        private string BlankIfNull(string s)
        {
            return s == null ? "" : s;
        }

        public string GetString(char arg)
        {
            return BlankIfNull(stringArgs[arg]);
        }

        public int GetInt(char arg)
        {
            return ZeroIfNull(intArgs[arg]);
        }

        public bool GetBoolean(char arg)
        {
            Am = booleanArgs.Add(arg);
            return Am.getBool();
        }

        public bool Has(char arg)
        {
            return argsFound.Contains((arg));
        }

        public bool IsValid()
        {
            return valid;
        }

        #region Nested type: ArgsException

        private class ArgsException : Exception
        {
        }

        #endregion

        #region Nested type: ErrorCode

        private enum ErrorCode
        {
            OK,
            Missing_string,
            Missing_integer,
            Invalid_Interger,
            Unexpected_argument
        }

        #endregion
    }

    public class ArgumentMarshaler
    {
        private bool boolValue = false;

        public void setBool(bool value)
        {
            boolValue = value;
        }

        public bool getBool()
        {
            return boolValue;
        }
    }

    public class BoolArgumentMarshaler : ArgumentMarshaler 
    { 
    
    }

    public class StringArgumentMarshaler : ArgumentMarshaler
    { 
    
    }

    public class IntegerArgumentMarshaler : ArgumentMarshaler
    { 
    
    }
}