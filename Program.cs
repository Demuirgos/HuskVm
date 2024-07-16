using VirtualMachine.Example.Stack;
using VirtualMachine.Example.Register;
using VirtualMachine.Instruction;
using VirtualMachine.Processor;
using VirtualMachine.Builder;


var s_program = AssemblyBuilder.Parse<Stack<int>>("push 2 push 3 add push 0 store");
var r_program = AssemblyBuilder.Parse<Registers>("mov 0 2 mov 1 3 add 2 1 0 mov 0 0 store 0 2");

IVirtualMachine<Stack<int>> s_vm = new VirtualMachine.Example.Stack.VirtualMachine();
IVirtualMachine<Registers> r_vm = new VirtualMachine.Example.Register.VirtualMachine();

s_vm.LoadProgram(s_program);

r_vm.LoadProgram(r_program);

Console.WriteLine(s_vm);
s_vm = s_vm.Run();
Console.WriteLine(s_vm);

Console.WriteLine(r_vm);
r_vm = r_vm.Run();
Console.WriteLine(r_vm);

