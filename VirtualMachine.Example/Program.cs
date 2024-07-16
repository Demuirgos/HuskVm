using VirtualMachine.Example.Stack;
using VirtualMachine.Example.Register;
using VirtualMachine.Instruction;
using VirtualMachine.Processor;
using VirtualMachine.Builder;


var s_program = AssemblyBuilder<Stack<int>>.Parse("push 2 push 3 add push 0 store");
var r_program = AssemblyBuilder<Registers>.Parse("mov 0 2 mov 1 3 add 2 1 0 mov 0 0 store 0 2");

var sb_program = new AssemblyBuilder<Stack<int>>()
    .Push(2)
    .Push(3)
    .Add()
    .Push(0)
    .Store();

var rb_program = new AssemblyBuilder<Registers>()
    .Mov(0, 2)
    .Mov(1, 3)
    .Add(2, 1, 0)
    .Mov(0, 0)
    .Store(0, 2);

IVirtualMachine<Stack<int>> s_vm = new VirtualMachine.Example.Stack.VirtualMachine();
IVirtualMachine<Registers> r_vm = new VirtualMachine.Example.Register.VirtualMachine();

s_vm.LoadProgram(sb_program.Build());

r_vm.LoadProgram(rb_program.Build());

Console.WriteLine(s_vm);
s_vm = s_vm.Run();
Console.WriteLine(s_vm);

Console.WriteLine(r_vm);
r_vm = r_vm.Run();
Console.WriteLine(r_vm);

string toHexString(byte[] bytes) => string.Join(" ", bytes.Select(b => b.ToString("X2")));