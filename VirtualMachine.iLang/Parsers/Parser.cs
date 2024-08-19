using CommandLine;
using iLang.SyntaxDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using VirtualMachine.iLang.Checker;
using Boolean = iLang.SyntaxDefinitions.Boolean;
using String = iLang.SyntaxDefinitions.String;

namespace iLang.Parsers
{
    public static class Parsers
    {
        private static bool PeekToken(string code, ref int index, string[] tokens)
        {
            int start = index;
            foreach (var t in tokens)
            {
                if (code.Substring(index).StartsWith(t))
                {
                    return true;
                }
            }
            index = start;
            return false;
        }

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

            return Fail(code, ref index, start);
        }
        private static bool Fail(string code, ref int index, int start)
        {
            index = start;
            return false;
        }

        public static bool ParseName(string code, ref int index, out Name? name)
        {
            bool ParseSubName(string code, ref int index, out string? identifier)
            {
                identifier = null;
                var start = index;

                while (index < code.Length && char.IsLetter(code[index]))
                {
                    index++;
                }
                if (start == index) return Fail(code, ref index, start);
                identifier = code[start..index];
                return true;
            }

            name = null;
            var start = index;

            // parse name1.name2.name3
            string namespaceName = string.Empty;
            if (!ParseSubName(code, ref index, out string? subIdentifier)) return Fail(code, ref index, start);
            if(PeekToken(code, ref index, ["::"]))
            {
                namespaceName = subIdentifier;
                index+= 2;
                if(!ParseSubName(code, ref index, out subIdentifier)) return Fail(code, ref index, start);
            } 

            name = new Name(namespaceName, subIdentifier);
            return true;
        }

        private static bool ParseIdentifier(string code, ref int index, out Atom? identifier)
        {
            bool ParseSubIdentifier(string code, ref int index, out Identifier? identifier)
            {
                identifier = null;
                var start = index;

                if(ParseName(code, ref index, out Name? name))
                {
                    identifier = name;
                    return true;
                } else if(ParseIndexer(code, ref index, out Indexer? indexer))
                {
                    identifier = indexer;
                    return true;
                }
                else
                {
                     return Fail(code, ref index, start);
                }
            }

            var start = index;
            identifier = null;
            if (!ParseName(code, ref index, out Name? name))
            {
                return Fail(code, ref index, start);
            }

            List<Identifier> identifiers = new();
            identifiers.Add(name);

            while (index < code.Length && ParseSubIdentifier(code, ref index, out Identifier? subIdentifier))
            {
                identifiers.Add(subIdentifier);
            }

            if (identifiers.Count == 1)
            {
                identifier = identifiers[0];
            }
            else
            {
                identifier = new Composed(identifiers.ToArray());
            }

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

        private static bool ParseArgument(string code, ref int index, out Argument? arg)
        {
            arg = null;
            int start = index;
            if (!ParseIdentifier(code, ref index, out Atom? identifier)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, [":"], out _)) return Fail(code, ref index, start);
            if (!ParseType(code, ref index, out SyntaxDefinitions.TypeNode? type)) return Fail(code, ref index, start);
            arg = new Argument((Name)identifier, type);
            return true;
        }

        private static bool ParseArgumentList(string code, ref int index, out ArgumentList? argList)
        {
            argList = null;
            int start = index;

            if (!ParseToken(code, ref index, ["["], out _)) return Fail(code, ref index, start);
            List<Argument> args = new();
            bool parseNextToken = true;
            while (parseNextToken && ParseArgument(code, ref index, out var token))
            {
                args.Add(token);
                if (!ParseToken(code, ref index, [",", "]"], out string? nextToken)) return Fail(code, ref index, start);
                else
                {
                    parseNextToken = nextToken != "]";
                }
            }

            if (args.Count == 0) return Fail(code, ref index, start);

            argList = new ArgumentList(args.ToArray());
            return true;
        }

        private static bool ParseUnaryOperation(string code, ref int index, out UnaryOperation? unaryOp)
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
                unaryOp = new UnaryOperation(op, atom);
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

