public class SimpleClass
{
  public void VoidMethodPatched(/*Parameter with token 08000007*/int count)
  // .maxstack 9
  // .locals init (
  // [0] bool V_0,
    // [1] object 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0016 ldloca.s))]'
  // )
  //
  // IL_0000: nop
  // IL_0001: ldtoken      UserQuery/SimpleClass
  // IL_0006: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_000b: ldarg.0      // this
  // IL_000c: ldtoken      [mscorlib]System.Void
  // IL_0011: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_0016: ldloca.s     'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0016 ldloca.s))]'
  // IL_0018: ldsfld       class [mscorlib]System.Type[] [mscorlib]System.Type::EmptyTypes
  // IL_001d: ldc.i4.1
  // IL_001e: newarr       [mscorlib]System.Object
  // IL_0023: dup
  // IL_0024: ldc.i4.0
  // IL_0025: ldarg.1      // count
  // IL_0026: box          [mscorlib]System.Int32
  // IL_002b: stelem.ref
  // IL_002c: call         bool UserQuery/PatchedAssemblyBridge::TryMock(class [mscorlib]System.Type, object, class [mscorlib]System.Type, object&, class [mscorlib]System.Type[], object[])
  // IL_0031: stloc.0      // V_0
  // IL_0032: ldloc.0      // V_0
  // IL_0033: brfalse.s    IL_0037
  // IL_0035: br.s         IL_0045
  // ---------
  // IL_0037: ldarg.0      // this
  // IL_0038: ldarg.0      // this
  // IL_0039: ldfld        int32 UserQuery/SimpleClass::Modified
  // IL_003e: ldarg.1      // count
  // IL_003f: add
  // IL_0040: stfld        int32 UserQuery/SimpleClass::Modified
  // IL_0045: ret
  //
  {
    object mockedReturnValue;
    if (UserQuery.PatchedAssemblyBridge.TryMock(typeof (UserQuery.SimpleClass), (object) this, typeof (void), out mockedReturnValue, Type.EmptyTypes, new object[1]
    {
      (object) count
    }))
      return;
    this.Modified = this.Modified + count;
  }

  /*Method VoidMethod with token 06000006*/
  // .method public hidebysig instance void
    // VoidMethod(
      // int32 count
    // ) cil managed
  public void VoidMethod(/*Parameter with token 08000008*/int count)
  // .maxstack 8
  //
  // IL_0000: nop
  // IL_0001: ldarg.0      // this
  // IL_0002: ldarg.0      // this
  // IL_0003: ldfld        int32 UserQuery/SimpleClass::Modified
  // IL_0008: ldarg.1      // count
  // IL_0009: add
  // IL_000a: stfld        int32 UserQuery/SimpleClass::Modified
  // IL_000f: ret
  //
  {
    this.Modified = this.Modified + count;
  }

