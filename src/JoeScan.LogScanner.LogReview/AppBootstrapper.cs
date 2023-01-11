using System.IO;
using Autofac;
using Autofac.Extras.NLog;
using JoeScan.LogScanner.Core;
using JoeScan.LogScanner.LogReview.Shell;
using JoeScan.LogScanner.Shared;
using System.Windows;
using Config.Net;
using JoeScan.LogScanner.Core.Config;
using JoeScan.LogScanner.LogReview.CrossSection;
using JoeScan.LogScanner.LogReview.Interfaces;
using JoeScan.LogScanner.LogReview.Models;
using JoeScan.LogScanner.LogReview.SectionTable;
using JoeScan.LogScanner.LogReview.Settings;
using JoeScan.LogScanner.Shared.Log3D;
using MvvmDialogs;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace JoeScan.LogScanner.LogReview;

public class AppBootstrapper : AutofacBootstrapper
{
    protected override void OnStartup(object sender, StartupEventArgs e)
    {
        DisplayRootViewFor<ShellViewModel>();
    }

    protected override void ConfigureContainer(ContainerBuilder builder)
    {

        builder.Register(c => new ConfigurationBuilder<ILogReviewSettings>()
            .UseJsonFile(Path.Combine(c.Resolve<IConfigLocator>().GetDefaultConfigLocation(), "LogReviewConfig.json"))
            .Build()).SingleInstance();



        // the actual log scanner engine is in CoreModule
        builder.RegisterModule<CoreModule>();
        // logging
        builder.RegisterModule<NLogModule>();
        // use the reviewer object as an observable that holds the loaded log data
        builder.RegisterType<LogReviewer>().As<ILogModelObservable>().SingleInstance();
        builder.RegisterType<DialogService>().As<IDialogService>();
    }

    protected override void OnExit(object sender, EventArgs e)
    {
        Process[] pname = Process.GetProcessesByName("JoeScan.LogScanner.LogReview");
        try
        {
            if (pname.Length > 0)
            {
                pname.FirstOrDefault().Kill();
            }
        }
        catch (Exception)
        {
            Thread.Sleep(500);
            if (!pname.FirstOrDefault().HasExited)
            {
                throw;
            }
        }

    }
}

