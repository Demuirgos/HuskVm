//#define BENCHMARK
#if BENCHMARK
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using iLang.Interpreter;
using iLang.Parsers;
using iLang.SyntaxDefinitions;
using System.Collections;
using System.Diagnostics;
using VirtualMachine.Example.Register;
using VirtualMachine.Example.Stack;
using VirtualMachine.Processor;
using VirtualMachine.TypeDefs.Processor;

BenchmarkRunner.Run<VirtualMachineWar>();

[MemoryDiagnoser]
public class VirtualMachineWar
{
    private string FilePath { get; set; } = "main.il";
    private CompilationUnit compilationUnit;
    private byte[] registerBytes;
    private byte[] stackBytes;
    IVirtualMachine<Registers> vm_r = new VirtualMachine.Example.Register.VirtualMachine();
    IVirtualMachine<Stacks> vm_s = new VirtualMachine.Example.Stack.VirtualMachine();

    public VirtualMachineWar()
    {
        var code = System.IO.File.ReadAllText(FilePath);
        Parsers.ParseCompilationUnit(code, out compilationUnit);
        registerBytes = iLang.Compilers.RegisterTarget.Compiler.Compile(compilationUnit);
        stackBytes = iLang.Compilers.StacksCompiler.Compiler.Compile(compilationUnit);
    }

    [Benchmark]
    public IVirtualMachine<Registers> RegisterVm() => vm_r.LoadProgram(registerBytes).Run(NullTimer<Stopwatch>.Instance);

    [Benchmark]
    public IVirtualMachine<Stacks> StackVm() => vm_s.LoadProgram(stackBytes).Run(NullTimer<Stopwatch>.Instance);

    [Benchmark]
    public Value InterpreterVm() => Interpreter.Interpret(compilationUnit, NullTimer<Stopwatch>.Instance);
}
#else
using iLang.Compilers;
using iLang.Interpreter;
using iLang.Parsers;
using iLang.SyntaxDefinitions;
using System;
using System.Diagnostics;
using VirtualMachine.Builder;
using VirtualMachine.Example.Register;
using VirtualMachine.Example.Stack;
using VirtualMachine.iLang.Checker;
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
string[] modes = ["i", "clr", "cr", "cs", "p"];

var mode = tokens.Find(word => modes.Contains(word));
var filePath = args[0];
var shouldTime = tokens.Contains("t");
var shouldDisassemble = tokens.Contains("d");
var shouldTrace = tokens.Contains("tr");
var shouldAot = tokens.Contains("aot");

// read the file content
var code = System.IO.File.ReadAllText(filePath);


if (!modes.Contains(mode))
{
    Console.WriteLine("Invalid mode");
    return;
}

if(!Parsers.ParseCompilationUnit(code, out CompilationUnit function))
{
    Console.WriteLine("Parsing failed");
    return;
}

if(!TypeChecker.Check(function))
{
    Console.WriteLine("Type checking failed");
    return;
}

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
    "clr" => dotnetRun(watch, function),
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

    if (shouldAot)
    {
        var methodInfo = iLang.Compilers.RegisterTarget.Compiler.ToClr.ToMethodInfo(program_r);
        return methodInfo(shouldTrace);
    }

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

    if (shouldAot)
    {
        var methodInfo = iLang.Compilers.StacksCompiler.Compiler.ToClr.ToMethodInfo(program_s);
        return methodInfo(shouldTrace);
    }

    IVirtualMachine<Stacks> vm_s = new VirtualMachine.Example.Stack.VirtualMachine();
    vm_s.LoadProgram(program_s);

    if (shouldTrace)
    {
        vm_s.Trace(tracer_s, watch);
    }
    else
    {
        vm_s.Run(watch);
    }

    return vm_s.State.Holder.Operands.LastOrDefault();
}
object dotnetRun(ITimer<Stopwatch> watch, iLang.SyntaxDefinitions.CompilationUnit function)
{
    Func<double> program_c = iLang.Compilers.CLRTarget.Compile(function, logILCode: shouldDisassemble);
    var result = program_c();
    return result;
}
#endif