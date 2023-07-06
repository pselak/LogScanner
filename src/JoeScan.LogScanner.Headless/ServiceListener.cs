using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JoeScan.LogScanner.Core.Models;
using NLog;

namespace JoeScan.LogScanner.Service
{
    internal class ServiceListener
    {
        HttpListener httpListener;
        Thread listenThread1;
        LogScannerEngine engine;
        static Logger logApp;


        public void StartServer(ref LogScannerEngine _engine)
        {
            logApp = LogManager.GetCurrentClassLogger();
            engine = _engine;
            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:8000/");
            httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            httpListener.Start();
             listenThread1 = new Thread( new ParameterizedThreadStart(StartListener));
            listenThread1.Start();
        }

        private void StartListener(object s)
        {
            while (true){
                ProcessRequest();
            }
        }

        private void ProcessRequest()
        {
            var result = httpListener.BeginGetContext(ListenerCallback, httpListener);
            result.AsyncWaitHandle.WaitOne();
        }
        private void ListenerCallback(IAsyncResult result)
        {
            try
            {
                var context = httpListener.EndGetContext(result);
                if(context.Request.HttpMethod == "OPTIONS") {
                    //handle CORS
                }
                else
                {
                    String? action = context.Request.QueryString.Get("Action");
                    if (action != null)
                    {
                        if (action == "Start")
                        {
                            logApp.Debug($"scanning started");
                            engine.Start();
                        }
                        if (action == "Stop")
                        {
                            logApp.Debug($"scanning stopped");
                            engine.Stop();
                        }
                    }
                    context.Response.StatusCode = 200;
                    context.Response.StatusDescription = "OK";
                    context.Response.ContentType = "application/json";
                    context.Response.Close();
                }
            }
            catch (Exception)
            {
                //TODO: Log an exception
            }
        }
    }
}
