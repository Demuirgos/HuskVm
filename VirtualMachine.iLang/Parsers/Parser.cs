using iLang.SyntaxDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boolean = iLang.SyntaxDefinitions.Boolean;
using String = iLang.SyntaxDefinitions.String;

namespace iLang.Parsers
{
    public static class Parsers
    {
        private static bool ParseToken(string code, ref int index, string[] tokens, out string? token)
        {
            int start = index;
            token = null;
            foreach (var t in tokens)
            {
                if (code.Substring(index).StartsWith(t))
                {
                    index += t.Length;
                    token = t;
                    return true;
                }
            }
            index = start;
            return false;
        }
        private static bool Fail(string code, ref int index, int start)
        {
            index = start;
            return false;
        }
        private static bool ParseIdentifier(string code, ref int index, out Atom? identifier)
        {
            bool ParseSubIdentifier(string code, ref int index, out Atom? identifier)
            {
                identifier = null;
                var start = index;

                while (index < code.Length && char.IsLetter(code[index]))
                {
                    index++;
                }
                if (start == index) return Fail(code, ref index, start);
                identifier = new Identifier(code[start..index]);
                return true;
            }

            identifier = null;
            var start = index;
            
            // parse name1.name2.name3
            List<string> subIdentifiers = new();

            do
            {
                if (!ParseSubIdentifier(code, ref index, out Atom? subIdentifier)) 
                    return Fail(code, ref index, start);
                subIdentifiers.Add(((Identifier)subIdentifier).Value);
            } while (ParseToken(code, ref index, ["."], out _));

            identifier = new Identifier(subIdentifiers.ToArray());
            return true;
        }

        private static bool ParseNumber(string code, ref int index, out Atom? number)
        {
            number = null;
            var start = index;
            while (index < code.Length && char.IsDigit(code[index]))
            {
                index++;
            }
            if (start == index) return Fail(code, ref index, start);
            number = new Number(double.Parse(code[start..index]));
            return true;
        }

        private static bool ParseString(string code, ref int index, out String? identifier)
        {
            identifier = null;
            var start = index;
            if (code[index] == '"')
            {
                index++;
                while (index < code.Length && code[index] != '"')
                {
                    index++;
                }
                if (index == code.Length) return Fail(code, ref index, start);
                index++;
                identifier = new String(code[(start + 1)..(index - 1)]);
                return true;
            } else
            {
                return Fail(code, ref index, start);
            }
        }

        private static bool ParseConditional(string code, ref int index, out IfStatement? conditional)
        {
            conditional = null;
            int start = index;
            if (!ParseToken(code, ref index, ["if"], out _)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, ["("], out _)) return Fail(code, ref index, start);
            if (!ParseExpression(code, ref index, out Expression? condition)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, [")"], out _)) return Fail(code, ref index, start);

            if (!ParseToken(code, ref index, ["then"], out _)) return Fail(code, ref index, start);
            if (!ParseBlock(code, ref index, out Block? trueBlock)) return Fail(code, ref index, start);

            if (!ParseToken(code, ref index, ["else"], out _)) return Fail(code, ref index, start);
            if (!ParseBlock(code, ref index, out Block? falseBlock)) return Fail(code, ref index, start);