  /*Method ReturnMethodPatched with token 06000007*/
  // .method public hidebysig instance int32
    // ReturnMethodPatched(
      // int32 count
    // ) cil managed
  public int ReturnMethodPatched(/*Parameter with token 08000009*/int count)
  // .maxstack 9
  // .locals init (
  // [0] object 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]',
    // [1] bool V_1,
    // [2] int32 V_2,
    // [3] int32 V_3
  // )
  //
  // IL_0000: nop
  // IL_0001: ldtoken      UserQuery/SimpleClass
  // IL_0006: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_000b: ldarg.0      // this
  // IL_000c: ldtoken      [mscorlib]System.Int32
  // IL_0011: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_0016: ldloca.s     'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]'
  // IL_0018: ldsfld       class [mscorlib]System.Type[] [mscorlib]System.Type::EmptyTypes
  // IL_001d: ldc.i4.1
  // IL_001e: newarr       [mscorlib]System.Object
  // IL_0023: dup
  // IL_0024: ldc.i4.0
  // IL_0025: ldarg.1      // count
  // IL_0026: box          [mscorlib]System.Int32
  // IL_002b: stelem.ref
  // IL_002c: call         bool UserQuery/PatchedAssemblyBridge::TryMock(class [mscorlib]System.Type, object, class [mscorlib]System.Type, object&, class [mscorlib]System.Type[], object[])
  // IL_0031: stloc.1      // V_1
  // IL_0032: ldloc.1      // V_1
  // IL_0033: brfalse.s    IL_003e
  // IL_0035: ldloc.0      // 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]'
  // IL_0036: unbox.any    [mscorlib]System.Int32
  // IL_003b: stloc.2      // V_2
  // IL_003c: br.s         IL_0052
  // ---------
  // IL_003e: ldarg.0      // this
  // IL_003f: ldarg.0      // this
  // IL_0040: ldfld        int32 UserQuery/SimpleClass::Modified
  // IL_0045: ldarg.1      // count
  // IL_0046: add
  // IL_0047: dup
  // IL_0048: stloc.3      // V_3
  // IL_0049: stfld        int32 UserQuery/SimpleClass::Modified
  // IL_004e: ldloc.3      // V_3
  // IL_004f: stloc.2      // V_2
  // IL_0050: br.s         IL_0052
  // IL_0052: ldloc.2      // V_2
  // IL_0053: ret
  //
  {
    object mockedReturnValue;
    if (UserQuery.PatchedAssemblyBridge.TryMock(typeof (UserQuery.SimpleClass), (object) this, typeof (int), out mockedReturnValue, Type.EmptyTypes, new object[1]
    {
      (object) count
    }))
      return (int) mockedReturnValue;
    return this.Modified = this.Modified + count;
  }

  /*Method ReturnMethod with token 06000008*/
  // .method public hidebysig instance int32
    // ReturnMethod(
      // int32 count
    // ) cil managed
  public int ReturnMethod(/*Parameter with token 0800000A*/int count)
  // .maxstack 3
  // .locals init (
  // [0] int32 V_0,
    // [1] int32 V_1
  // )
  //
  // IL_0000: nop
  // IL_0001: ldarg.0      // this
  // IL_0002: ldarg.0      // this
  // IL_0003: ldfld        int32 UserQuery/SimpleClass::Modified
  // IL_0008: ldarg.1      // count
  // IL_0009: add
  // IL_000a: dup
  // IL_000b: stloc.0      // V_0
  // IL_000c: stfld        int32 UserQuery/SimpleClass::Modified
  // IL_0011: ldloc.0      // V_0
  // IL_0012: stloc.1      // V_1
  // IL_0013: br.s         IL_0015
  // IL_0015: ldloc.1      // V_1
  // IL_0016: ret
  //
  {
    return this.Modified = this.Modified + count;
  }

  /*Method VoidGenericMethodPatched with token 06000009*/
  // .method public hidebysig instance void
    // VoidGenericMethodPatched<T>(
      // !!0/*T*/ count
    // ) cil managed
  public void VoidGenericMethodPatched</*Generic argument with token 2A000002*/T>(/*Parameter with token 0800000B*/T count)
  // .maxstack 9
  // .locals init (
  // [0] object 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0016 ldloca.s))]',
    // [1] bool V_1
  // )
  //
  // IL_0000: nop
  // IL_0001: ldtoken      UserQuery/SimpleClass
  // IL_0006: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_000b: ldarg.0      // this
  // IL_000c: ldtoken      [mscorlib]System.Int32
  // IL_0011: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_0016: ldloca.s     'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0016 ldloca.s))]'
  // IL_0018: ldsfld       class [mscorlib]System.Type[] [mscorlib]System.Type::EmptyTypes
  // IL_001d: ldc.i4.1
  // IL_001e: newarr       [mscorlib]System.Object
  // IL_0023: dup
  // IL_0024: ldc.i4.0
  // IL_0025: ldarg.1      // count
  // IL_0026: box          !!0/*T*/
  // IL_002b: stelem.ref
  // IL_002c: call         bool UserQuery/PatchedAssemblyBridge::TryMock(class [mscorlib]System.Type, object, class [mscorlib]System.Type, object&, class [mscorlib]System.Type[], object[])
  // IL_0031: stloc.1      // V_1
  // IL_0032: ldloc.1      // V_1
  // IL_0033: brfalse.s    IL_0037
  // IL_0035: br.s         IL_0045
  // IL_0037: ldarg.0      // this
  // IL_0038: ldarg.0      // this
  // IL_0039: ldfld        int32 UserQuery/SimpleClass::Modified
  // IL_003e: ldc.i4.1
  // IL_003f: add
  // IL_0040: stfld        int32 UserQuery/SimpleClass::Modified
  // IL_0045: ret
  //
  {
    object mockedReturnValue;
    if (UserQuery.PatchedAssemblyBridge.TryMock(typeof (UserQuery.SimpleClass), (object) this, typeof (int), out mockedReturnValue, Type.EmptyTypes, new object[1]
    {
      (object) count
    }))
      return;
    this.Modified = this.Modified + 1;
  }

