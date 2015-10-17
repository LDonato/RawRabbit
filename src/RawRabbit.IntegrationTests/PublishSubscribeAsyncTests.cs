﻿using System.Threading.Tasks;
using RawRabbit.Client;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests
{
	public class PublishSubscribeAsyncTests
	{
		[Fact]
		public async void Should_Support_Simple_Subscribe_Publish_Scenario()
		{
			/* Setup */
			var messageHandlerCalled = false;
			var sender = new BusClient();
			var reciever = new BusClient();
			reciever.SubscribeAsync<BasicMessage>((message, info) =>
			{
				messageHandlerCalled = true;
				return Task.FromResult(true);
			}, configuration =>
				configuration
					.WithExchange(exchange =>
						exchange
							.WithName("my_exchange")
							.WithAutoDelete()
							.WithType("direct")
						)
					.WithQueue(queue =>
						queue
							.WithName("my_queue")
							.WithRoutingKey("hello")
					)
			);

			/* Test */
			await sender.PublishAsync(new BasicMessage());

			/* Assert */
			Assert.True(messageHandlerCalled);
		}
	}
}