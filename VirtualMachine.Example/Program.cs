using VirtualMachine.Example.Stack;
using VirtualMachine.Example.Register;
using VirtualMachine.Instruction;
using VirtualMachine.Processor;
using VirtualMachine.Builder;


var s_program = AssemblyBuilder<Stacks>.Parse("push 2 push 3 add push 0 push 1 store");
var r_program = AssemblyBuilder<Registers>.Parse("mov 0 2 mov 1 3 add 2 1 0 mov 0 0 store 0 2 1");

var sb_program = new AssemblyBuilder<Stacks>()
    .Push(2)
    .Push(3)
    .Add()
    .Push(0)
    .Push(1)
    .Store();

var rb_program = new AssemblyBuilder<Registers>()
    .Mov(0, 2)
    .Mov(1, 3)
    .Add(2, 1, 0)
    .Mov(0, 0)
    .Store(0, 2, 1);

byte[] bytecode = [0x01, 0x02, 0x00, 0x00, 0x00, 0x01, 0x03, 0x00, 0x00, 0x00, 0x03];
var program = AssemblyBuilder<Stacks>.Disassemble(bytecode);
Console.WriteLine(program);

IVirtualMachine<Stacks> s_vm = new VirtualMachine.Example.Stack.VirtualMachine();
IVirtualMachine<Registers> r_vm = new VirtualMachine.Example.Register.VirtualMachine();

s_vm.LoadProgram(sb_program.Build());
s_vm = s_vm.Run();

r_vm.LoadProgram(rb_program.Build());
r_vm = r_vm.Run();

string toHexString(byte[] bytes) => string.Join(" ", bytes.Select(b => b.ToString("X2")));