        private static bool ParseNull(string code, ref int index, out Atom? nullexpr)
        {
            nullexpr = null;
            int start = index;
            if (ParseToken(code, ref index, ["null"], out string? token))
            {
                nullexpr = new Null(token);
                return true;
            }
            return Fail(code, ref index, start);
        }

        private static bool ParseBinaryOperation(string code, ref int index, out BinaryOperation? binaryOp)
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
            binaryOp = new BinaryOperation(leftAtom, op, rightAtom);
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

        private static bool ParseCall(string code, ref int index, out CallExpression? call)
        {

            int start = index;

            call = null;
            if (!ParseIdentifier(code, ref index, out Atom? identifier)) return Fail(code, ref index, start);
            if (!ParseParameters(code, ref index, out Expression[]? argsList)) return Fail(code, ref index, start);

            call = new CallExpression((Name)identifier, new ParameterList(argsList));
            return true;
        }

        private static bool ParseExpression(string code, ref int index, out Expression? expression, bool excludeBinary = false)
        {
            expression = null;
            if (!excludeBinary && ParseBinaryOperation(code, ref index, out BinaryOperation? binaryOp))
            {
                expression = binaryOp;
                return true;
            }
            else if(ParseRecordValue(code, ref index, out RecordExpression? record))
            {
                expression = record;
                return true;
            }
            else if (ParseArrayExpr(code, ref index, out ArrayExpression? array))
            {
                expression = array;
                return true;
            }
            else if (ParseNull(code, ref index, out Atom? nullexpr))
            {
                expression = nullexpr;
                return true;
            }
            else if (ParseParenthesis(code, ref index, out ParenthesisExpr? parenthesis))
            {
                expression = parenthesis;
                return true;
            }
            else if (ParseCall(code, ref index, out CallExpression? call))
            {
                expression = call;
                return true;
            }
            else if (ParseUnaryOperation(code, ref index, out UnaryOperation? unaryOp))
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

        private static bool ParseIndexer(string code, ref int index, out Indexer? indexer)
        {
            indexer = null;
            int start = index;
            if (!ParseToken(code, ref index, ["["], out _)) return Fail(code, ref index, start);
            if (!ParseExpression(code, ref index, out Expression? indexExpr)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, ["]"], out _)) return Fail(code, ref index, start);
            indexer = new Indexer(indexExpr);
            return true;
        }

