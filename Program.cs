using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading;

namespace FFMpegShutdown
{
	internal class Program
	{
		private static readonly bool _actuallyShutdown = true;

		private static readonly Timer CheckTimer = new(DoCheck);
		private static bool _shutdownArmed = false;
		private static bool _keepRunning = true;

		static void Main()
		{
			IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile("serilog.json").Build();
			Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().ReadFrom.Configuration(configuration).CreateLogger();

			Log.Information("----------------------------------------");
			Log.Information("Started");

			CheckTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));

			while (_keepRunning) Thread.Sleep(1);

			Log.Information("Stopping");

			Thread.Sleep(5000);

			Log.Information("Stopped");
		}

		private static void DoCheck(object? state)
		{
			Log.Information("----------");
			Log.Debug("_shutdownArmed = {shutdownArmed}", _shutdownArmed);

			bool ffmpegIsRunning = false;

			Process[] processes = Process.GetProcessesByName("ffmpeg");

			if (processes.Length > 0) ffmpegIsRunning = true;

			Log.Information("ffmpeg processes running: {Count}", processes.Length);
			foreach (Process p in processes) Log.Information("|   PID: {PID}", p.Id);

			Log.Debug("ffmpegIsRunning = {ffmpegIsRunning}", ffmpegIsRunning);

			if (ffmpegIsRunning)
			{
				if (_shutdownArmed)
				{
					_shutdownArmed = false;
					Log.Information("Shutdown disarmed");
				}
			}
			else
			{
				if (_shutdownArmed)
				{
					Log.Information("Shutdown is armed. Commencing shutdown...");
					DoShutdown();
				}
				else
				{
					_shutdownArmed = true;
					Log.Information("Shutdown has been armed.");
				}
			}
		}

		private static void DoShutdown()
		{
			CheckTimer.Change(Timeout.Infinite, Timeout.Infinite);

			if (_actuallyShutdown)
			{
				Process p = new();
				p.StartInfo.FileName = "shutdown";
				p.StartInfo.Arguments = "/s /f /t 10";
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.UseShellExecute = false;
				p.Start();
			}
			_keepRunning = false;
		}
	}
}
