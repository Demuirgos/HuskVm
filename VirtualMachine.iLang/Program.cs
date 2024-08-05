using iLang.Compilers;
using iLang.Interpreter;
using iLang.Parsers;
using System;
using System.Diagnostics;
using VirtualMachine.Builder;
using VirtualMachine.Example.Register;
using VirtualMachine.Example.Stack;
using VirtualMachine.iLang.Extras;
using VirtualMachine.Processor;
using VirtualMachine.TypeDefs.Processor;

// get mode and file path from command line arguments
if (args.Length < 2)
{
    Console.WriteLine("Usage: VirtualMachine.iLang.exe <mode> <file> [-t]");
    return;
}

var tokens = args[0].Split(' ').Select(s => s.Replace("-", string.Empty)).ToList();
string[] modes = ["i", "cr", "cs"];

var mode = tokens.Find(word => modes.Contains(word));
var filePath = args[1];
var shouldTime = tokens.Contains("t");
var shouldDisassemble = tokens.Contains("d");
var shouldTrace = tokens.Contains("tr");


// read the file content
var code = System.IO.File.ReadAllText(filePath);


if (!modes.Contains(mode))
{
    Console.WriteLine("Invalid mode");
    return;
}

Parsers.ParseCompilationUnit(code, out var function);

ITimer<Stopwatch> watch = shouldTime ? NullTimer<Stopwatch>.Instance : new Timer<Stopwatch>();
switch (mode)
{
    case "-i":
        InterpreterRun(watch, function);
        break;
    case "-cr":
        RegisterRun(watch, function);
        break;
    case "-cs":
        StackRun(watch, function);
        break;
    default:
        Console.WriteLine("Invalid mode");
        break;

}
static object InterpreterRun(ITimer<Stopwatch> watch, iLang.SyntaxDefinitions.CompilationUnit function)
{
    Value result = Interpreter.Interpret(function, watch);
    return result;
}

static object RegisterRun(ITimer<Stopwatch> watch, iLang.SyntaxDefinitions.CompilationUnit function)
{
    var tracer_r = new Tracer<Registers>();
    byte[] program_r = iLang.Compilers.RegisterTarget.Compiler.Compile(function);

    IVirtualMachine<Registers> vm_r = new VirtualMachine.Example.Register.VirtualMachine();
    vm_r.LoadProgram(program_r);

    _ = vm_r.Run(tracer_r, watch);
    return vm_r.State.Holder[0];
}

static object StackRun(ITimer<Stopwatch> watch, iLang.SyntaxDefinitions.CompilationUnit function)
{
    var tracer_s = new Tracer<Stacks>();
    byte[] program_s = iLang.Compilers.StacksCompiler.Compiler.Compile(function);

    IVirtualMachine<Stacks> vm_s = new VirtualMachine.Example.Stack.VirtualMachine();
    vm_s.LoadProgram(program_s);

    _ = vm_s.Run(tracer_s, watch);

    return vm_s.State.Holder.Operands.LastOrDefault();
}