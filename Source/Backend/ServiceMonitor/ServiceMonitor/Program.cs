﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceMonitor.Common;

namespace ServiceMonitor
{
    class Program
    {
        private static ILogger logger;

        static Program()
        {
            logger = LoggerHelper.GetLogger<Program>();
        }

        static void Main(string[] args)
        {
            StartAsync().GetAwaiter().GetResult();

            Console.ReadLine();
        }

        static async Task StartAsync()
        {
            logger.LogDebug("Starting application...");

            var initializer = new ServiceMonitorInitializer();

            try
            {
                await initializer.LoadResponseAsync();
            }
            catch (Exception ex)
            {
                logger.LogError("Error on retrieve watch items: {0}", ex);
                return;
            }

            try
            {
                initializer.DeserializeResponse();
            }
            catch (Exception ex)
            {
                logger.LogError("Error on deserializing object: {0}", ex);
                return;
            }

            foreach (var item in initializer.Response.Model)
            {
                var watcherType = Type.GetType(item.TypeName, true);

                var watcherInstance = Activator.CreateInstance(watcherType) as IWatcher;

                var task = Task.Factory.StartNew(async () =>
                {
                    var controller = new MonitorController(logger, watcherInstance, initializer.RestClient);

                    await controller.ProcessAsync(item);
                });
            }
        }
    }
}
