using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace BeerMP.Helpers;

public class ActionContainer
{
	internal Dictionary<Action, MethodBase> actions = new Dictionary<Action, MethodBase>();

	public static ActionContainer operator +(ActionContainer self, Action action)
	{
		if (self == null)
		{
			self = new ActionContainer();
		}
		StackTrace stackTrace = new StackTrace();
		self.actions.Add(action, stackTrace.GetFrame(1).GetMethod());
		return self;
	}

	public static ActionContainer operator -(ActionContainer self, Action action)
	{
		if (self == null)
		{
			throw new NullReferenceException();
		}
		self.actions.Remove(action);
		return self;
	}

	public void Invoke()
	{
		foreach (KeyValuePair<Action, MethodBase> action in actions)
		{
			try
			{
				action.Key?.Invoke();
			}
			catch (Exception ex)
			{
				Console.LogError($"ActionContainer: action of {action.Value.DeclaringType}.{action.Value.Name} threw an exception!\nMessage: {ex.Message}, stack trace: {ex.StackTrace}");
			}
		}
	}
}
public class ActionContainer<T>
{
	internal Dictionary<Action<T>, MethodBase> actions = new Dictionary<Action<T>, MethodBase>();

	public static ActionContainer<T> operator +(ActionContainer<T> self, Action<T> action)
	{
		if (self == null)
		{
			self = new ActionContainer<T>();
		}
		StackTrace stackTrace = new StackTrace();
		self.actions.Add(action, stackTrace.GetFrame(1).GetMethod());
		return self;
	}

	public static ActionContainer<T> operator -(ActionContainer<T> self, Action<T> action)
	{
		if (self == null)
		{
			throw new NullReferenceException();
		}
		self.actions.Remove(action);
		return self;
	}

	public void Invoke(T obj)
	{
		foreach (KeyValuePair<Action<T>, MethodBase> action in actions)
		{
			try
			{
				action.Key?.Invoke(obj);
			}
			catch (Exception ex)
			{
				Console.LogError($"ActionContainer: action of {action.Value.DeclaringType}.{action.Value.Name} threw an exception!\nMessage: {ex.Message}, stack trace: {ex.StackTrace}");
			}
		}
	}
}
public class ActionContainer<T1, T2>
{
	internal Dictionary<Action<T1, T2>, MethodBase> actions = new Dictionary<Action<T1, T2>, MethodBase>();

	public static ActionContainer<T1, T2> operator +(ActionContainer<T1, T2> self, Action<T1, T2> action)
	{
		if (self == null)
		{
			self = new ActionContainer<T1, T2>();
		}
		StackTrace stackTrace = new StackTrace();
		self.actions.Add(action, stackTrace.GetFrame(1).GetMethod());
		return self;
	}

	public static ActionContainer<T1, T2> operator -(ActionContainer<T1, T2> self, Action<T1, T2> action)
	{
		if (self == null)
		{
			throw new NullReferenceException();
		}
		self.actions.Remove(action);
		return self;
	}

	public void Invoke(T1 arg1, T2 arg2)
	{
		foreach (KeyValuePair<Action<T1, T2>, MethodBase> action in actions)
		{
			try
			{
				action.Key?.Invoke(arg1, arg2);
			}
			catch (Exception ex)
			{
				Console.LogError($"ActionContainer: action of {action.Value.DeclaringType}.{action.Value.Name} threw an exception!\nMessage: {ex.Message}, stack trace: {ex.StackTrace}");
			}
		}
	}
}
public class ActionContainer<T1, T2, T3>
{
	internal Dictionary<Action<T1, T2, T3>, MethodBase> actions = new Dictionary<Action<T1, T2, T3>, MethodBase>();

	public static ActionContainer<T1, T2, T3> operator +(ActionContainer<T1, T2, T3> self, Action<T1, T2, T3> action)
	{
		if (self == null)
		{
			self = new ActionContainer<T1, T2, T3>();
		}
		StackTrace stackTrace = new StackTrace();
		self.actions.Add(action, stackTrace.GetFrame(1).GetMethod());
		return self;
	}

	public static ActionContainer<T1, T2, T3> operator -(ActionContainer<T1, T2, T3> self, Action<T1, T2, T3> action)
	{
		if (self == null)
		{
			throw new NullReferenceException();
		}
		self.actions.Remove(action);
		return self;
	}

	public void Invoke(T1 arg1, T2 arg2, T3 arg3)
	{
		foreach (KeyValuePair<Action<T1, T2, T3>, MethodBase> action in actions)
		{
			try
			{
				action.Key?.Invoke(arg1, arg2, arg3);
			}
			catch (Exception ex)
			{
				Console.LogError($"ActionContainer: action of {action.Value.DeclaringType}.{action.Value.Name} threw an exception!\nMessage: {ex.Message}, stack trace: {ex.StackTrace}");
			}
		}
	}
}
public class ActionContainer<T1, T2, T3, T4>
{
	internal Dictionary<Action<T1, T2, T3, T4>, MethodBase> actions = new Dictionary<Action<T1, T2, T3, T4>, MethodBase>();

	public static ActionContainer<T1, T2, T3, T4> operator +(ActionContainer<T1, T2, T3, T4> self, Action<T1, T2, T3, T4> action)
	{
		if (self == null)
		{
			self = new ActionContainer<T1, T2, T3, T4>();
		}
		StackTrace stackTrace = new StackTrace();
		self.actions.Add(action, stackTrace.GetFrame(1).GetMethod());
		return self;
	}

	public static ActionContainer<T1, T2, T3, T4> operator -(ActionContainer<T1, T2, T3, T4> self, Action<T1, T2, T3, T4> action)
	{
		if (self == null)
		{
			throw new NullReferenceException();
		}
		self.actions.Remove(action);
		return self;
	}

	public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		foreach (KeyValuePair<Action<T1, T2, T3, T4>, MethodBase> action in actions)
		{
			try
			{
				action.Key?.Invoke(arg1, arg2, arg3, arg4);
			}
			catch (Exception ex)
			{
				Console.LogError($"ActionContainer: action of {action.Value.DeclaringType}.{action.Value.Name} threw an exception!\nMessage: {ex.Message}, stack trace: {ex.StackTrace}");
			}
		}
	}
}
