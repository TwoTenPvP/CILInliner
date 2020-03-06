# CILInliner
CILInliner is a CLI tool for manually inlining CIL assemblies.

Useful to get extra control where you know you are not causing cache misses or for JIT without inlining support.

## Features
* Inlines methods with the attribute ``[MethodImpl(MethodImplOptions.AggressiveInlining)]``
* Inlines call and virtcall instructions
* Leaves the original method intact for external assemblies to use
* Allows specifying max code size to limit cache misses
* Works on multiple assemblies at once


## Results

Original source:

```csharp
internal class Program
{
    private static int myX = 10;

    public static void MyMethod()
    {
        int num = 0;
        int num2 = 0;
        if (Add(myX, IPAddress.Parse("127.0.0.1")))
        {
            Console.WriteLine("1");
        }
        else
        {
            Console.WriteLine("2");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Add(int elements, IPAddress addr)
    {
        myX += elements;
        Console.WriteLine(addr.ToString());
        for (int i = 0; i < elements; i++)
        {
            Console.WriteLine(i);
        }
        if (elements == 10)
        {
            return true;
        }
        return false;
    }
}
```

Original CIL:

```
.method public hidebysig static
    void MyMethod () cil managed
{
    // Method begins at RVA 0x2134
    // Code size 60 (0x3c)
    .maxstack 2
    .locals init (
        [0] int32,
        [1] int32,
        [2] bool,
        [3] bool
    )

    IL_0000: nop
    IL_0001: ldc.i4.0
    IL_0002: stloc.0
    IL_0003: ldc.i4.0
    IL_0004: stloc.1
    IL_0005: ldsfld int32 CILInliner.TestTarget.Program::myX
    IL_000a: ldstr "127.0.0.1"
    IL_000f: call class [System.Net.Primitives]System.Net.IPAddress [System.Net.Primitives]System.Net.IPAddress::Parse(string)
    IL_0014: call bool CILInliner.TestTarget.Program::Add(int32, class [System.Net.Primitives]System.Net.IPAddress)
    IL_0019: stloc.2
    IL_001a: ldloc.2
    IL_001b: stloc.3
    IL_001c: ldloc.3
    IL_001d: brfalse.s IL_002e

    IL_001f: nop
    IL_0020: ldstr "1"
    IL_0025: call void [System.Console]System.Console::WriteLine(string)
    IL_002a: nop
    IL_002b: nop
    IL_002c: br.s IL_003b

    IL_002e: nop
    IL_002f: ldstr "2"
    IL_0034: call void [System.Console]System.Console::WriteLine(string)
    IL_0039: nop
    IL_003a: nop

    IL_003b: ret
} // end of method Program::MyMethod


.method public hidebysig static
    bool Add (
        int32 elements,
        class [System.Net.Primitives]System.Net.IPAddress addr
    ) cil managed aggressiveinlining
{
    // Method begins at RVA 0x21c4
    // Code size 71 (0x47)
    .maxstack 2
    .locals init (
        [0] int32,
        [1] bool,
        [2] bool,
        [3] bool
    )

    IL_0000: nop
    IL_0001: ldsfld int32 CILInliner.TestTarget.Program::myX
    IL_0006: ldarg.0
    IL_0007: add
    IL_0008: stsfld int32 CILInliner.TestTarget.Program::myX
    IL_000d: ldarg.1
    IL_000e: callvirt instance string [System.Runtime]System.Object::ToString()
    IL_0013: call void [System.Console]System.Console::WriteLine(string)
    IL_0018: nop
    IL_0019: ldc.i4.0
    IL_001a: stloc.0
    IL_001b: br.s IL_002a
    // loop start (head: IL_002a)
        IL_001d: nop
        IL_001e: ldloc.0
        IL_001f: call void [System.Console]System.Console::WriteLine(int32)
        IL_0024: nop
        IL_0025: nop
        IL_0026: ldloc.0
        IL_0027: ldc.i4.1
        IL_0028: add
        IL_0029: stloc.0

        IL_002a: ldloc.0
        IL_002b: ldarg.0
        IL_002c: clt
        IL_002e: stloc.1
        IL_002f: ldloc.1
        IL_0030: brtrue.s IL_001d
    // end loop

    IL_0032: ldarg.0
    IL_0033: ldc.i4.s 10
    IL_0035: ceq
    IL_0037: stloc.2
    IL_0038: ldloc.2
    IL_0039: brfalse.s IL_0040

    IL_003b: nop
    IL_003c: ldc.i4.1
    IL_003d: stloc.3
    IL_003e: br.s IL_0045

    IL_0040: nop
    IL_0041: ldc.i4.0
    IL_0042: stloc.3
    IL_0043: br.s IL_0045

    IL_0045: ldloc.3
    IL_0046: ret
} // end of method Program::Add
```