  /*Method VoidGenericMethod with token 0600000A*/
  // .method public hidebysig instance void
    // VoidGenericMethod<T>(
      // !!0/*T*/ count
    // ) cil managed
  public void VoidGenericMethod</*Generic argument with token 2A000003*/T>(/*Parameter with token 0800000C*/T count)
  // .maxstack 8
  //
  // IL_0000: nop
  // IL_0001: ldarg.0      // this
  // IL_0002: ldarg.0      // this
  // IL_0003: ldfld        int32 UserQuery/SimpleClass::Modified
  // IL_0008: ldc.i4.1
  // IL_0009: add
  // IL_000a: stfld        int32 UserQuery/SimpleClass::Modified
  // IL_000f: ret
  //
  {
    this.Modified = this.Modified + 1;
  }

  /*Method GenericMethodPatched with token 0600000B*/
  // .method public hidebysig instance !!0/*T*/
    // GenericMethodPatched<T>(
      // !!0/*T*/ count
    // ) cil managed
  public T GenericMethodPatched</*Generic argument with token 2A000004*/T>(/*Parameter with token 0800000D*/T count)
  // .maxstack 9
  // .locals init (
  // [0] object 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]',
    // [1] bool V_1,
    // [2] !!0/*T*/ V_2
  // )
  //
  // IL_0000: nop
  // IL_0001: ldtoken      UserQuery/SimpleClass
  // IL_0006: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_000b: ldarg.0      // this
  // IL_000c: ldtoken      [mscorlib]System.Int32
  // IL_0011: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_0016: ldloca.s     'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]'
  // IL_0018: ldsfld       class [mscorlib]System.Type[] [mscorlib]System.Type::EmptyTypes
  // IL_001d: ldc.i4.1
  // IL_001e: newarr       [mscorlib]System.Object
  // IL_0023: dup
  // IL_0024: ldc.i4.0
  // IL_0025: ldarg.1      // count
  // IL_0026: box          !!0/*T*/
  // IL_002b: stelem.ref
  // IL_002c: call         bool UserQuery/PatchedAssemblyBridge::TryMock(class [mscorlib]System.Type, object, class [mscorlib]System.Type, object&, class [mscorlib]System.Type[], object[])
  // IL_0031: stloc.1      // V_1
  // IL_0032: ldloc.1      // V_1
  // IL_0033: brfalse.s    IL_003e
  // IL_0035: ldloc.0      // 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]'
  // IL_0036: unbox.any    !!0/*T*/
  // IL_003b: stloc.2      // V_2
  // IL_003c: br.s         IL_0050
  // IL_003e: ldarg.0      // this
  // IL_003f: ldarg.0      // this
  // IL_0040: ldfld        int32 UserQuery/SimpleClass::Modified
  // IL_0045: ldc.i4.1
  // IL_0046: add
  // IL_0047: stfld        int32 UserQuery/SimpleClass::Modified
  // IL_004c: ldarg.1      // count
  // IL_004d: stloc.2      // V_2
  // IL_004e: br.s         IL_0050
  // IL_0050: ldloc.2      // V_2
  // IL_0051: ret
  //
  {
    object mockedReturnValue;
    if (UserQuery.PatchedAssemblyBridge.TryMock(typeof (UserQuery.SimpleClass), (object) this, typeof (int), out mockedReturnValue, Type.EmptyTypes, new object[1]
    {
      (object) count
    }))
      return (T) mockedReturnValue;
    this.Modified = this.Modified + 1;
    return count;
  }

