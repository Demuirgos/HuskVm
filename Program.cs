using VirtualMachine.Example;
using VirtualMachine.Processor;

IVirtualMachine<Stack<int>> vm = new StackVirtualMachine(VirtualMachine.Example.InstructionSet.Instructions);

vm.LoadProgram([0x01, 0x00, 0x00, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x03, 0x03]);

Console.WriteLine(vm);
vm = vm.Run();
Console.WriteLine(vm);

