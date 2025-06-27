using System;
using UnityEngine;

namespace Streams.StreamActions {

  public abstract class SelfClosingAction : StreamAction {

    public abstract float DeltaTime { get; }

    private protected override Delegate Action => _action;
    private readonly Action<SelfClosingAction> _action;

    private readonly InterruptException _interruptException = new();
    private float _sleepTime;
    private float _sleepTimestamp;
    private Func<bool> _wakeUpCondition;
    private bool _insideExecution;

    protected SelfClosingAction(Action<SelfClosingAction> action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    public void Sleep(float time) {
      switch (time) {
        case < 0:
          throw new ArgumentOutOfRangeException(nameof(time), time, "Time cannot be negative");
        case 0:
          return;
        default:
          _sleepTime = time;
          _sleepTimestamp = Time.time;
          break;
      }

      _wakeUpCondition = null;
      if (_insideExecution)
        throw _interruptException;
    }

    public void Sleep(Func<bool> wakeUpCondition) {
      _wakeUpCondition = wakeUpCondition ?? throw new ArgumentNullException(nameof(wakeUpCondition));
      _sleepTimestamp = _sleepTime = 0;
      if (_insideExecution)
        throw _interruptException;
    }

    private protected void InvokeAction() {
      try {
        _insideExecution = true;
        _action(this);
      }
      catch (InterruptException) {
      }
      finally {
        _insideExecution = false;
      }
    }

    private protected bool CanExecute() {
      if (Canceled())
        return false;
      if (Time.time < _sleepTime + _sleepTimestamp)
        return false;

      if (_wakeUpCondition != null) {
        if (!_wakeUpCondition())
          return false;
        _wakeUpCondition = null;
      }

      return true;
    }

  }

  public abstract class SelfClosingAction<TReturn> : StreamAction {

    public abstract float DeltaTime { get; }

    private protected override Delegate Action => _func;
    private readonly Func<SelfClosingAction<TReturn>, TReturn> _func;

    private readonly InterruptException _interruptException = new();
    private float _sleepTime;
    private float _sleepTimestamp;
    private Func<bool> _wakeUpCondition;
    private bool _insideExecution;

    protected SelfClosingAction(Func<SelfClosingAction<TReturn>, TReturn> func, StreamToken cancellationToken) : base(cancellationToken) {
      _func = func;
    }

    public void Sleep(float time) {
      switch (time) {
        case < 0:
          throw new ArgumentOutOfRangeException(nameof(time), time, "Time cannot be negative");
        case 0:
          return;
        default:
          _sleepTime = time;
          _sleepTimestamp = Time.time;
          break;
      }

      _wakeUpCondition = null;
      if (_insideExecution)
        throw _interruptException;
    }

    public void Sleep(Func<bool> wakeUpCondition) {
      _wakeUpCondition = wakeUpCondition ?? throw new ArgumentNullException(nameof(wakeUpCondition));
      _sleepTimestamp = _sleepTime = 0;
      if (_insideExecution)
        throw _interruptException;
    }

    private protected TReturn InvokeAction() {
      return _func(this);
    }

    private protected bool CanExecute() {
      if (Canceled())
        return false;
      if (Time.time < _sleepTime + _sleepTimestamp)
        return false;

      if (_wakeUpCondition != null) {
        if (!_wakeUpCondition())
          return false;
        _wakeUpCondition = null;
      }

      return true;
    }

  }

}