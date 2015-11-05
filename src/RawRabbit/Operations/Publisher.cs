﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Configuration.Publish;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Publisher<TMessageContext> : OperatorBase, IPublisher where TMessageContext : IMessageContext
	{
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;

		public Publisher(IChannelFactory channelFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider)
			: base(channelFactory, serializer)
		{
			_contextProvider = contextProvider;
		}

		public Task PublishAsync<T>(T message, Guid globalMessageId, PublishConfiguration config)
		{
			return _contextProvider
				.GetMessageContextAsync(globalMessageId)
				.ContinueWith(ctxTask =>
				{
					var channel = ChannelFactory.CreateChannel();
					DeclareQueue(config.Queue, channel);
					DeclareExchange(config.Exchange, channel);
					var properties = new BasicProperties
					{
						MessageId = Guid.NewGuid().ToString(),
						Headers = new Dictionary<string, object>
						{
							[_contextProvider.ContextHeaderName] = ctxTask.Result
						}
					};

					channel.BasicPublish(
						exchange: config.Exchange.ExchangeName,
						routingKey: config.RoutingKey,
						basicProperties: properties,
						body: Serializer.Serialize(message)
						);
					channel.Dispose();
				});
		}
	}
}