        private static bool ParseRecordValue(string code, ref int index, out RecordExpression? record)
        {
            record = null;
            int start = index;
            if (!ParseToken(code, ref index, ["new"], out _)) return Fail(code, ref index, start);
            if (!ParseType(code, ref index, out SyntaxDefinitions.TypeNode? type)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, ["|"], out _)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, ["{"], out _)) return Fail(code, ref index, start);
            Dictionary<string, Expression> fields = new();
            while (index < code.Length && code[index] != '}')
            {
                if (!ParseIdentifier(code, ref index, out Atom? identifier)) return Fail(code, ref index, start);
                if (!ParseToken(code, ref index, ["="], out _)) return Fail(code, ref index, start);
                if (!ParseExpression(code, ref index, out Expression? expression)) return Fail(code, ref index, start);
                fields.Add(((Name)identifier).FullName, expression);
                if (!ParseToken(code, ref index, [",", "}"], out string? token)) return Fail(code, ref index, start);
                if (token == "}") break;
            }
            if (!ParseToken(code, ref index, ["}"], out _)) return Fail(code, ref index, start);
            record = new RecordExpression(type, fields);
            return true;
        }

        private static bool ParseVariableDeclaration(string code, ref int index, out VarDeclaration? varDeclaration)
        {
            varDeclaration = null;
            int start = index;
            if (!ParseToken(code, ref index, ["letlocal", "letglobal", "let"], out var token)) return Fail(code, ref index, start);
            if (!ParseIdentifier(code, ref index, out Atom? identifier)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, [":"], out _)) return Fail(code, ref index, start);
            if (!ParseType(code, ref index, out SyntaxDefinitions.TypeNode? type))
            {
                type = null;
            }
            if (!ParseToken(code, ref index, ["="], out _)) return Fail(code, ref index, start);
            if (!ParseExpression(code, ref index, out Expression? expression)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, [";"], out _)) return Fail(code, ref index, start);
            varDeclaration = new VarDeclaration((Name)identifier, type, token == "letglobal", expression);
            return true;
        }

        private static bool ParseType(string code, ref int index, out SyntaxDefinitions.TypeNode? type)
        {
            ArrayTypeNode ConstructArrayNode(Name Name, Indexer[] dimentions)
            {
                if(dimentions.Length == 1)
                {
                    return new ArrayTypeNode(new SyntaxDefinitions.TypeNode(Name), dimentions[0].Index as Number);
                }
                else
                {
                    return new ArrayTypeNode(ConstructArrayNode(Name, dimentions[1..]), dimentions[0].Index as Number);
                }
            }

            int start = index;
            type = null;

            if (!ParseIdentifier(code, ref index, out Atom? identifier))
            {
                return Fail(code, ref index, start);    
            }

            if(identifier is Name name)
            {
                type = new SyntaxDefinitions.TypeNode(name);
            } else if(identifier is Composed composed)
            {
                type = ConstructArrayNode(composed.Root, composed.Values.OfType<Indexer>().ToArray());
            }
            return true;
        }

        private static bool ParseVariableAssignement(string code, ref int index, out Assignment? assignment)
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
                paths.Add(new FilePath(path.Value, ((Name)alias).FullName));
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
            else if (ParseVariableAssignement(code, ref index, out Assignment? assignment))
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

        public static bool ParseFunction(string code, ref int index, out FunctionDefinition function)
        {
            int start = index;
            function = null;
            if (!ParseIdentifier(code, ref index, out Atom? identifier))
            {
                Fail(code, ref index, start);
            }

            if (!ParseToken(code, ref index, [":"], out _)) return false;

            ArgumentList? argsList;
            TypeNode? returnType = null;
            bool returnTypeNotFound = false;
            if (identifier is Name identifier1 && identifier1.FullName.ToLower() != "main")
            {

                if (!ParseArgumentList(code, ref index, out argsList))
                {
                    return Fail(code, ref index, start);
                }

                if (!ParseToken(code, ref index, ["=>"], out _))
                {
                    return Fail(code, ref index, start);
                }

                if(!ParseType(code, ref index, out returnType))
                {
                    returnTypeNotFound = true;
                }
            }
            else
            {
                argsList = ArgumentList.Empty;
            }

            Block block = null;
            Expression expression = null;

            // if returnTypeNotFound we can only have an expression
            // otherwise we can have a block or an expression

            if(returnTypeNotFound && !ParseExpression(code, ref index, out expression))
            {
                return Fail(code, ref index, start);
            } else
            {
                if (!ParseBlock(code, ref index, out block) && !ParseExpression(code, ref index, out expression))
                {
                    return Fail(code, ref index, start);
                }
            }

            if (!ParseToken(code, ref index, [";"], out _)) return Fail(code, ref index, start);
            function = new FunctionDefinition((Name)identifier, returnType, argsList, ((SyntaxTree)block) ?? ((SyntaxTree)expression));
            return true;
        }

        private static bool ParseArrayExpr(string code, ref int index, out ArrayExpression arrayExpr)
        {
            var start = index;
            arrayExpr = null;
            if (!ParseToken(code, ref index, ["new"], out _)) return Fail(code, ref index, start);
            if(!ParseType(code, ref index, out SyntaxDefinitions.TypeNode? type))
            {
                return Fail(code, ref index, start);
            }
            if (!ParseToken(code, ref index, ["|"], out _)) return Fail(code, ref index, start);
            if (!ParseToken(code, ref index, ["["], out _))
            {
                return Fail(code, ref index, start);
            }

            List<Expression> items = new();
            while (index < code.Length && code[index] != ']')
            {
                if (!ParseExpression(code, ref index, out Expression? expression))
                {
                    return Fail(code, ref index, start);
                }
                items.Add(expression);
                if (!ParseToken(code, ref index, [",", "]"], out string? token))
                {
                    return Fail(code, ref index, start);
                }
                if (token == "]") break;
            }


            arrayExpr = new ArrayExpression(type, items.ToArray());
            return true;
        }

        private static bool ParseFieldDef(string code, ref int index, out FieldDefinition fieldDef)
        {
            int start = index;
            fieldDef = null;
            if (!ParseIdentifier(code, ref index, out Atom? identifier))
            {
                return Fail(code, ref index, start);
            }
            if (!ParseToken(code, ref index, [":"], out _))
            {
                return Fail(code, ref index, start);
            }
            if (!ParseType(code, ref index, out SyntaxDefinitions.TypeNode? type))
            {
                return Fail(code, ref index, start);
            }
            if (!ParseToken(code, ref index, [","], out _))
            {
                return Fail(code, ref index, start);
            }
            fieldDef = new FieldDefinition(((Name)identifier).FullName, type);
            return true;
        }

        private static bool ParseTypeDef(string code, ref int index, out TypeDefinition typeDef)
        {
            int start = index;
            typeDef = null;

            if (!ParseToken(code, ref index, ["typedef"], out _))
            {
                return Fail(code, ref index, start);
            }
            
            if (!ParseIdentifier(code, ref index, out Atom? identifier))
            {
                return Fail(code, ref index, start);
            }

            if(!ParseToken(code, ref index, [":="], out _))
            {
                return Fail(code, ref index, start);
            }

            if (!ParseToken(code, ref index, ["{"], out _))
            {
                return Fail(code, ref index, start);
            }

            List<FieldDefinition> fields = new();
            while (index < code.Length && code[index] != '}')
            {
                if (!ParseFieldDef(code, ref index, out FieldDefinition fieldDef))
                {
                    return Fail(code, ref index, start);
                }
                fields.Add(fieldDef);
            }

            if (!ParseToken(code, ref index, ["}"], out _))
            {
                return Fail(code, ref index, start);
            }
            typeDef = new TypeDefinition(((Name)identifier).FullName, fields.ToArray());
            return true;
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
            code = code.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");

            int index = 0;
            compilationUnit = null;

            Dictionary<string, CompilationUnit> Funclibraries = new();
            Dictionary<string, CompilationUnit> Typelibraries = new();


            if (ParseIncludeRegion(code, ref index, out IncludeFile includeFiles) && includeFiles.Paths.Length > 0)
            {
                var groups = includeFiles.Paths.GroupBy(x => x.Path.EndsWith(".ilt")).ToList();
                if (groups.Count < 1 || !HandleFuncIncludes(new IncludeFile(groups[0].ToArray()), out Funclibraries))
                {
                    return false;
                }

                if (groups.Count < 2 || !HandleTypeIncludes(new IncludeFile(groups[1].ToArray()), out Typelibraries))
                {
                    return false;
                }
            }

            
            List<SyntaxTree> body = new();
            FunctionDefinition? function = null;
            TypeDefinition? typedef = null;
            while (index < code.Length)
            {
                if (ParseFunction(code, ref index, out function) || ParseTypeDef(code, ref index, out typedef))
                {
                    if (function is not null && (body.Count == 0 || body.First() is FunctionDefinition))
                    {
                        body.Add(function);
                        continue;
                    }
                    else if (typedef is not null && (body.Count == 0 || body.First() is TypeDefinition))
                    {
                        body.Add(typedef);
                        continue;
                    }
                }
                return false;
            }
            compilationUnit = new CompilationUnit(Typelibraries, Funclibraries, [.. body]);

            return true;
        }

        private static bool HandleTypeIncludes(IncludeFile includeFile, out Dictionary<string, CompilationUnit> typelibraries)
        {
            typelibraries = new();
            foreach (var file in includeFile.Paths)
            {
                string code = System.IO.File.ReadAllText(file.Path);
                if (!ParseCompilationUnit(code, out var typeDef))
                {
                    return false;
                }
                typelibraries.Add(file.Alias, typeDef);
            }
            return true;
        }

        private static bool HandleFuncIncludes(IncludeFile includeFiles, out Dictionary<string, CompilationUnit> compilationUnits)
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