            conditional = new IfStatement(condition, trueBlock, falseBlock);
            return true;
        }

        private static bool ParseAtom(string code, ref int index, out Atom? token)
        {
            token = null;
            int start = index;
            if (index < code.Length && char.IsLetter(code[index]))
            {
                if (ParseBoolean(code, ref index, out token)) return true;
                return ParseIdentifier(code, ref index, out token);
            }
            if (index < code.Length && char.IsDigit(code[index]))
            {
                return ParseNumber(code, ref index, out token);
            }
            return Fail(code, ref index, start);
        }

        private static bool ParseArgumentList(string code, ref int index, out Atom[]? argList)
        {
            argList = null;
            int start = index;

            if (!ParseToken(code, ref index, ["["], out _)) return Fail(code, ref index, start);
            List<Atom> args = new();
            bool parseNextToken = true;
            while (parseNextToken && ParseIdentifier(code, ref index, out var token))
            {
                args.Add(token);
                if (!ParseToken(code, ref index, [",", "]"], out string? nextToken)) return Fail(code, ref index, start);
                else
                {
                    parseNextToken = nextToken != "]";
                }
            }

            if (args.Count == 0) return Fail(code, ref index, start);

            argList = args.ToArray();
            return true;
        }

        private static bool ParseUnaryOperation(string code, ref int index, out UnaryOp? unaryOp)
        {
            unaryOp = null;
            Expression? atom = null;
            int start = index;


            Operation? op = code[index++] switch
            {
                '+' => new Operation('+'),
                '-' => new Operation('-'),
                '!' => new Operation('!'),
                '~' => new Operation('~'),
                _ => null
            };

            if (op == null) return Fail(code, ref index, start);

            if (ParseExpression(code, ref index, out atom, excludeBinary: true))
            {
                unaryOp = new UnaryOp(op, atom);
                return true;
            }
            return Fail(code, ref index, start);
        }

        private static bool ParseParenthesis(string code, ref int index, out ParenthesisExpr? parenthesis)
        {
            parenthesis = null;
            int start = index;
            if (!ParseToken(code, ref index, ["("], out _)) return Fail(code, ref index, start);
            if (!ParseExpression(code, ref index, out Expression? body)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, [")"], out _)) return Fail(code, ref index, start);
            parenthesis = new ParenthesisExpr(body);
            return true;
        }

        private static bool ParseBoolean(string code, ref int index, out Atom? boolean)
        {
            boolean = null;
            int start = index;
            if (ParseToken(code, ref index, ["true", "false"], out string? token))
            {
                boolean = new Boolean(token == "true");
                return true;
            }
            return Fail(code, ref index, start);
        }

        private static bool ParseBinaryOperation(string code, ref int index, out BinaryOp? binaryOp)
        {
            binaryOp = null;
            Operation op = null;
            int start = index;

            if (!ParseExpression(code, ref index, out Expression? leftAtom, excludeBinary: true))
            {
                return Fail(code, ref index, start);
            }

            string[] strings = ["+", "-", "*", "/", "%", "<", ">", "=", "&", "|", "^"];
            if (!ParseToken(code, ref index, strings, out string? token))
            {
                return Fail(code, ref index, start);
            }

            op = new Operation(token[0]);

            if (!ParseExpression(code, ref index, out Expression? rightAtom))
            {
                return Fail(code, ref index, start);
            }
            binaryOp = new BinaryOp(leftAtom, op, rightAtom);
            return true;
        }
        private static bool ParseParameters(string code, ref int index, out Expression[]? argsList)
        {
            int start = index;
            argsList = null;

            if (!ParseToken(code, ref index, ["("], out _)) return Fail(code, ref index, start);
            List<Expression> args = new();
            bool parseNextToken = true;
            while (parseNextToken && ParseExpression(code, ref index, out var token))
            {
                args.Add(token);
                if (!ParseToken(code, ref index, [",", ")"], out string? NextToken)) return Fail(code, ref index, start);
                else
                {
                    parseNextToken = NextToken != ")";
                }
            }
            if (args.Count == 0) return Fail(code, ref index, start);

            argsList = args.ToArray();
            return true;

        }

        private static bool ParseLoop(string code, ref int index, out WhileStatement? loop)
        {
            loop = null;
            int start = index;
            if (!ParseToken(code, ref index, ["while"], out _)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, ["("], out _)) return Fail(code, ref index, start);
            if (!ParseExpression(code, ref index, out Expression? condition)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, [")"], out _)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, ["do"], out _)) return Fail(code, ref index, start);
            if (!ParseBlock(code, ref index, out Block? body)) return Fail(code, ref index, start);
            loop = new WhileStatement(condition, body);
            return true;
        }

        private static bool ParseCall(string code, ref int index, out CallExpr? call)
        {

            int start = index;

            call = null;
            if (!ParseIdentifier(code, ref index, out Atom? identifier)) return Fail(code, ref index, start);
            if (!ParseParameters(code, ref index, out Expression[]? argsList)) return Fail(code, ref index, start);

            call = new CallExpr((Identifier)identifier, new ParameterList(argsList));
            return true;
        }

        private static bool ParseExpression(string code, ref int index, out Expression? expression, bool excludeBinary = false)
        {
            expression = null;
            if (!excludeBinary && ParseBinaryOperation(code, ref index, out BinaryOp? binaryOp))
            {
                expression = binaryOp;
                return true;
            }
            else if (ParseParenthesis(code, ref index, out ParenthesisExpr? parenthesis))
            {
                expression = parenthesis;
                return true;
            }
            else if (ParseCall(code, ref index, out CallExpr? call))
            {
                expression = call;
                return true;
            }
            else if (ParseUnaryOperation(code, ref index, out UnaryOp? unaryOp))
            {
                expression = unaryOp;
                return true;
            }
            else if (ParseAtom(code, ref index, out Atom? atom))
            {
                expression = atom;
                return true;
            }
            return false;
        }

        private static bool ParseVariableDeclaration(string code, ref int index, out VarDeclaration? varDeclaration)
        {
            varDeclaration = null;
            int start = index;
            if (!ParseToken(code, ref index, ["var"], out _)) return Fail(code, ref index, start);
            if (!ParseIdentifier(code, ref index, out Atom? identifier)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, ["<-"], out _)) return Fail(code, ref index, start);
            if (!ParseExpression(code, ref index, out Expression? expression)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, [";"], out _)) return Fail(code, ref index, start);
            varDeclaration = new VarDeclaration((Identifier)identifier, expression);
            return true;
        }

        private static bool ParseVariavleAssignement(string code, ref int index, out Assignment? assignment)
        {
            assignment = null;
            int start = index;
            if (!ParseIdentifier(code, ref index, out Atom? identifier)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, ["<-"], out _)) return Fail(code, ref index, start);
            if (!ParseExpression(code, ref index, out Expression? expression)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, [";"], out _)) return Fail(code, ref index, start);
            assignment = new Assignment((Identifier)identifier, expression);
            return true;
        }

        private static bool ParseIncludeFiles(string code, ref int index, out IncludeFile? includeFile)
        {
            // include ["path1", "path2"];
            includeFile = null;
            int start = index;
            if (!ParseToken(code, ref index, ["include"], out _)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, ["["], out _)) return Fail(code, ref index, start);
            List<FilePath> paths = new();
            while (index < code.Length && code[index] != ']')
            {
                if (!ParseString(code, ref index, out String path)) return Fail(code, ref index, start);
                if (!ParseToken(code, ref index, ["as"], out _)) return Fail(code, ref index, start);
                if (!ParseIdentifier(code, ref index, out Atom alias)) return Fail(code, ref index, start);
                paths.Add(new FilePath(path.Value, ((Identifier)alias).Value));
                if (!ParseToken(code, ref index, [",", "]"], out string? token)) return Fail(code, ref index, start);
                if (token == "]") break;
            }
            if (!ParseToken(code, ref index, [";"], out _)) return Fail(code, ref index, start);
            includeFile = new IncludeFile(paths.ToArray());
            return true;
        }

        private static bool ParseStatement(string code, ref int index, out Statement? statement)
        {
            statement = null;
            if (ParseConditional(code, ref index, out IfStatement? conditional))
            {
                statement = conditional;
                return true;
            }
            else if (ParseVariableDeclaration(code, ref index, out VarDeclaration? varDeclaration))
            {
                statement = varDeclaration;
                return true;
            }
            else if (ParseVariavleAssignement(code, ref index, out Assignment? assignment))
            {
                statement = assignment;
                return true;
            }
            else if (ParseLoop(code, ref index, out WhileStatement? loop))
            {
                statement = loop;
                return true;
            }
            else if (ParseReturn(code, ref index, out ReturnStatement? returnStatement))
            {
                statement = returnStatement;
                return true;
            } 
            return false;
        }

        public static bool ParseReturn(string code, ref int index, out ReturnStatement? returnStatement)
        {
            returnStatement = null;
            int start = index;
            if (!ParseToken(code, ref index, ["return"], out _)) return Fail(code, ref index, start);
            if (!ParseExpression(code, ref index, out Expression? expression)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, [";"], out _)) return Fail(code, ref index, start);
            returnStatement = new ReturnStatement(expression);
            return true;
        }

        private static bool ParseBlock(string code, ref int index, out Block? block)
        {
            block = null;
            int start = index;
            if (!ParseToken(code, ref index, ["{"], out _)) return Fail(code, ref index, start);
            List<Statement> statements = new();
            while (index < code.Length && code[index] != '}')
            {
                if (!ParseStatement(code, ref index, out Statement? statement)) return Fail(code, ref index, start);
                statements.Add(statement!);
            }
            if (!ParseToken(code, ref index, ["}"], out _)) return Fail(code, ref index, start);

            if (statements.Count == 0) return Fail(code, ref index, start);

            block = new Block(statements.ToArray());
            return true;
        }

        public static bool ParseFunction(string code, ref int index, out FunctionDef function)
        {

            function = null;
            if (!ParseIdentifier(code, ref index, out Atom? identifier))
            {
                return false;
            }

            if (!ParseToken(code, ref index, [":"], out _)) return false;

            Atom[]? argsList;
            if (identifier is Identifier identifier1 && identifier1.Value.ToLower() != "main")
            {

                if (!ParseArgumentList(code, ref index, out argsList))
                {
                    return false;
                }

                if (!ParseToken(code, ref index, ["=>"], out _))
                {
                    return false;
                }
            }
            else
            {
                argsList = Array.Empty<Atom>();
            }

            if (ParseBlock(code, ref index, out Block? body))
            {
                if (!ParseToken(code, ref index, [";"], out _)) return false;
                function = new FunctionDef((Identifier)identifier, new ArgumentList(argsList.Cast<Identifier>().ToArray()), body);
                return true;
            }
            else if (ParseExpression(code, ref index, out Expression? expression))
            {
                if (!ParseToken(code, ref index, [";"], out _)) return false;
                function = new FunctionDef((Identifier)identifier, new ArgumentList(argsList.Cast<Identifier>().ToArray()), expression);
                return true;
            }
            return false;
        }

        private static bool ParseIncludeRegion(string code, ref int index, out IncludeFile includeFiles)
        {
            includeFiles = default;
            List<FilePath> files = new();
            while (index < code.Length)
            {
                if (!ParseIncludeFiles(code, ref index, out IncludeFile? includeFile))
                {
                    break;
                }
                files.AddRange(includeFile!.Paths);
            }

            includeFiles = new IncludeFile(files.ToArray());
            return true;
        }

        public static bool ParseCompilationUnit(string code, out CompilationUnit compilationUnit)
        {
            code = code.Replace(" ", "").Replace("\n", "").Replace("\r", "");

            int index = 0;
            compilationUnit = null;

            Dictionary<string, CompilationUnit> libraries = new();
            if (ParseIncludeRegion(code, ref index, out IncludeFile includeFiles))
            {
                if (!HandleIncludes(includeFiles, out libraries))
                {
                    return false;
                }
            }

            
            List<FunctionDef> body = new();
            while (index < code.Length)
            {
                if (!ParseFunction(code, ref index, out FunctionDef? function))
                {
                    return false;
                }
                body.Add(function);
            }
            compilationUnit = new CompilationUnit(libraries, [.. body]);

            return true;
        }

        private static bool HandleIncludes(IncludeFile includeFiles, out Dictionary<string, CompilationUnit> compilationUnits)
        {
            compilationUnits = new();
            foreach (var file in includeFiles.Paths)
            {
                string code = System.IO.File.ReadAllText(file.Path);
                if(!ParseCompilationUnit(code, out var unit))
                {
                    return false;
                }
                compilationUnits.Add(file.Alias, unit);
            }
            return true;
        }
    }
}