  /*Method GenericMethod with token 0600000C*/
  // .method public hidebysig instance !!0/*T*/
    // GenericMethod<T>(
      // !!0/*T*/ count
    // ) cil managed
  public T GenericMethod</*Generic argument with token 2A000005*/T>(/*Parameter with token 0800000E*/T count)
  // .maxstack 3
  // .locals init (
  // [0] !!0/*T*/ V_0
  // )
  //
  // IL_0000: nop
  // IL_0001: ldarg.0      // this
  // IL_0002: ldarg.0      // this
  // IL_0003: ldfld        int32 UserQuery/SimpleClass::Modified
  // IL_0008: ldc.i4.1
  // IL_0009: add
  // IL_000a: stfld        int32 UserQuery/SimpleClass::Modified
  // IL_000f: ldarg.1      // count
  // IL_0010: stloc.0      // V_0
  // IL_0011: br.s         IL_0013
  // IL_0013: ldloc.0      // V_0
  // IL_0014: ret
  //
  {
    this.Modified = this.Modified + 1;
    return count;
  }

  /*Method .ctor with token 0600000D*/
  // .method public hidebysig specialname rtspecialname instance void
    // .ctor() cil managed
  public SimpleClass()
  // .maxstack 8
  //
  // IL_0000: ldarg.0      // this
  // IL_0001: call         instance void [mscorlib]System.Object::.ctor()
  // IL_0006: nop
  // IL_0007: ret
  //
  {
    base.\u002Ector();
  }
}

/*Type GenericClass`1 with token 02000006*/
// .class nested public auto ansi beforefieldinit
  // GenericClass`1<TC>
    // extends [mscorlib]System.Object
//
public class GenericClass</*Generic argument with token 2A000001*/TC>
{
  /*Field Modified with token 04000002*/
  // .field public int32 Modified
  public int Modified;

  /*Method VoidMethod with token 0600000E*/
  // .method public hidebysig instance void
    // VoidMethod(
      // int32 count
    // ) cil managed
  public void VoidMethod(/*Parameter with token 0800000F*/int count)
  // .maxstack 9
  // .locals init (
  // [0] bool V_0,
    // [1] object 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0016 ldloca.s))]'
  // )
  //
  // IL_0000: nop
  // IL_0001: ldtoken      UserQuery/SimpleClass
  // IL_0006: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_000b: ldarg.0      // this
  // IL_000c: ldtoken      [mscorlib]System.Void
  // IL_0011: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_0016: ldloca.s     'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0016 ldloca.s))]'
  // IL_0018: ldsfld       class [mscorlib]System.Type[] [mscorlib]System.Type::EmptyTypes
  // IL_001d: ldc.i4.1
  // IL_001e: newarr       [mscorlib]System.Object
  // IL_0023: dup
  // IL_0024: ldc.i4.0
  // IL_0025: ldarg.1      // count
  // IL_0026: box          [mscorlib]System.Int32
  // IL_002b: stelem.ref
  // IL_002c: call         bool UserQuery/PatchedAssemblyBridge::TryMock(class [mscorlib]System.Type, object, class [mscorlib]System.Type, object&, class [mscorlib]System.Type[], object[])
  // IL_0031: stloc.0      // V_0
  // IL_0032: ldloc.0      // V_0
  // IL_0033: brfalse.s    IL_0037
  // IL_0035: br.s         IL_0045
  // IL_0037: ldarg.0      // this
  // IL_0038: ldarg.0      // this
  // IL_0039: ldfld        int32 class UserQuery/GenericClass`1<!0/*TC*/>::Modified
  // IL_003e: ldarg.1      // count
  // IL_003f: add
  // IL_0040: stfld        int32 class UserQuery/GenericClass`1<!0/*TC*/>::Modified
  // IL_0045: ret
  //
  {
    object mockedReturnValue;
    if (UserQuery.PatchedAssemblyBridge.TryMock(typeof (UserQuery.SimpleClass), (object) this, typeof (void), out mockedReturnValue, Type.EmptyTypes, new object[1]
    {
      (object) count
    }))
      return;
    this.Modified = this.Modified + count;
  }