Resulting source:

```csharp
internal class Program
{
    private static int myX = 10;

    public static void MyMethod()
    {
        int num = 0;
        int num2 = 0;
        int num3 = myX;
        IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
        int num4 = num3;
        myX += num4;
        Console.WriteLine(iPAddress.ToString());
        for (int i = 0; i < num4; i++)
        {
            Console.WriteLine(i);
        }
        if (num4 == 10)
        {
            Console.WriteLine("1");
        }
        else
        {
            Console.WriteLine("2");
        }
    }


    // Add method is unchanged in case other assemblies use it.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Add(int elements, IPAddress addr)
    {
        myX += elements;
        Console.WriteLine(addr.ToString());
        for (int i = 0; i < elements; i++)
        {
            Console.WriteLine(i);
        }
        if (elements == 10)
        {
            return true;
        }
        return false;
    }
}
```

Resuling CIL:

```
.method public hidebysig static
    void MyMethod () cil managed
{
    // Method begins at RVA 0x2144
    // Code size 154 (0x9a)
    .maxstack 2
    .locals init (
        [0] int32,
        [1] int32,
        [2] bool,
        [3] bool,
        [4] int32,
        [5] bool,
        [6] bool,
        [7] bool,
        [8] int32,
        [9] class [System.Net.Primitives]System.Net.IPAddress
    )

    IL_0000: nop
    IL_0001: ldc.i4.0
    IL_0002: stloc.0
    IL_0003: ldc.i4.0
    IL_0004: stloc.1
    IL_0005: ldsfld int32 UnityInliner.TestTarget.Program::myX
    IL_000a: ldstr "127.0.0.1"
    IL_000f: call class [System.Net.Primitives]System.Net.IPAddress [System.Net.Primitives]System.Net.IPAddress::Parse(string)
    IL_0014: stloc 9
    IL_0018: stloc 8
    IL_001c: nop
    IL_001d: ldsfld int32 UnityInliner.TestTarget.Program::myX
    IL_0022: ldloc.s 8
    IL_0024: add
    IL_0025: stsfld int32 UnityInliner.TestTarget.Program::myX
    IL_002a: ldloc.s 9
    IL_002c: callvirt instance string [System.Runtime]System.Object::ToString()
    IL_0031: call void [System.Console]System.Console::WriteLine(string)
    IL_0036: nop
    IL_0037: ldc.i4.0
    IL_0038: stloc.s 4
    IL_003a: br.s IL_004c
    // loop start (head: IL_004c)
        IL_003c: nop
        IL_003d: ldloc.s 4
        IL_003f: call void [System.Console]System.Console::WriteLine(int32)
        IL_0044: nop
        IL_0045: nop
        IL_0046: ldloc.s 4
        IL_0048: ldc.i4.1
        IL_0049: add
        IL_004a: stloc.s 4

        IL_004c: ldloc.s 4
        IL_004e: ldloc.s 8
        IL_0050: clt
        IL_0052: stloc.s 5
        IL_0054: ldloc.s 5
        IL_0056: brtrue.s IL_003c
    // end loop

    IL_0058: ldloc.s 8
    IL_005a: ldc.i4.s 10
    IL_005c: ceq
    IL_005e: stloc.s 6
    IL_0060: ldloc.s 6
    IL_0062: brfalse.s IL_006a

    IL_0064: nop
    IL_0065: ldc.i4.1
    IL_0066: stloc.s 7
    IL_0068: br.s IL_0070

    IL_006a: nop
    IL_006b: ldc.i4.0
    IL_006c: stloc.s 7
    IL_006e: br.s IL_0070

    IL_0070: ldloc.s 7
    IL_0072: br IL_0077

    IL_0077: stloc.2
    IL_0078: ldloc.2
    IL_0079: stloc.3
    IL_007a: ldloc.3
    IL_007b: brfalse.s IL_008c

    IL_007d: nop
    IL_007e: ldstr "1"
    IL_0083: call void [System.Console]System.Console::WriteLine(string)
    IL_0088: nop
    IL_0089: nop
    IL_008a: br.s IL_0099

    IL_008c: nop
    IL_008d: ldstr "2"
    IL_0092: call void [System.Console]System.Console::WriteLine(string)
    IL_0097: nop
    IL_0098: nop

    IL_0099: ret
} // end of method Program::MyMethod
```
