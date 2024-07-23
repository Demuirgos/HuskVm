using iLang.Compilers;
using iLang.Parsers;
using System;
using VirtualMachine.Builder;
using VirtualMachine.Example.Register;
using VirtualMachine.Example.Stack;
using VirtualMachine.Processor;

var code = @"
SumRangeLoop : [a, b] => {
    var sum <- 0;
    while (a < b) do {
        sum <- sum + a;
        a <- a + 1;
    }
    return sum;
}; 

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