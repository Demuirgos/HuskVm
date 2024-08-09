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

var tokens = args.Select(s => s.Replace("-", string.Empty)).ToList();
string[] modes = ["i", "cr", "cs", "p"];

var mode = tokens.Find(word => modes.Contains(word));
var filePath = args[0];
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

if(mode == "p") {
    Console.WriteLine(function);
    return;
}

ITimer<Stopwatch> watch = shouldTime ? new Timer<Stopwatch>(): NullTimer<Stopwatch>.Instance;
object result = mode switch
{
    "i" => InterpreterRun(watch, function),
    "cr" => RegisterRun(watch, function),
    "cs" => StackRun(watch, function),
    _ => throw new Exception("Invalid mode")
};

Console.WriteLine(result);
if(shouldTime) Console.WriteLine(watch.Resource.ElapsedMilliseconds);

object InterpreterRun(ITimer<Stopwatch> watch, iLang.SyntaxDefinitions.CompilationUnit function)
{
    Value result = Interpreter.Interpret(function, watch);
    return result;
}

object RegisterRun(ITimer<Stopwatch> watch, iLang.SyntaxDefinitions.CompilationUnit function)
{
    ITracer<Registers> tracer_r = shouldTrace ? new Tracer<Registers>() : NullTracer<Registers>.Instance;
    byte[] program_r = iLang.Compilers.RegisterTarget.Compiler.Compile(function);

    if (shouldDisassemble) Console.WriteLine(AssemblyBuilder<Registers>.Disassemble(program_r));

    IVirtualMachine<Registers> vm_r = new VirtualMachine.Example.Register.VirtualMachine();
    vm_r.LoadProgram(program_r);

    if(shouldTrace)
    {
        vm_r.Trace(tracer_r, watch);
    } else
    {
        vm_r.Run(watch);
    }

    return vm_r.State.Holder[0];
}

object StackRun(ITimer<Stopwatch> watch, iLang.SyntaxDefinitions.CompilationUnit function)
{
    ITracer<Stacks> tracer_s = shouldTrace ? new Tracer<Stacks>() : NullTracer<Stacks>.Instance;
    byte[] program_s = iLang.Compilers.StacksCompiler.Compiler.Compile(function);

    if (shouldDisassemble) Console.WriteLine(AssemblyBuilder<Stacks>.Disassemble(program_s));

    IVirtualMachine<Stacks> vm_s = new VirtualMachine.Example.Stack.VirtualMachine();
    vm_s.LoadProgram(program_s);

    if(shouldTrace)
    {
        vm_s.Trace(tracer_s, watch);
    } else
    {
        vm_s.Run(watch);
    }

    return vm_s.State.Holder.Operands.LastOrDefault();
}