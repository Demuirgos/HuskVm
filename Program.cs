using VirtualMachine.Example;
using VirtualMachine.Example.Register;
using VirtualMachine.Processor;

IVirtualMachine<Stack<int>> s_vm = new VirtualMachine.Example.Stack.VirtualMachine(VirtualMachine.Example.Stack.InstructionSet.Instructions);
IVirtualMachine<Register> r_vm = new VirtualMachine.Example.Register.VirtualMachine(VirtualMachine.Example.Register.InstructionSet.Instructions);

s_vm.LoadProgram([
    0x01, 0x00, 0x00, 0x00, 0x02, 
    0x01, 0x00, 0x00, 0x00, 0x03, 
    0x03
]);

r_vm.LoadProgram([
    0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 
    0x01, 0x01, 0x00, 0x00, 0x00, 0x03, 
    0x02, 0x02, 0x01, 0x00 
]);

Console.WriteLine(s_vm);
s_vm = s_vm.Run();
Console.WriteLine(s_vm);

Console.WriteLine(r_vm);
r_vm = r_vm.Run();
Console.WriteLine(r_vm);

