using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace VirtualMachine.Generator {
    [Generator]
    public class BytecodeBuilderGenerator : ISourceGenerator
    {
        

        public void Execute(GeneratorExecutionContext context)
        {
            BuilderMachanics.EmitCode(context);
            SingletonBuilder.EmitCode(context);
        }

        

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}
