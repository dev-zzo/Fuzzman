﻿using System;
using Fuzzman.Agent.Config;
using Fuzzman.Core;

namespace Fuzzman.Agent
{
    public class Agent : IAgent
    {
        public Agent(Options options, AgentConfiguration config)
        {
            this.options = options;
            this.config = config;
        }

        public void Start()
        {
            Console.CancelKeyPress += this.OnControlC;

            // http://msdn.microsoft.com/en-us/library/windows/hardware/ff545528%28v=vs.85%29.aspx
            Environment.SetEnvironmentVariable("_NO_DEBUG_HEAP", "1");

            this.logger.Info("Starting agent thread(s)...");
            this.threads = new AgentThread[this.options.ParallelInstances];
            for (int i = 0; i < this.threads.Length; ++i)
            {
                AgentThread agent = new AgentThread(i, this.config);
                agent.Start();
                this.threads[i] = agent;
            }
        }

        public void Stop()
        {
            this.logger.Info("Stopping agent thread(s)...");
            foreach (AgentThread agent in this.threads)
            {
                agent.Stop();
            }
        }

        private readonly ILogger logger = LogManager.GetLogger("Agent");
        private readonly Options options = null;
        private readonly AgentConfiguration config = null;
        private AgentThread[] threads;

        private void OnControlC(object sender, ConsoleCancelEventArgs args)
        {
            this.logger.Info("Caught ctrl-c, stopping.");
            this.Stop();
        }

    }
}
