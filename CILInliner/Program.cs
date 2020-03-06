using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CommandLine;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CILInliner
{
    class Program
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Verbose logging", Default = false)]
            public bool Verbose { get; set; }

            [Option('f', "files", Required = true, HelpText = "Input files to be processed.", Min = 1)]
            public IEnumerable<string> InputFiles { get; set; }

            [Option('o', "output", Required = false, HelpText = "The output folder. Will overwrite the assemblies if not specified")]
            public string OutputFolder { get; set; }

            [Option('s', "size", Required = false, HelpText = "The maximum method size to inline in bytes", Default = 256)]
            public int MaxMethodSize { get; set; }

            public bool OverwriteAssemblies { get; set; }
        }

        public static Options LoadedOptions { get; private set; }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                LoadedOptions = o;

                if (LoadedOptions.InputFiles.Any())
                {
                    if (string.IsNullOrWhiteSpace(LoadedOptions.OutputFolder))
                    {
                        // Overwrite
                        LoadedOptions.OverwriteAssemblies = true;
                    }
                    else
                    {
                        // Dont overwrite
                        LoadedOptions.OverwriteAssemblies = false;

                        // Create output folder
                        if (!Directory.Exists(LoadedOptions.OutputFolder))
                        {
                            Directory.CreateDirectory(LoadedOptions.OutputFolder);
                        }
                    }

                    foreach (string path in LoadedOptions.InputFiles)
                    {
                        if (!File.Exists(path))
                        {
                            Console.WriteLine("Could not find assembly " + path);
                            Environment.Exit(1);
                            return;
                        }
                    }

                    List<AssemblyDefinition> assemblies = new List<AssemblyDefinition>();

                    // Read all assemblies to memory
                    foreach (string path in LoadedOptions.InputFiles)
                    {
                        assemblies.Add(AssemblyDefinition.ReadAssembly(path, new ReaderParameters()
                        {
                            InMemory = true
                        }));
                    }

                    if (LoadedOptions.Verbose) Console.WriteLine("Processing assemblies...");

                    ProcessAssemblies(assemblies);

                    if (LoadedOptions.Verbose) Console.WriteLine("Processed all assemblies");

                    for (int i = 0; i < assemblies.Count; i++)
                    {
                        if (LoadedOptions.OverwriteAssemblies)
                        {
                            if (LoadedOptions.Verbose) Console.WriteLine("Overwriting assembly at " + LoadedOptions.InputFiles.ElementAt(i));
                            assemblies[i].Write(LoadedOptions.InputFiles.ElementAt(i));
                        }
                        else
                        {
                            if (LoadedOptions.Verbose) Console.WriteLine("Saving assembly to " + Path.Combine(LoadedOptions.OutputFolder, Path.GetFileName(LoadedOptions.InputFiles.ElementAt(i))));
                            assemblies[i].Write(Path.Combine(LoadedOptions.OutputFolder, Path.GetFileName(LoadedOptions.InputFiles.ElementAt(i))));
                        }
                    }

                    Console.WriteLine("Done");
                    Environment.Exit(0);
                }
            });
        }

        private static void ProcessAssemblies(List<AssemblyDefinition> assemblies)
        {
            Dictionary<string, MethodDefinition> forceInlined = GetAllForceInlinedMethods(assemblies);

            foreach (AssemblyDefinition assembly in assemblies)
            {
                foreach (ModuleDefinition module in assembly.Modules)
                {
                    foreach (TypeDefinition type in module.Types)
                    {
                        foreach (MethodDefinition method in type.Methods)
                        {
                            if (method.HasBody)
                            {
                                while (true)
                                {
                                    bool foundInstruction = false;

                                    for (int x = 0; x < method.Body.Instructions.Count; x++)
                                    {
                                        Instruction instruction = method.Body.Instructions[x];

                                        // Check if this is a call instruction to a method that is to be inlined
                                        if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) && instruction.Operand is MethodReference targetMethodReference && forceInlined.TryGetValue(targetMethodReference.FullName, out MethodDefinition targetMethod))
                                        {
                                            // This is a call or callvirt instruction to a method with aggresive inlining

                                            // Get the next instruction. Every return will be replaced with a branch to this instruction.
                                            Instruction nextInstruction = method.Body.Instructions[x + 1];

                                            // Get the amount of variables originally in the method. Will be used as an offset
                                            int originalMethodVariables = method.Body.Variables.Count;

                                            // Add variables from target method
                                            for (int i = 0; i < targetMethod.Body.Variables.Count; i++)
                                            {
                                                if (LoadedOptions.Verbose) Console.WriteLine("Injecting " + targetMethod.Body.Variables[i].VariableType + " variable. (Local Variable)");
                                                method.Body.Variables.Add(new VariableDefinition(targetMethod.Body.Variables[i].VariableType));
                                            }

                                            // Get the offset of variables where the inlined methods variables start.
                                            // NOTE: Paramters will be added after this
                                            // NOTE: If the method is an instance method, there will be another variable after to hold the instance
                                            int argumentVariableOffset = method.Body.Variables.Count;

                                            // Add parameters as variables
                                            for (int i = 0; i < targetMethod.Parameters.Count; i++)
                                            {
                                                if (LoadedOptions.Verbose) Console.WriteLine("Injecting " + targetMethod.Parameters[i].ParameterType + " variable. (Parameter)");
                                                method.Body.Variables.Add(new VariableDefinition(targetMethod.Parameters[i].ParameterType));
                                            }

                                            // If the target method is of type instance. Create a variable for its instance and make it the last variable
                                            if (targetMethod.HasThis)
                                            {
                                                if (LoadedOptions.Verbose) Console.WriteLine("Injecting " + targetMethod.DeclaringType + " variable. (Instance)");
                                                method.Body.Variables.Add(new VariableDefinition(targetMethod.DeclaringType));
                                            }

                                            // Get IL processor of the caller
                                            ILProcessor processor = method.Body.GetILProcessor();

                                            if (targetMethod.Body.Instructions[0].OpCode == OpCodes.Ret)
                                            {
                                                // TODO: Could be a few nop then a ret
                                                // This method just returns straight away. Remove the call instruction
                                                processor.Remove(instruction);
                                            }
                                            else
                                            {
                                                // The target method doesnt return straight away.

                                                // Remove the call or callvirt instruction
                                                if (LoadedOptions.Verbose) Console.WriteLine("Removing call/virtcall instruction");
                                                processor.Remove(instruction);

                                                // Store the previous instruction, used to append instructions after
                                                Instruction previousInstruction = method.Body.Instructions[x - 1];

                                                // Inject load local arg vars from stack
                                                // Pop all the values from the stack into their args. (Arguments are on the stack)
                                                for (int i = targetMethod.Parameters.Count - 1; i >= 0; i--)
                                                {
                                                    // TODO: Optimize to smaller
                                                    if (LoadedOptions.Verbose) Console.WriteLine("Injecting Stloc." + (argumentVariableOffset + i) + " (Argument Load)");

                                                    Instruction stlocInstruction = processor.Create(OpCodes.Stloc, method.Body.Variables[argumentVariableOffset + i]);
                                                    processor.InsertAfter(previousInstruction, stlocInstruction);
                                                    previousInstruction = stlocInstruction;
                                                }

                                                if (targetMethod.HasThis)
                                                {
                                                    // TODO: Optimize smaller
                                                    if (LoadedOptions.Verbose) Console.WriteLine("Injecting Stloc." + (method.Body.Variables.Count - 1) + " (Type Instance Load)");

                                                    // Instruction to load the instance variable
                                                    Instruction stlocInstruction = processor.Create(OpCodes.Stloc, method.Body.Variables[method.Body.Variables.Count - 1]);
                                                    processor.InsertAfter(previousInstruction, stlocInstruction);
                                                    previousInstruction = stlocInstruction;
                                                }


                                                // Instructions that are completley replaced has to be replaced in any operands for jumps etc. Store them here
                                                Dictionary<Instruction, Instruction> instructionsToBeReplaced = new Dictionary<Instruction, Instruction>();

                                                // Iterate all instructions in the target method
                                                for (int i = 0; i < targetMethod.Body.Instructions.Count; i++)
                                                {
                                                    // Construct a convered instruction. Compensates for stack offsets etc
                                                    Instruction targetInstruction = OpCodesHelper.ConvertInstruction(targetMethod.Body.Instructions[i], processor, nextInstruction, method.Body.Variables, originalMethodVariables, argumentVariableOffset, targetMethod.HasThis);

                                                    if (targetMethod.Body.Instructions[i] != targetInstruction)
                                                    {
                                                        // This instruction was replaced. Add it to the replacement table
                                                        instructionsToBeReplaced.Add(targetMethod.Body.Instructions[i], targetInstruction);
                                                    }

                                                    // Insert the instruction
                                                    processor.InsertAfter(previousInstruction, targetInstruction);

                                                    // Save as previous instruction
                                                    previousInstruction = targetInstruction;
                                                }

                                                for (int i = 0; i < method.Body.Instructions.Count; i++)
                                                {
                                                    if (method.Body.Instructions[i].Operand is Instruction instructionToRemap && instructionsToBeReplaced.ContainsKey(instructionToRemap))
                                                    {
                                                        // Create a new instruction with the right operand (as to not destroy the old one)
                                                        processor.Replace(method.Body.Instructions[i], processor.Create(method.Body.Instructions[i].OpCode, instructionsToBeReplaced[instructionToRemap]));
                                                    }
                                                }
                                            }

                                            // Once we find an instruction. Break the loop and repeat
                                            foundInstruction = true;
                                            break;
                                        }
                                    }


                                    if (!foundInstruction)
                                    {
                                        // No instruction was found. Exit while
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static Dictionary<string, MethodDefinition> GetAllForceInlinedMethods(List<AssemblyDefinition> assemblies)
        {
            Dictionary<string, MethodDefinition> methods = new Dictionary<string, MethodDefinition>();

            foreach (AssemblyDefinition assembly in assemblies)
            {
                foreach (ModuleDefinition module in assembly.Modules)
                {
                    foreach (TypeDefinition type in module.Types)
                    {
                        foreach (MethodDefinition method in type.Methods)
                        {
                            if (method.HasBody)
                            {
                                if (method.AggressiveInlining)
                                {
                                    if (!method.NoInlining)
                                    {
                                        if (method.Body.CodeSize <= LoadedOptions.MaxMethodSize)
                                        {
                                            if (LoadedOptions.Verbose) Console.WriteLine("Discovered method for inlining " + method.FullName + ".");
                                            methods.Add(method.FullName, method);
                                        }
                                        else
                                        {
                                            if (LoadedOptions.Verbose) Console.WriteLine("Skipping method " + method.FullName + ". Method is too large. Size: " + method.Body.CodeSize);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return methods;
        }
    }
}