  /*Method ReturnMethod with token 0600000F*/
  // .method public hidebysig instance int32
    // ReturnMethod(
      // int32 count
    // ) cil managed
  public int ReturnMethod(/*Parameter with token 08000010*/int count)
  // .maxstack 9
  // .locals init (
  // [0] object 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]',
    // [1] bool V_1,
    // [2] int32 V_2,
    // [3] int32 V_3
  // )
  //
  // IL_0000: nop
  // IL_0001: ldtoken      UserQuery/SimpleClass
  // IL_0006: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_000b: ldarg.0      // this
  // IL_000c: ldtoken      [mscorlib]System.Int32
  // IL_0011: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_0016: ldloca.s     'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]'
  // IL_0018: ldsfld       class [mscorlib]System.Type[] [mscorlib]System.Type::EmptyTypes
  // IL_001d: ldc.i4.1
  // IL_001e: newarr       [mscorlib]System.Object
  // IL_0023: dup
  // IL_0024: ldc.i4.0
  // IL_0025: ldarg.1      // count
  // IL_0026: box          [mscorlib]System.Int32
  // IL_002b: stelem.ref
  // IL_002c: call         bool UserQuery/PatchedAssemblyBridge::TryMock(class [mscorlib]System.Type, object, class [mscorlib]System.Type, object&, class [mscorlib]System.Type[], object[])
  // IL_0031: stloc.1      // V_1
  // IL_0032: ldloc.1      // V_1
  // IL_0033: brfalse.s    IL_003e
  // IL_0035: ldloc.0      // 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]'
  // IL_0036: unbox.any    [mscorlib]System.Int32
  // IL_003b: stloc.2      // V_2
  // IL_003c: br.s         IL_0052
  // IL_003e: ldarg.0      // this
  // IL_003f: ldarg.0      // this
  // IL_0040: ldfld        int32 class UserQuery/GenericClass`1<!0/*TC*/>::Modified
  // IL_0045: ldarg.1      // count
  // IL_0046: add
  // IL_0047: dup
  // IL_0048: stloc.3      // V_3
  // IL_0049: stfld        int32 class UserQuery/GenericClass`1<!0/*TC*/>::Modified
  // IL_004e: ldloc.3      // V_3
  // IL_004f: stloc.2      // V_2
  // IL_0050: br.s         IL_0052
  // IL_0052: ldloc.2      // V_2
  // IL_0053: ret
  //
  {
    object mockedReturnValue;
    if (UserQuery.PatchedAssemblyBridge.TryMock(typeof (UserQuery.SimpleClass), (object) this, typeof (int), out mockedReturnValue, Type.EmptyTypes, new object[1]
    {
      (object) count
    }))
      return (int) mockedReturnValue;
    return this.Modified = this.Modified + count;
  }

  /*Method VoidGenericMethod with token 06000010*/
  // .method public hidebysig instance void
    // VoidGenericMethod<T>(
      // !!0/*T*/ count
    // ) cil managed
  public void VoidGenericMethod</*Generic argument with token 2A000006*/T>(/*Parameter with token 08000011*/T count)
  // .maxstack 9
  // .locals init (
  // [0] object 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0016 ldloca.s))]',
    // [1] bool V_1
  // )
  //
  // IL_0000: nop
  // IL_0001: ldtoken      UserQuery/SimpleClass
  // IL_0006: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_000b: ldarg.0      // this
  // IL_000c: ldtoken      [mscorlib]System.Int32
  // IL_0011: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_0016: ldloca.s     'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0016 ldloca.s))]'
  // IL_0018: ldsfld       class [mscorlib]System.Type[] [mscorlib]System.Type::EmptyTypes
  // IL_001d: ldc.i4.1
  // IL_001e: newarr       [mscorlib]System.Object
  // IL_0023: dup
  // IL_0024: ldc.i4.0
  // IL_0025: ldarg.1      // count
  // IL_0026: box          !!0/*T*/
  // IL_002b: stelem.ref
  // IL_002c: call         bool UserQuery/PatchedAssemblyBridge::TryMock(class [mscorlib]System.Type, object, class [mscorlib]System.Type, object&, class [mscorlib]System.Type[], object[])
  // IL_0031: stloc.1      // V_1
  // IL_0032: ldloc.1      // V_1
  // IL_0033: brfalse.s    IL_0037
  // IL_0035: br.s         IL_0045
  // IL_0037: ldarg.0      // this
  // IL_0038: ldarg.0      // this
  // IL_0039: ldfld        int32 class UserQuery/GenericClass`1<!0/*TC*/>::Modified
  // IL_003e: ldc.i4.1
  // IL_003f: add
  // IL_0040: stfld        int32 class UserQuery/GenericClass`1<!0/*TC*/>::Modified
  // IL_0045: ret
  //
  {
    object mockedReturnValue;
    if (UserQuery.PatchedAssemblyBridge.TryMock(typeof (UserQuery.SimpleClass), (object) this, typeof (int), out mockedReturnValue, Type.EmptyTypes, new object[1]
    {
      (object) count
    }))
      return;
    this.Modified = this.Modified + 1;
  }

