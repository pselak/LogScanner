﻿using System;

namespace JoeScan.LogScanner.Desktop;

public interface IUserNotifier
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Success(string message);
    bool IsBusy { get; set; }
    public event EventHandler? BusyChanged;

}
