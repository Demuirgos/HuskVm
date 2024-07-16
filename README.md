# Stack Based Virtual Machine using the HuskVM infra :
## Use :  
```cs
IVirtualMachine<Stack<int>> s_vm = new VirtualMachine.Example.Stack.VirtualMachine();
s_vm.LoadProgram([
    0x01, 0x00, 0x00, 0x00, 0x02, 
    0x01, 0x00, 0x00, 0x00, 0x03, 
    0x03,
    0x10,
    0x01, 0x00, 0x00, 0x00, 0x00,
    0x0f
]);
s_vm = s_vm.Run();
```
## Instructions : 
```cs
namespace Instructions {
    public class Push : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x01;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 4);
            span.Reverse();
            int value = BitConverter.ToInt32(span);
            state.ProgramCounter += 4;
            stack.Push(value);
            return vm;
        }
    }

    public class Pop : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x02;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            _ = stack.Pop();
            return vm;
        }
    }
   ...
    public class Dup : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x10;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Peek());
            return vm;
        }
    }
}
```

# Register Based Virtual Machine using the HuskVM infra :
## Use :  
```cs
IVirtualMachine<Registers> r_vm = new VirtualMachine.Example.Register.VirtualMachine();
r_vm.LoadProgram([
    0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 
    0x01, 0x01, 0x00, 0x00, 0x00, 0x03, 
    0x02, 0x02, 0x01, 0x00 ,
    0x0b, 0x02, 0x00
]);
r_vm = r_vm.Run();
```
## Instructions : 
```cs
namespace Instructions {
    public class Mov : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x01;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var Register = state.Program[state.ProgramCounter++];
            var span = state.Program.AsSpan(state.ProgramCounter, 4);
            span.Reverse();
            Registers[Register] = BitConverter.ToInt32(span);
            state.ProgramCounter += 4;
            return vm;
        }
    }

    public class Add : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x02;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 3);
            int Register = span[0];
            int value1 = Registers[span[1]];
            int value2 = Registers[span[2]];
            state.ProgramCounter += 3;
            Registers[Register] = value1 + value2;
            return vm;
        }
    }
    ...
    public class Halt : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0xff;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            vm.State.ProgramCounter = vm.State.Program.Length;
            return vm;
        }
    }
}
```

# Creating an Instance of VM type : 
```cs
// stack based VM
public class VirtualMachine() : BaseVirtualMachine<Stack<int>>(InstructionSet<Stack<int>>.Opcodes, new StackState()) ;

// register based VM
public class VirtualMachine() : BaseVirtualMachine<Registers>(InstructionSet<Registers>.Opcodes, new RegistersState());

```
# Using Bytecode Parser : 
```cs
// stack based VM
var s_program = AssemblyBuilder.Parse<Stack<int>>("push 2 push 3 add push 0 store");

// register based VM
var r_program = AssemblyBuilder.Parse<Registers>("mov 0 2 mov 1 3 add 2 1 0 mov 0 0 store 0 2");
```

# Using Bytecode Builder : [In Progress]
 