  /*Method GenericMethod with token 06000011*/
  // .method public hidebysig instance !!0/*T*/
    // GenericMethod<T>(
      // !!0/*T*/ count
    // ) cil managed
  public T GenericMethod</*Generic argument with token 2A000007*/T>(/*Parameter with token 08000012*/T count)
  // .maxstack 9
  // .locals init (
  // [0] object 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]',
    // [1] bool V_1,
    // [2] !!0/*T*/ V_2
  // )
  //
  // IL_0000: nop
  // IL_0001: ldtoken      UserQuery/SimpleClass
  // IL_0006: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_000b: ldarg.0      // this
  // IL_000c: ldtoken      [mscorlib]System.Int32
  // IL_0011: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
  // IL_0016: ldloca.s     'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]'
  // IL_0018: ldsfld       class [mscorlib]System.Type[] [mscorlib]System.Type::EmptyTypes
  // IL_001d: ldc.i4.1
  // IL_001e: newarr       [mscorlib]System.Object
  // IL_0023: dup
  // IL_0024: ldc.i4.0
  // IL_0025: ldarg.1      // count
  // IL_0026: box          !!0/*T*/
  // IL_002b: stelem.ref
  // IL_002c: call         bool UserQuery/PatchedAssemblyBridge::TryMock(class [mscorlib]System.Type, object, class [mscorlib]System.Type, object&, class [mscorlib]System.Type[], object[])
  // IL_0031: stloc.1      // V_1
  // IL_0032: ldloc.1      // V_1
  // IL_0033: brfalse.s    IL_003e
  // IL_0035: ldloc.0      // 'mockedReturnValue [Range(Instruction(IL_0016 ldloca.s)-Instruction(IL_0035 ldloc.0))]'
  // IL_0036: unbox.any    !!0/*T*/
  // IL_003b: stloc.2      // V_2
  // IL_003c: br.s         IL_0050
  // IL_003e: ldarg.0      // this
  // IL_003f: ldarg.0      // this
  // IL_0040: ldfld        int32 class UserQuery/GenericClass`1<!0/*TC*/>::Modified
  // IL_0045: ldc.i4.1
  // IL_0046: add
  // IL_0047: stfld        int32 class UserQuery/GenericClass`1<!0/*TC*/>::Modified
  // IL_004c: ldarg.1      // count
  // IL_004d: stloc.2      // V_2
  // IL_004e: br.s         IL_0050
  // IL_0050: ldloc.2      // V_2
  // IL_0051: ret
  //
  {
    object mockedReturnValue;
    if (UserQuery.PatchedAssemblyBridge.TryMock(typeof (UserQuery.SimpleClass), (object) this, typeof (int), out mockedReturnValue, Type.EmptyTypes, new object[1]
    {
      (object) count
    }))
      return (T) mockedReturnValue;
    this.Modified = this.Modified + 1;
    return count;
  }

  /*Method .ctor with token 06000012*/
  // .method public hidebysig specialname rtspecialname instance void
    // .ctor() cil managed
  public GenericClass()
  // .maxstack 8
  //
  // IL_0000: ldarg.0      // this
  // IL_0001: call         instance void [mscorlib]System.Object::.ctor()
  // IL_0006: nop
  // IL_0007: ret
  //
  {
    base.\u002Ector();
  }
}
