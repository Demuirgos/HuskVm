#define REGISTER
using iLang.Compilers;
using iLang.Parsers;
using System;
using VirtualMachine.Builder;
#if REGISTER 
using VirtualMachine.Example.Register;
using VirtualMachine.Processor;

var code = @"
SumRangeRec : [a, b] => {
    if (a = b) then {
        return 0;
    } else {
        return a + SumRangeRec(a + 1, b);
    }
}; 

Main: SumRangeRec(0, 11);
";

Parsers.ParseCompilationUnit(code, out var function);
byte[] program = iLang.Compilers.RegisterTarget.Compiler.Compile(function);
IVirtualMachine<Registers> vm = new VirtualMachine.Example.Register.VirtualMachine();
Console.WriteLine(AssemblyBuilder<Registers>.Disassemble(program));
vm.LoadProgram(program);
Console.WriteLine(vm.Run());
#else 
using VirtualMachine.Example.Stack;
using VirtualMachine.Processor;

var code = @"
SumRangeRec : [a, b] => {
    if (a = b) then {
        return 0;
    } else {
        return a + SumRangeRec(a + 1, b);
    }
}; 

Main: SumRangeRec(0, 11);
";

Parsers.ParseCompilationUnit(code, out var function);
byte[] program = iLang.Compilers.StacksCompiler.Compiler.Compile(function);
IVirtualMachine<Stacks> vm = new VirtualMachine.Example.Stack.VirtualMachine();
Console.WriteLine(AssemblyBuilder<Stacks>.Disassemble(program));
vm.LoadProgram(program);
Console.WriteLine(vm.Run());
#endif
