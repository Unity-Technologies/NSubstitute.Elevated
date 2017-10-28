<Query Kind="Program" />

void Main()
{
	Assembly.GetExecutingAssembly().Location.Dump();
}

// purpose of this file is to exercise all patched and unpatched variations, for examining the
// compiled IL and having the patcher generate it.
//
// variations to apply:
//   class is concrete or generic
//   method is concrete or generic
//   return is void, ref type, value type

public class MockPlaceholderType { }
public static class PatchedAssemblyBridge
{
	public static bool TryMock(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, Type[] methodGenericTypes, object[] args)
	{
		mockedReturnValue = null;
		return true;
	}
}

public class SimpleClass
{
	public int Modified;

	public void VoidMethodPatched(int count)
	{
		if (PatchedAssemblyBridge.TryMock(typeof(SimpleClass), this, typeof(void), out var _, Type.EmptyTypes, new object[] { count }))
			return;

		Modified += count;
	}

	public void VoidMethod(int count)
	{
		Modified += count;
	}

	public int ReturnMethodPatched(int count)
	{
		if (PatchedAssemblyBridge.TryMock(typeof(SimpleClass), this, typeof(int), out var returnValue, Type.EmptyTypes, new object[] { count }))
			return (int)returnValue;

		return Modified += count;
	}

	public int ReturnMethod(int count)
	{
		return Modified += count;
	}

	public void VoidGenericMethodPatched<T>(T count)
	{
		if (PatchedAssemblyBridge.TryMock(typeof(SimpleClass), this, typeof(int), out var returnValue, Type.EmptyTypes, new object[] { count }))
			return;

		++Modified;
	}

	public void VoidGenericMethod<T>(T count)
	{
		++Modified;
	}

	public T GenericMethodPatched<T>(T count)
	{
		if (PatchedAssemblyBridge.TryMock(typeof(SimpleClass), this, typeof(int), out var returnValue, Type.EmptyTypes, new object[] { count }))
			return (T)returnValue;

		++Modified;
		return count;
	}

	public T GenericMethod<T>(T count)
	{
		++Modified;
		return count;
	}
}

public class GenericClass<TC>
{
	public int Modified;

	public void VoidMethod(int count)
	{
		if (PatchedAssemblyBridge.TryMock(typeof(SimpleClass), this, typeof(void), out var _, Type.EmptyTypes, new object[] { count }))
			return;

		Modified += count;
	}

	public int ReturnMethod(int count)
	{
		if (PatchedAssemblyBridge.TryMock(typeof(SimpleClass), this, typeof(int), out var returnValue, Type.EmptyTypes, new object[] { count }))
			return (int)returnValue;

		return Modified += count;
	}

	public void VoidGenericMethod<T>(T count)
	{
		if (PatchedAssemblyBridge.TryMock(typeof(SimpleClass), this, typeof(int), out var returnValue, Type.EmptyTypes, new object[] { count }))
			return;

		++Modified;
	}
	public T GenericMethod<T>(T count)
	{
		if (PatchedAssemblyBridge.TryMock(typeof(SimpleClass), this, typeof(int), out var returnValue, Type.EmptyTypes, new object[] { count }))
			return (T)returnValue;

		++Modified;
		return count;
	}
}
