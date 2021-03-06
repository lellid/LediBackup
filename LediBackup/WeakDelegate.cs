﻿/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LediBackup
{
  /// <summary>
  /// Can be used to build an event that binds the clients weak.
  /// </summary>
  /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
  /// <remarks>Credits and further info: Albahari, Joseph and Ben, C# 6.0 in a Nutshell - The definitive reference.</remarks>
  public class WeakDelegate<TDelegate> where TDelegate : class
  {
    private List<MethodTarget> _targets = new List<MethodTarget>();

    public WeakDelegate()
    {
      if (!typeof(TDelegate).IsSubclassOf(typeof(Delegate)))
        throw new InvalidOperationException("TDelegate must be a delegate type");
    }

    public void Combine(TDelegate target)
    {
      if (target is Delegate targetDelegate)
      {
        foreach (Delegate d in targetDelegate.GetInvocationList())
          _targets.Add(new MethodTarget(d));
      }
    }

    public void Remove(TDelegate target)
    {
      if (target is Delegate targetDelegate)
      {
        foreach (Delegate d in targetDelegate.GetInvocationList())
        {
          if (_targets.Find(w => Equals(d.Target, w.Reference?.Target) && Equals(d.Method.MethodHandle, w.Method.MethodHandle)) is { } methodTarget)
          {
            _targets.Remove(methodTarget);
          }
        }
      }
    }

    /// <summary>
    /// Gets or sets the target.
    /// Gets all delegates that are still alive (and removes the dead targets by the way).
    /// If set, it removes the present targets and replaces them with the provided target(s).
    /// </summary>
    /// <value>
    /// The target.
    /// </value>
    public TDelegate? Target
    {
      get
      {
        Delegate? combinedTarget = null;
        foreach (MethodTarget methodTarget in _targets.ToArray())
        {
          var weakReference = methodTarget.Reference;
          //     Static target      ||    alive instance target
          if (null == weakReference || null != weakReference.Target)
          {
            var newDelegate = Delegate.CreateDelegate(typeof(TDelegate), weakReference?.Target, methodTarget.Method);

            combinedTarget = Delegate.Combine(combinedTarget, newDelegate);
          }
          else // Target is already garbage collected
          {
            _targets.Remove(methodTarget); // thus remove it
          }
        }

        return combinedTarget as TDelegate;
      }
      set
      {
        if (!(value is null))
        {
          _targets.Clear();
          Combine(value);
        }
      }
    }

    #region Inner class

    private class MethodTarget
    {
      public readonly WeakReference Reference;
      public readonly MethodInfo Method;

      public MethodTarget(Delegate d)
      {
        Reference = new WeakReference(d.Target);
        Method = d.Method;
      }
    }

    #endregion Inner class
  }
}
