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

var code = @"
SumRangeRec : [a, b] => {
    if (a = b) then {
        return 0;
    } else {
        return a + SumRangeRec(a + 1, b);
    }
}; 

SumRangeLoop : [a, b] => {
    var sum <- 0;
    while (a < 100) do {
        if (a = b) then {
            return sum;
        } else {
            sum <- sum + a;
            a <- a + 1;
        }
    }
    return sum;
};

Main: SumRangeLoop(0, 11);
";
Parsers.ParseCompilationUnit(code, out var function);
InterpreterExample(new Timer<Stopwatch>(), function);
static void InterpreterExample(Timer<Stopwatch> watch, iLang.SyntaxDefinitions.CompilationUnit function)
{
    Value result = iLang.Interpreter.Interpreter.Interpret(function, watch);
    Console.WriteLine(result);
}

static void RegisterExample(Timer<Stopwatch> watch, iLang.SyntaxDefinitions.CompilationUnit function)
{
    var tracer_r = new Tracer<Registers>();
    byte[] program_r = iLang.Compilers.RegisterTarget.Compiler.Compile(function);

    Console.WriteLine(VirtualMachine.Builder.AssemblyBuilder<Registers>.Disassemble(program_r));

    IVirtualMachine<Registers> vm_r = new VirtualMachine.Example.Register.VirtualMachine();
    vm_r.LoadProgram(program_r);

    _ = vm_r.Run(tracer_r, watch);
}

static void StackExample(Timer<Stopwatch> watch, iLang.SyntaxDefinitions.CompilationUnit function)
{
    var tracer_s = new Tracer<Stacks>();
    byte[] program_s = iLang.Compilers.StacksCompiler.Compiler.Compile(function);
    
    Console.WriteLine(VirtualMachine.Builder.AssemblyBuilder<Stacks>.Disassemble(program_s));

    IVirtualMachine<Stacks> vm_s = new VirtualMachine.Example.Stack.VirtualMachine();
    vm_s.LoadProgram(program_s);

    _ = vm_s.Run(tracer_s, watch);
}