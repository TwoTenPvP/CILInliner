using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CILInliner
{
    internal static class OpCodesHelper
    {
        internal static Instruction ConvertInstruction(Instruction targetInstruction, ILProcessor processor, Instruction lastInstruction, Collection<VariableDefinition> allVars, int varOffset, int parameterVarOffset, bool isInstance)
        {
            // If the opcode is a return
            if (targetInstruction.OpCode == OpCodes.Ret)
            {
                // Replace with branch to final
                if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing ret with br to end of inline instruction");
                return processor.Create(OpCodes.Br, lastInstruction);
            }

            return CreateShortestInstruction(targetInstruction, allVars, varOffset, parameterVarOffset, processor, isInstance);
        }


        // Shrinks and applies offsets to variable loads and sets.
        // Also convert argument loads and sets to optimized variable loads and sets
        private static Instruction CreateShortestInstruction(Instruction instruction, Collection<VariableDefinition> variables, int targetVarOffset, int parameterVarOffset, ILProcessor processor, bool isInstance)
        {
            OpCode baseCode = OpCodes.Nop;
            int originalVar = -1;
            int targetVar = -1;

            if (instruction.OpCode == OpCodes.Ldloc_0)
            {
                targetVar = 0 + targetVarOffset;
                originalVar = 0;
                baseCode = OpCodes.Ldloc;
            }

            if (instruction.OpCode == OpCodes.Ldloc_1)
            {
                targetVar = 1 + targetVarOffset;
                originalVar = 1;
                baseCode = OpCodes.Ldloc;
            }

            if (instruction.OpCode == OpCodes.Ldloc_2)
            {
                targetVar = 2 + targetVarOffset;
                originalVar = 2;
                baseCode = OpCodes.Ldloc;
            }

            if (instruction.OpCode == OpCodes.Ldloc_3)
            {
                targetVar = 3 + targetVarOffset;
                originalVar = 3;
                baseCode = OpCodes.Ldloc;
            }

            if (instruction.OpCode == OpCodes.Ldloc_S)
            {
                targetVar = ((VariableDefinition)instruction.Operand).Index + targetVarOffset;
                originalVar = ((VariableDefinition)instruction.Operand).Index;
                baseCode = OpCodes.Ldloc;
            }

            if (instruction.OpCode == OpCodes.Ldloc)
            {
                targetVar = ((VariableDefinition)instruction.Operand).Index + targetVarOffset;
                originalVar = ((VariableDefinition)instruction.Operand).Index;
                baseCode = OpCodes.Ldloc;
            }

            if (baseCode == OpCodes.Ldloc && targetVar > -1)
            {
                if (targetVar == 0)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc_0 in target");
                    return processor.Create(OpCodes.Ldloc_0);
                }
                if (targetVar == 1)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc_1 in target");
                    return processor.Create(OpCodes.Ldloc_1);
                }

                if (targetVar == 2)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc_2 in target");
                    return processor.Create(OpCodes.Ldloc_2);
                }

                if (targetVar == 3)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc_3 in target");
                    return processor.Create(OpCodes.Ldloc_3);
                }

                if (targetVar <= byte.MaxValue)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc_S." + targetVar + " in target");
                    return processor.Create(OpCodes.Ldloc_S, variables[targetVar]);
                }

                if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc." + targetVar + " in target");
                return processor.Create(OpCodes.Ldloc, variables[targetVar]);
            }

            if (instruction.OpCode == OpCodes.Stloc_0)
            {
                targetVar = 0 + targetVarOffset;
                originalVar = 0;
                baseCode = OpCodes.Stloc;
            }

            if (instruction.OpCode == OpCodes.Stloc_1)
            {
                targetVar = 1 + targetVarOffset;
                originalVar = 1;
                baseCode = OpCodes.Stloc;
            }

            if (instruction.OpCode == OpCodes.Stloc_2)
            {
                targetVar = 2 + targetVarOffset;
                originalVar = 2;
                baseCode = OpCodes.Stloc;
            }

            if (instruction.OpCode == OpCodes.Stloc_3)
            {
                targetVar = 3 + targetVarOffset;
                originalVar = 3;
                baseCode = OpCodes.Stloc;
            }

            if (instruction.OpCode == OpCodes.Stloc_S)
            {
                targetVar = ((VariableDefinition)instruction.Operand).Index + targetVarOffset;
                originalVar = ((VariableDefinition)instruction.Operand).Index;
                baseCode = OpCodes.Stloc;
            }

            if (instruction.OpCode == OpCodes.Stloc)
            {
                targetVar = ((VariableDefinition)instruction.Operand).Index + targetVarOffset;
                originalVar = ((VariableDefinition)instruction.Operand).Index;
                baseCode = OpCodes.Stloc;
            }

            if (baseCode == OpCodes.Stloc && targetVar > -1)
            {
                if (targetVar == 0)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc_0 in target");
                    return processor.Create(OpCodes.Stloc_0);
                }

                if (targetVar == 1)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc_1 in target");
                    return processor.Create(OpCodes.Stloc_1);
                }

                if (targetVar == 2)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc_2 in target");
                    return processor.Create(OpCodes.Stloc_2);
                }

                if (targetVar == 3)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc_3 in target");
                    return processor.Create(OpCodes.Stloc_3);
                }

                if (targetVar <= byte.MaxValue)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc_S." + targetVar + " in target");
                    return processor.Create(OpCodes.Stloc_S, variables[targetVar]);
                }

                if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldloc_" + originalVar + " with Ldloc." + targetVar + " in target");
                return processor.Create(OpCodes.Stloc, variables[targetVar]);
            }

            if (instruction.OpCode == OpCodes.Ldarg_0)
            {
                if (!isInstance)
                {
                    targetVar = 0 + parameterVarOffset;
                    originalVar = 0;
                    baseCode = OpCodes.Ldarg;
                }
                else
                {
                    targetVar = variables.Count - 1;
                    originalVar = 0;
                    baseCode = OpCodes.Ldarg;
                }
            }

            if (instruction.OpCode == OpCodes.Ldarg_1)
            {
                targetVar = 1 + parameterVarOffset;
                originalVar = 1;
                baseCode = OpCodes.Ldarg;
            }

            if (instruction.OpCode == OpCodes.Ldarg_2)
            {
                targetVar = 2 + parameterVarOffset;
                originalVar = 2;
                baseCode = OpCodes.Ldarg;
            }

            if (instruction.OpCode == OpCodes.Ldarg_3)
            {
                targetVar = 3 + parameterVarOffset;
                originalVar = 3;
                baseCode = OpCodes.Ldarg;
            }

            if (instruction.OpCode == OpCodes.Ldarg_S)
            {
                if (!(isInstance && ((ParameterDefinition)instruction.Operand).Index == 0))
                {
                    targetVar = ((ParameterDefinition)instruction.Operand).Index + parameterVarOffset;
                    originalVar = ((ParameterDefinition)instruction.Operand).Index;
                    baseCode = OpCodes.Ldarg;
                }
                else
                {
                    targetVar = variables.Count - 1;
                    originalVar = ((ParameterDefinition)instruction.Operand).Index;
                    baseCode = OpCodes.Ldarg;
                }
            }

            if (instruction.OpCode == OpCodes.Ldarg)
            {
                if (!(isInstance && ((ParameterDefinition)instruction.Operand).Index == 0))
                {
                    targetVar = ((ParameterDefinition)instruction.Operand).Index + parameterVarOffset;
                    originalVar = ((ParameterDefinition)instruction.Operand).Index;
                    baseCode = OpCodes.Ldarg;
                }
                else
                {
                    targetVar = variables.Count - 1;
                    originalVar = 0;
                    baseCode = OpCodes.Ldarg;
                }
            }

            if (baseCode == OpCodes.Ldarg && targetVar > -1)
            {
                if (targetVar == 0)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldarg_" + originalVar + " with Ldloc_0 in target");
                    return processor.Create(OpCodes.Ldloc_0);
                }

                if (targetVar == 1)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldarg_" + originalVar + " with Ldloc_1 in target");
                    return processor.Create(OpCodes.Ldloc_1);
                }

                if (targetVar == 2)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldarg_" + originalVar + " with Ldloc_2 in target");
                    return processor.Create(OpCodes.Ldloc_2);
                }

                if (targetVar == 3)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldarg_" + originalVar + " with Ldloc_3 in target");
                    return processor.Create(OpCodes.Ldloc_3);
                }

                if (targetVar <= byte.MaxValue)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldarg_" + originalVar + " with Ldloc_S." + targetVar + " in target");
                    return processor.Create(OpCodes.Ldloc_S, variables[targetVar]);
                }

                if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Ldarg_" + originalVar + " with Ldloc." + targetVar + " in target");
                return processor.Create(OpCodes.Ldloc, variables[targetVar]);
            }

            if (instruction.OpCode == OpCodes.Starg_S)
            {
                if (!(isInstance && ((ParameterDefinition)instruction.Operand).Index == 0))
                {
                    targetVar = ((ParameterDefinition)instruction.Operand).Index + parameterVarOffset;
                    originalVar = ((ParameterDefinition)instruction.Operand).Index;
                    baseCode = OpCodes.Ldarg;
                }
                else
                {
                    targetVar = variables.Count - 1;
                    originalVar = ((ParameterDefinition)instruction.Operand).Index;
                    baseCode = OpCodes.Starg;
                }
            }

            if (instruction.OpCode == OpCodes.Starg)
            {
                if (!(isInstance && ((ParameterDefinition)instruction.Operand).Index == 0))
                {
                    targetVar = ((ParameterDefinition)instruction.Operand).Index + parameterVarOffset;
                    originalVar = ((ParameterDefinition)instruction.Operand).Index;
                    baseCode = OpCodes.Ldarg;
                }
                else
                {
                    targetVar = variables.Count - 1;
                    originalVar = 0;
                    baseCode = OpCodes.Starg;
                }
            }

            if (baseCode == OpCodes.Starg && targetVar > -1)
            {
                if (targetVar == 0)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Starg_" + originalVar + " with Stloc_0 in target");
                    return processor.Create(OpCodes.Stloc_0);
                }

                if (targetVar == 1)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Starg_" + originalVar + " with Stloc_1 in target");
                    return processor.Create(OpCodes.Stloc_1);
                }

                if (targetVar == 2)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Starg_" + originalVar + " with Stloc_2 in target");
                    return processor.Create(OpCodes.Stloc_2);
                }

                if (targetVar == 3)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Starg_" + originalVar + " with Stloc_3 in target");
                    return processor.Create(OpCodes.Stloc_3);
                }

                if (targetVar <= byte.MaxValue)
                {
                    if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Starg_" + originalVar + " with Stloc_S." + targetVar + " in target");
                    return processor.Create(OpCodes.Stloc_S, variables[targetVar]);
                }

                if (Program.LoadedOptions.Verbose) Console.WriteLine("Replacing Starg_" + originalVar + " with Stloc." + targetVar + " in target");
                return processor.Create(OpCodes.Stloc, variables[targetVar]);
            }

            return instruction;
        }
    }
}
