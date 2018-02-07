﻿using System;
using System.IO;

using Serilog;

using RawRabbit;
using RawRabbit.Logging;

using System.Threading.Tasks;

using System.Collections.Generic;

namespace RawRabbitTest
{
    class Program
    {
        static ILogger logger;

        static void Main(string[] args)
        {
            
            Serilog.LoggerConfiguration loggerConfiguration = new Serilog.LoggerConfiguration() 
                .MinimumLevel.Debug()
                .WriteTo.RollingFile(Path.Combine(Directory.GetCurrentDirectory(), "Logs/log-{Date}.txt"));
            logger = loggerConfiguration.CreateLogger();
            
            /*Serilog.LoggerConfiguration loggerConfiguration2 = new Serilog.LoggerConfiguration() 
                .MinimumLevel.Debug()
                .WriteTo.RollingFile(Path.Combine(Directory.GetCurrentDirectory(), "Logs/rawRabbit-{Date}.txt"));
            ILogger logger2 = loggerConfiguration2.CreateLogger();*/

            IBusClient client = RawRabbit.Instantiation.RawRabbitFactory.CreateSingleton();
            //Log.Logger = logger2;
            client.SubscribeAsync<List<Message>>(
               async (message) => {
                    logger.Information("received data {0}.Forwarding ...", message[0].cnt);
                    await client.PublishAsync(message,
                        ctx => ctx.UsePublishConfiguration(
                            cfg => cfg.OnDeclaredExchange(exchange => exchange.WithName("queue2"))
                        ).UsePublishAcknowledge(false));
                },
                ctx => ctx.UseSubscribeConfiguration(
                    cfg => cfg.OnDeclaredExchange(exchange => exchange.WithName("queue1")
                )
            ));

            client.SubscribeAsync<List<Message>>(
                async (message) => {
                    logger.Information("received forwarded data {0}. ...", message[0].cnt);
                },
                ctx => ctx.UseSubscribeConfiguration(
                    cfg => cfg.OnDeclaredExchange(exchange => exchange.WithName("queue2")
                )
            ));

            List<Publisher> publishers = new List<Publisher>();

            for(int i = 0; i < 100; i++)
            {
                publishers.Add(new Publisher(i));
            }

            while(true)
            {
                foreach (Publisher p in publishers)
                {
                    List<Message> toBePublished = p.GetMessage();
                    logger.Information("Publisher {0} is publishing message {1}", p.id, toBePublished[0].cnt);
                    client.PublishAsync(toBePublished,
                        ctx => ctx.UsePublishConfiguration(
                            cfg => cfg.OnDeclaredExchange(exchange => exchange.WithName("queue1"))
                        ));
                }
            }
        }

        public class Message
        {
            static public int Counter {get;set;}
            public int cnt;
            public Message()
            {
                Counter ++;
                cnt = Counter;
            }
        }

        public class Publisher
        {
            public int id;

            public Publisher(int id) {this.id = id;}
            public List<Message> GetMessage()
            {
                List<Message>ret = new List<Message>();
                for(int i = 0; i < 10; i++)
                {
                    ret.Add(new Message());
                }
                return ret;
            }
        }    

    }
}